namespace PeopleHub.Domain.Entities;

public enum EmployeeStatus { Active = 0, OnLeave = 1, Suspended = 2, Resigned = 3, Terminated = 4, Retired = 5 }
public enum EmploymentType { FullTime = 0, PartTime = 1, Contractor = 2 }
public enum Gender { Male = 0, Female = 1 }
public enum ContractType { Permanent = 0, FixedTerm = 1 }
public enum CareerEventType { Promotion = 0, Transfer = 1, TitleChange = 2, SalaryChange = 3, ManagerChange = 4, ContractRenewal = 5, Reward = 6, Penalty = 7 }

public class Employee
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string FullName { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public string NationalId { get; set; } = "";
    public DateTime? BirthDate { get; set; }
    public Gender Gender { get; set; }
    public string MaritalStatus { get; set; } = "";
    public string Nationality { get; set; } = "Egyptian";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";

    public string LeaveRequestStamp { get; set; } = "";
    public DateTime HireDate { get; set; }
    public int JobTitleId { get; set; }
    public JobTitle? JobTitle { get; set; }
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    public int? ManagerId { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public int? ShiftId { get; set; }

    public string BankName { get; set; } = "";
    public string Iban { get; set; } = "";
    public string InsuranceNumber { get; set; } = "";
    public decimal BasicSalary { get; set; }

    public bool IsWorking => Status is EmployeeStatus.Active or EmployeeStatus.OnLeave;

    public void Validate(bool validateNationalId=true)
    {
        if (string.IsNullOrWhiteSpace(FullName)) throw new DomainException("err.name");
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@')) throw new DomainException("err.email");
        if (BasicSalary < 0) throw new DomainException("err.salary");
        if (validateNationalId && !string.IsNullOrWhiteSpace(NationalId) && NationalId.Trim().Length != 14) throw new DomainException("err.nationalId");
        Email = Email.Trim().ToLowerInvariant();
    }

    /// <summary>Returns the history entry for the change. Terminal statuses cannot be left.</summary>
    public StatusHistory ChangeStatus(EmployeeStatus to, string by, string? reason, DateTime now)
    {
        if (!Enum.IsDefined(to)) throw new DomainException("err.required");
        if (to == Status) throw new DomainException("err.sameStatus");
        if (Status is EmployeeStatus.Terminated or EmployeeStatus.Resigned or EmployeeStatus.Retired)
            throw new DomainException("err.finalStatus");
        var h = new StatusHistory { EmployeeId = Id, From = Status, To = to, ChangedBy = by, Reason = reason?.Trim(), At = now };
        Status = to;
        return h;
    }
}

public class StatusHistory
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public EmployeeStatus From { get; set; }
    public EmployeeStatus To { get; set; }
    public DateTime At { get; set; }
    public string ChangedBy { get; set; } = "";
    public string? Reason { get; set; }
}

public class CareerEvent
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public CareerEventType Type { get; set; }
    public DateTime Date { get; set; }
    public string Details { get; set; } = "";
    public string RecordedBy { get; set; } = "";
}

public class Contract
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public ContractType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Notes { get; set; } = "";
    public bool IsCurrent { get; set; } = true;

    public static Contract Create(int employeeId, ContractType type, DateTime start, DateTime? end, string? notes)
    {
        if (!Enum.IsDefined(type)||start.Year<1900||start.Year>9998||end?.Year>9998||employeeId<=0)throw new DomainException("err.contractDates");
        if (type == ContractType.FixedTerm && (end is null || end.Value.Date <= start.Date)) throw new DomainException("err.contractDates");
        return new Contract { EmployeeId = employeeId, Type = type, StartDate = start.Date, EndDate = type == ContractType.Permanent ? null : end?.Date, Notes = notes?.Trim() ?? "" };
    }

    public bool ExpiresWithin(DateTime today, int days) =>
        IsCurrent && EndDate is { } e && e.Date >= today.Date && e.Date <= today.Date.AddDays(days);

    public Contract Renew(DateTime newEnd)
    {
        if (EndDate is null) throw new DomainException("err.contractPermanent");
        if (!IsCurrent || newEnd.Year>9998 || newEnd.Date <= EndDate.Value.Date) throw new DomainException("err.contractDates");
        IsCurrent = false;
        return new Contract { EmployeeId = EmployeeId, Type = Type, StartDate = EndDate.Value.AddDays(1), EndDate = newEnd.Date, Notes = "Renewal" };
    }
}
