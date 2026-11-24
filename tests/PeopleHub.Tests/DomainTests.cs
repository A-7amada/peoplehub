using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Security;
using Xunit;

namespace PeopleHub.Tests;

public class AttendanceTests
{
    private static readonly Shift Day = new() { Start = new(9, 0, 0), End = new(17, 0, 0), GraceMinutes = 10, MaxLateMinutes = 120 };
    private static readonly DateTime D = new(2026, 11, 22); // Sunday

    [Fact] public void OnTime_WithinGrace_IsPresent() => Assert.Equal(AttendanceStatus.Present, AttendanceCalculator.Calculate(Day, D, D.AddHours(9).AddMinutes(8), D.AddHours(17)).Status);
    [Fact] public void Late_CountsMinutes() { var r = AttendanceCalculator.Calculate(Day, D, D.AddHours(9).AddMinutes(25), D.AddHours(17)); Assert.Equal(25, r.LateMinutes); Assert.Equal(AttendanceStatus.Late, r.Status); }
    [Fact] public void TooLate_CountsAsAbsent() => Assert.Equal(AttendanceStatus.Absent, AttendanceCalculator.Calculate(Day, D, D.AddHours(11).AddMinutes(30), null).Status);
    [Fact] public void EarlyLeave_IsMeasured() => Assert.Equal(45, AttendanceCalculator.Calculate(Day, D, D.AddHours(9), D.AddHours(16).AddMinutes(15)).EarlyLeaveMinutes);
    [Fact] public void Overtime_NeedsThreshold() { Assert.Equal(0, AttendanceCalculator.Calculate(Day, D, D.AddHours(9), D.AddHours(17).AddMinutes(20)).OvertimeMinutes); Assert.Equal(60, AttendanceCalculator.Calculate(Day, D, D.AddHours(9), D.AddHours(18)).OvertimeMinutes); }
    [Fact] public void CheckoutBeforeCheckin_Throws() => Assert.Throws<DomainException>(() => AttendanceCalculator.Calculate(Day, D, D.AddHours(10), D.AddHours(9)));
    [Fact] public void NoCheckIn_IsAbsent() => Assert.Equal(AttendanceStatus.Absent, AttendanceCalculator.Calculate(Day, D, null, null).Status);
    [Fact] public void WorkingDays_SkipFridaySaturdayAndHolidays()
    {
        // Sun 1 Nov to Sat 7 Nov 2026 = 5 working days; a holiday on Tuesday leaves 4.
        Assert.Equal(5, AttendanceCalculator.CountWorkingDays(new(2026, 11, 1), new(2026, 11, 7)));
        Assert.Equal(4, AttendanceCalculator.CountWorkingDays(new(2026, 11, 1), new(2026, 11, 7), [new DateTime(2026, 11, 3)]));
    }
    [Fact] public void Overtime_Request_WeekendPaysDouble()
    {
        var e = new Employee { Id = 1, ManagerId = 2 };
        Assert.Equal(1.5m, OvertimeRequest.Create(e, new(2026, 11, 22), 2, "release", new(2026, 11, 24)).Multiplier);
        Assert.Equal(2m, OvertimeRequest.Create(e, new(2026, 11, 20), 2, "release", new(2026, 11, 24)).Multiplier); // Friday
        Assert.Throws<DomainException>(() => OvertimeRequest.Create(e, new(2026, 11, 22), 9, "x", new(2026, 11, 24)));
    }
}

public class LeaveTests
{
    private static readonly Employee Emp = new() { Id = 1, ManagerId = 9, Status = EmployeeStatus.Active };
    private static readonly LeaveType Annual = new() { Id = 1, DeductsBalance = true, IsPaid = true };
    private static readonly LeaveType Unpaid = new() { Id = 4, DeductsBalance = false, IsPaid = false };
    private static readonly LeaveType Sick = new() { Id = 2, RequiresDocument = true, AllowBackdated = true, DeductsBalance = false };
    private static readonly DateTime Today = new(2026, 11, 24);
    private static LeaveBalance Bal(int entitled, int used = 0) => new() { Entitled = entitled, Used = used };

