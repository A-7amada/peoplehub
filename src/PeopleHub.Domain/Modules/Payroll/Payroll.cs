namespace PeopleHub.Domain.Entities;

public enum ComponentType { Earning = 0, Deduction = 1 }
public enum PayrollStatus { Draft = 0, HRReviewed = 1, FinanceApproved = 2, Finalized = 3, Paid = 4 }

public class SalaryComponent
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameAr { get; set; } = "";
    public ComponentType Type { get; set; }
    /// <summary>Fixed components have a standing amount per employee; variable ones come from attendance, requests or manual entry.</summary>
    public bool IsFixed { get; set; } = true;
}

public class EmployeeSalaryComponent
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int ComponentId { get; set; }
    public SalaryComponent? Component { get; set; }
    public decimal Amount { get; set; }
}

public class AdvanceRequest : Approvable
{
    public decimal Amount { get; set; }
    public int Installments { get; set; }
    public int StartYear { get; set; }
    public int StartMonth { get; set; }
    public decimal Paid { get; set; }
    public string Reason { get; set; } = "";
    public decimal Monthly => Installments > 0 ? Math.Round(Amount / Installments, 2) : 0;
    public decimal Remaining => Amount - Paid;

    public static AdvanceRequest Create(Employee e, decimal amount, int installments, int year, int month, string? reason)
    {
        if (amount <= 0 || decimal.Round(amount,2)!=amount || year is <2000 or >9998 || month is <1 or >12) throw new DomainException("err.advanceAmount");
        if (installments is < 1 or > 24) throw new DomainException("err.installments");
        if (amount > e.BasicSalary * 3) throw new DomainException("err.advanceLimit");
        return new AdvanceRequest { EmployeeId = e.Id, Amount = amount, Installments = installments, StartYear = year, StartMonth = month,
            Reason = reason?.Trim() ?? "", Route = ApprovalStage.HR + "," + ApprovalStage.Finance };
    }

    public bool DueIn(int year, int month) => Status == RequestStatus.Approved && Remaining > 0 && (year * 12 + month) >= (StartYear * 12 + StartMonth);
    public decimal InstallmentFor() => Math.Min(Monthly, Remaining);

    /// <summary>Withdraws an approved advance before any installment was deducted. Partly repaid advances need an owner policy, so they are refused.</summary>
    public void CancelUnpaid()
    {
        if (Status != RequestStatus.Approved || Paid != 0) throw new DomainException("err.notPending");
        Status = RequestStatus.Cancelled;
    }

    public void Settle()
    {
        if (Status != RequestStatus.Approved || Remaining <= 0) throw new DomainException("err.notPending");
        Paid = Amount;
    }
}

public class PayrollRun
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public PayrollStatus Status { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }
    public int CalculationVersion { get; set; }
    public string? FinalizedBy { get; set; }
    public bool AdvancesApplied { get; set; }
    public string CorrectionStamp { get; set; } = "";
    public bool IsLocked => Status is PayrollStatus.Finalized or PayrollStatus.Paid;

    public void Review() => Move(PayrollStatus.Draft, PayrollStatus.HRReviewed);
    public void FinanceApprove() => Move(PayrollStatus.HRReviewed, PayrollStatus.FinanceApproved);
    public void Finalize() => Move(PayrollStatus.FinanceApproved, PayrollStatus.Finalized);

    public void MarkPaid(DateTime now)
    {
        Move(PayrollStatus.Finalized, PayrollStatus.Paid);
        PaidAt = now;
    }

    public void SendBack(string? reason)
    {
        if (Status is not (PayrollStatus.HRReviewed or PayrollStatus.FinanceApproved)) throw new DomainException("err.payrollState");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("err.reasonRequired");
        Status = PayrollStatus.Draft;
        Note = reason.Trim();
    }

    /// <summary>Documented exceptional reopening of a closed run.</summary>
    public void Reopen(string? reason)
    {
        if (!IsLocked) throw new DomainException("err.payrollState");
        if (Status == PayrollStatus.Paid) throw new DomainException("err.payrollPaid");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("err.reasonRequired");
        Status = PayrollStatus.Draft;
        Note = "Reopened: " + reason.Trim();
    }

    private void Move(PayrollStatus from, PayrollStatus to)
    {
        if (Status != from) throw new DomainException("err.payrollState");
        Status = to;
    }
}