    [Fact] public void ShortPaidLeave_GoesToManagerOnly() => Assert.Equal("Manager", LeaveRequest.Create(Emp, Annual, Bal(21), new(2026, 11, 30), new(2026, 12, 1), null, null, Today).Route);
    [Fact] public void LongLeave_AlsoNeedsHr() => Assert.Equal($"Manager,{ApprovalStage.HR}", LeaveRequest.Create(Emp, Annual, Bal(21), new(2026, 11, 29), new(2026, 12, 3), null, null, Today).Route);
    [Fact] public void UnpaidLeave_AlwaysNeedsHr() => Assert.Contains("HR", LeaveRequest.Create(Emp, Unpaid, null, new(2026, 11, 30), new(2026, 11, 30), null, null, Today).Route);
    [Fact] public void InsufficientBalance_Throws() => Assert.Throws<DomainException>(() => LeaveRequest.Create(Emp, Annual, Bal(21, 20), new(2026, 11, 29), new(2026, 12, 3), null, null, Today));
    [Fact] public void WeekendOnly_Throws() => Assert.Throws<DomainException>(() => LeaveRequest.Create(Emp, Annual, Bal(21), new(2026, 11, 27), new(2026, 11, 28), null, null, Today));
    [Fact] public void PastStart_Throws_ExceptSick() { Assert.Throws<DomainException>(() => LeaveRequest.Create(Emp, Annual, Bal(21), new(2026, 11, 22), new(2026, 11, 22), null, null, Today)); LeaveRequest.Create(Emp, Sick, null, new(2026, 11, 22), new(2026, 11, 22), null, "note.pdf", Today); }
    [Fact] public void SickLeave_NeedsDocument() => Assert.Throws<DomainException>(() => LeaveRequest.Create(Emp, Sick, null, new(2026, 11, 29), new(2026, 11, 29), null, null, Today));
    [Fact] public void Approval_DeductsBalance_AndCancelRestores()
    {
        var b = Bal(21);
        var r = LeaveRequest.Create(Emp, Annual, b, new(2026, 12, 6), new(2026, 12, 8), null, null, Today);
        r.ApplyToBalance(b); Assert.Equal(3, b.Used);
        r.Status = RequestStatus.Approved; r.CancelApproved(b, Today, DateTime.UtcNow);
        Assert.Equal(0, b.Used); Assert.Equal(RequestStatus.Cancelled, r.Status);
    }
    [Fact] public void Rollover_CapsCarryOver() => Assert.Equal(7, LeaveBalance.Rollover(new LeaveBalance { Entitled = 21, Used = 2, Year = 2026 }, new LeaveType { DefaultDays = 21, MaxCarryOver = 7 }).CarriedOver);
}

public class ApprovalTests
{
    [Fact] public void Route_SkipsManagerWhenNone() => Assert.Equal(ApprovalStage.HR, Approvable.BuildRoute(new Employee(), ApprovalStage.HR));
    [Fact] public void Stages_AdvanceInOrder_ThenApprove()
    {
        var r = new OvertimeRequest { Route = $"Manager,{ApprovalStage.HR}" };
        Assert.False(r.ApproveStage(DateTime.UtcNow)); Assert.Equal(ApprovalStage.HR, r.CurrentStage);
        Assert.True(r.ApproveStage(DateTime.UtcNow)); Assert.Equal(RequestStatus.Approved, r.Status);
    }
    [Fact] public void Reject_NeedsReason() { var r = new OvertimeRequest { Route = "Manager" }; Assert.Throws<DomainException>(() => r.RejectRequest(" ", DateTime.UtcNow)); r.RejectRequest("busy", DateTime.UtcNow); Assert.Equal(RequestStatus.Rejected, r.Status); }
    [Fact] public void DecidedRequest_CannotBeApprovedAgain() { var r = new OvertimeRequest { Route = "Manager" }; r.ApproveStage(DateTime.UtcNow); Assert.Throws<DomainException>(() => r.ApproveStage(DateTime.UtcNow)); }
    [Fact] public void Advance_Route_IsHrThenFinance_AndLimited()
    {
        var e = new Employee { Id = 1, BasicSalary = 10000 };
        Assert.Equal($"{ApprovalStage.HR},{ApprovalStage.Finance}", AdvanceRequest.Create(e, 5000, 5, 2026, 12, "x").Route);
        Assert.Throws<DomainException>(() => AdvanceRequest.Create(e, 31000, 5, 2026, 12, "x"));
        Assert.Throws<DomainException>(() => AdvanceRequest.Create(e, 1000, 30, 2026, 12, "x"));
    }
    [Fact] public void Advance_InstallmentsStopWhenRepaid() { var a = new AdvanceRequest { Amount = 3000, Installments = 3, StartYear = 2026, StartMonth = 11, Status = RequestStatus.Approved, Paid = 2500 }; Assert.Equal(500, a.InstallmentFor()); Assert.False(a.DueIn(2026, 10)); Assert.True(a.DueIn(2026, 12)); }
}

public class EmployeeTests
{
    [Fact] public void ChangeStatus_RecordsHistory() { var e = new Employee { Id = 3 }; var h = e.ChangeStatus(EmployeeStatus.Suspended, "hr", "x", DateTime.UtcNow); Assert.Equal(EmployeeStatus.Suspended, e.Status); Assert.Equal(EmployeeStatus.Active, h.From); }
    [Fact] public void FinalStatus_CannotChange() { var e = new Employee { Status = EmployeeStatus.Resigned }; Assert.Throws<DomainException>(() => e.ChangeStatus(EmployeeStatus.Active, "hr", null, DateTime.UtcNow)); }
    [Fact] public void Validate_RejectsBadData() { Assert.Throws<DomainException>(() => new Employee { FullName = "", Email = "a@b.c" }.Validate()); Assert.Throws<DomainException>(() => new Employee { FullName = "A", Email = "nope" }.Validate()); Assert.Throws<DomainException>(() => new Employee { FullName = "A", Email = "a@b.c", NationalId = "123" }.Validate()); }
    [Fact] public void Contract_FixedTermNeedsEnd_RenewExtends()
    {
        Assert.Throws<DomainException>(() => Contract.Create(1, ContractType.FixedTerm, new(2026, 1, 1), null, null));
        var c = Contract.Create(1, ContractType.FixedTerm, new(2026, 1, 1), new(2026, 12, 31), null);
        var n = c.Renew(new(2027, 12, 31)); Assert.False(c.IsCurrent); Assert.Equal(new DateTime(2027, 1, 1), n.StartDate);
        Assert.Throws<DomainException>(() => n.Renew(new(2027, 1, 1)));
    }
    [Fact] public void Contract_ExpiresWithin() { var c = Contract.Create(1, ContractType.FixedTerm, new(2026, 1, 1), new(2026, 12, 10), null); Assert.True(c.ExpiresWithin(new(2026, 11, 24), 30)); Assert.False(c.ExpiresWithin(new(2026, 11, 24), 10)); }
}

public class PayrollTests
{
    private static PayrollLine Calc(decimal basic, decimal allow = 0, decimal bonus = 0, int absent = 0, int late = 0, decimal ot = 0, decimal adv = 0, decimal other = 0)
    { var l = new PayrollLine(); PayrollCalculator.Apply(l, new PayrollInput(basic, allow, 0, bonus, absent, late, ot, adv, other)); return l; }

    [Fact] public void Net_EqualsGrossMinusDeductions() { var l = Calc(20000, 1000, 500, 1, 30, 4, 200, 150); Assert.Equal(l.Gross - l.TotalDeductions, l.Net); }
    [Fact] public void Absence_UsesDailyRate() => Assert.Equal(2000m, Calc(30000, absent: 2).AbsenceDeduction);
    [Fact] public void Overtime_UsesHourlyRate() => Assert.Equal(500m, Calc(24000, ot: 5).Overtime); // 24000/30/8 = 100 per hour
    [Fact] public void Insurance_IsCapped() => Assert.Equal(1595m, Calc(60000).Insurance);
    [Fact] public void Tax_IsProgressive() { Assert.Equal(0m, PayrollCalculator.Tax(4000)); Assert.Equal(500m, PayrollCalculator.Tax(10000)); Assert.Equal(1250m, PayrollCalculator.Tax(15000)); }
    [Fact] public void Late_DeductsPerMinute() => Assert.Equal(125m, Calc(30000, late: 60).LateDeduction); // hourly 125
    [Fact] public void Workflow_FollowsStages()
    {
        var r = new PayrollRun();
        Assert.Throws<DomainException>(() => r.FinanceApprove());
        r.Review(); r.FinanceApprove(); r.Finalize(); Assert.True(r.IsLocked);
        Assert.Throws<DomainException>(() => r.Review());
        r.MarkPaid(DateTime.UtcNow); Assert.Throws<DomainException>(() => r.Reopen("fix"));
    }
    [Fact] public void SendBack_And_Reopen_NeedReason()
    {
        var r = new PayrollRun(); r.Review();
        Assert.Throws<DomainException>(() => r.SendBack(""));
        r.SendBack("check overtime"); Assert.Equal(PayrollStatus.Draft, r.Status);
        r.Review(); r.FinanceApprove(); r.Finalize();
        Assert.Throws<DomainException>(() => r.Reopen(null)); r.Reopen("wrong bank file"); Assert.Equal(PayrollStatus.Draft, r.Status);
    }
}

public class SecurityTests
{
    [Fact] public void PasswordHash_RoundTrips_AndSalts() { var a = PasswordHasher.Hash("Demo123!"); var b = PasswordHasher.Hash("Demo123!"); Assert.NotEqual(a, b); Assert.True(PasswordHasher.Verify("Demo123!", a)); Assert.False(PasswordHasher.Verify("wrong", a)); }
}