public class PayrollLine
{
    public int Id { get; set; }
    public int RunId { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string EmployeeCodeSnapshot { get; set; } = "";
    public string EmployeeNameSnapshot { get; set; } = "";
    public string EmployeeNameArSnapshot { get; set; } = "";
    public string DepartmentSnapshot { get; set; } = "";
    public string DepartmentArSnapshot { get; set; } = "";
    public string CostCenterSnapshot { get; set; } = "";
    public string BankSnapshot { get; set; } = "";
    public string IbanSnapshot { get; set; } = "";
    public decimal FixedDeduction { get; set; }
    public decimal Basic { get; set; }
    public decimal Allowances { get; set; }
    public decimal Overtime { get; set; }
    public decimal Bonus { get; set; }
    public decimal Gross { get; set; }
    public decimal LateDeduction { get; set; }
    public decimal AbsenceDeduction { get; set; }
    public decimal Insurance { get; set; }
    public decimal Tax { get; set; }
    public decimal Martyrs { get; set; }
    public decimal AdvanceInstallment { get; set; }
    public decimal OtherDeduction { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal Net { get; set; }
    public int AbsentDays { get; set; }
    public int LateMinutes { get; set; }
    public decimal OvertimeHours { get; set; }
    public string? AdjustmentNote { get; set; }
}

public record PayrollInput(decimal Basic, decimal Allowances, decimal OvertimeValueBase, decimal Bonus, int AbsentDays,
    int LateMinutes, decimal OvertimeWeightedHours, decimal AdvanceInstallment, decimal OtherDeduction);

/// <summary>Payroll rules using the 2026 Egyptian parameters as published by Deloitte, PwC and Law 91/2005 as amended. A demo: verify with a specialist before real use.</summary>
public static class PayrollCalculator
{
    public const decimal DaysPerMonth = 30m;
    public const decimal HoursPerDay = 8m;
    // Values come from the active country pack (Strategy, ADR-0007). They stay as constants for existing callers and tests.
    public const decimal InsuranceRate = EgyptPack.InsuranceRate;
    public const decimal InsuranceMin = EgyptPack.InsuranceMin;
    public const decimal InsuranceCap = EgyptPack.InsuranceCap;
    public const decimal AnnualExemption = EgyptPack.AnnualExemption;
    public const decimal MonthlyExemption = AnnualExemption / 12m;
    public const decimal MartyrsRate = EgyptPack.MartyrsRate;

    public static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    /// <summary>Annual brackets: 0-40k 0%, 40-55k 10%, 55-70k 15%, 70-200k 20%, 200-400k 22.5%, 400-600k 25%, above 27.5%.</summary>
    public static decimal AnnualTax(decimal annualTaxable) => AnnualTax(annualTaxable, EgyptPack.Y2026);

    /// <summary>Annual tax using the slabs of the given country pack.</summary>
    public static decimal AnnualTax(decimal annualTaxable, ICountryPack pack)
    {
        if (annualTaxable <= 0) return 0;
        decimal tax = 0, prev = 0;
        foreach (var (upTo, rate) in pack.TaxSlabs)
        {
            if (annualTaxable > prev) tax += (Math.Min(annualTaxable, upTo) - prev) * rate;
            prev = upTo;
        }
        return tax;
    }

    /// <summary>Monthly tax for a monthly taxable amount (already net of the personal exemption).</summary>
    public static decimal Tax(decimal monthlyTaxable) => Tax(monthlyTaxable, EgyptPack.Y2026);
    public static decimal Tax(decimal monthlyTaxable, ICountryPack pack) => monthlyTaxable <= 0 ? 0 : Round(AnnualTax(monthlyTaxable * 12m, pack) / 12m);

    public static decimal InsuranceOf(decimal insurableWage) => InsuranceOf(insurableWage, EgyptPack.Y2026);
    public static decimal InsuranceOf(decimal insurableWage, ICountryPack pack) => Round(Math.Min(Math.Max(insurableWage, pack.InsuranceMin), pack.InsuranceCap) * pack.InsuranceRate);

    /// <summary>The pack in force for a payroll period. A period before the first known pack keeps using the first pack, which is what the engine did before packs were effective-dated; that case is reported by <see cref="IsFallback"/>, not guessed silently.</summary>
    public static ICountryPack PackFor(int year, int month, string country = "EG")
    {
        try { return CountryPacks.Resolve(country, new DateTime(year, month, 1)); }
        catch (DomainException) when (country == "EG") { return EgyptPack.Y2026; }
    }
    public static bool IsFallback(int year, int month, string country = "EG") => !CountryPacks.Covers(country, new DateTime(year, month, 1));

    public static void Apply(PayrollLine l, PayrollInput i) => Apply(l, i, EgyptPack.Y2026);

    public static void Apply(PayrollLine l, PayrollInput i, ICountryPack pack)
    {
        var daily = i.Basic / DaysPerMonth;
        var hourly = daily / HoursPerDay;
        l.Basic = Round(i.Basic);
        l.Allowances = Round(i.Allowances);
        l.Overtime = Round(i.OvertimeWeightedHours * hourly);
        l.Bonus = Round(i.Bonus);
        l.Gross = l.Basic + l.Allowances + l.Overtime + l.Bonus;
        l.AbsentDays = i.AbsentDays;
        l.LateMinutes = i.LateMinutes;
        l.AbsenceDeduction = Round(i.AbsentDays * daily);
        l.LateDeduction = Round(i.LateMinutes * (hourly / 60m));
        l.Insurance = InsuranceOf(i.Basic + i.Allowances, pack);
        l.Martyrs = Round(l.Gross * pack.MartyrsRate);
        var taxable = l.Gross - l.AbsenceDeduction - l.LateDeduction - l.Insurance - pack.AnnualExemption / 12m;
        l.Tax = Tax(taxable, pack);
        l.AdvanceInstallment = Round(i.AdvanceInstallment);
        l.OtherDeduction = Round(i.OtherDeduction);
        l.TotalDeductions = l.LateDeduction + l.AbsenceDeduction + l.Insurance + l.Martyrs + l.Tax + l.AdvanceInstallment + l.OtherDeduction;
        l.Net = l.Gross - l.TotalDeductions;
    }
}
