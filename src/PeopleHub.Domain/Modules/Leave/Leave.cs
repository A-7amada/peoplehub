namespace PeopleHub.Domain.Entities;

public class LeaveType
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameAr { get; set; } = "";
    public int DefaultDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool DeductsBalance { get; set; } = true;
    public bool RequiresDocument { get; set; }
    public int MaxCarryOver { get; set; }
    /// <summary>Past start dates are allowed (sick and emergency leave).</summary>
    public bool AllowBackdated { get; set; }
}

/// <summary>ADR-0022: editable policy for one leave type. While Enabled is false the type keeps using DefaultDays and MaxCarryOver and the policy is preview only.</summary>
public class LeavePolicy
{
    public int Id { get; set; }
    public int LeaveTypeId { get; set; }
    public bool Enabled { get; set; }
    /// <summary>Months of service before the type is available.</summary>
    public int WaitingMonths { get; set; } = 6;
    /// <summary>"Annual" (whole grant at the entitlement date) or "Monthly" (entitlement / 12 per full month).</summary>
    public string Accrual { get; set; } = "Annual";
    public bool ProrateFirstYear { get; set; } = true;
    /// <summary>Carried days lapse this many months after year end. 0 means they never lapse.</summary>
    public int CarryExpiryMonths { get; set; } = 3;
    public bool AllowHalfDay { get; set; } = true;
    public bool AllowHourly { get; set; }
    public int HoursPerDay { get; set; } = 8;
}

/// <summary>One row of the entitlement table: days granted once the employee reaches the service years, or the age when FromAge is set. The highest matching row wins.</summary>
public class LeaveEntitlementTier
{
    public int Id { get; set; }
    public int LeaveTypeId { get; set; }
    public int FromServiceYears { get; set; }
    public int? FromAge { get; set; }
    public int Days { get; set; }
}

public class LeaveBalance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public int Year { get; set; }
    public int Entitled { get; set; }
    public int CarriedOver { get; set; }
    public int Used { get; set; }
    public int Remaining => Entitled + CarriedOver - Used;

    /// <summary>Opening balance of a new year: unused days carry over up to the type's maximum.</summary>
    public static LeaveBalance Rollover(LeaveBalance old, LeaveType type) => new()
    {
        EmployeeId = old.EmployeeId, LeaveTypeId = old.LeaveTypeId, Year = old.Year + 1,
        Entitled = type.DefaultDays, CarriedOver = Math.Min(Math.Max(0, old.Remaining), type.MaxCarryOver)
    };
}

public class LeaveRequest : Approvable
{
    public int LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Days { get; set; }
    public string? Reason { get; set; }
    public string? DocumentName { get; set; }

    public static LeaveRequest Create(Employee e, LeaveType type, LeaveBalance? balance, DateTime start, DateTime end,
        string? reason, string? document, DateTime today, IEnumerable<DateTime>? holidays = null)
    {
        if (!e.IsWorking) throw new DomainException("err.terminated");
        if (end.Date < start.Date) throw new DomainException("err.dates");
        if (start.Date < today.Date && !type.AllowBackdated) throw new DomainException("err.past");
        var days = AttendanceCalculator.CountWorkingDays(start, end, holidays);
        if (days == 0) throw new DomainException("err.noWorkingDays");
        if (type.RequiresDocument && string.IsNullOrWhiteSpace(document)) throw new DomainException("err.docRequired");
        if (type.DeductsBalance && (balance == null || days > balance.Remaining)) throw new DomainException("err.balance");
        // Short paid leave stops at the manager. Longer or unpaid leave also needs HR.
        var route = days > 2 || !type.IsPaid ? BuildRoute(e, ApprovalStage.HR) : BuildRoute(e);
        return new LeaveRequest { EmployeeId = e.Id, LeaveTypeId = type.Id, LeaveType = type, StartDate = start.Date, EndDate = end.Date,
            Days = days, Reason = reason?.Trim(), DocumentName = document?.Trim(), Route = route };
    }

    public bool Overlaps(LeaveRequest o) =>
        o.Id != Id && o.Status is RequestStatus.Pending or RequestStatus.Approved && o.StartDate <= EndDate && StartDate <= o.EndDate;

    /// <summary>Called when the last approval stage is passed.</summary>
    public void ApplyToBalance(LeaveBalance? balance)
    {
        if (LeaveType is { DeductsBalance: true })
        {
            if (balance == null || Days > balance.Remaining) throw new DomainException("err.balance");
            balance.Used += Days;
        }
    }

    public void CancelApproved(LeaveBalance? balance, DateTime today, DateTime now)
    {
        if (Status != RequestStatus.Approved) throw new DomainException("err.notPending");
        if (StartDate.Date <= today.Date) throw new DomainException("err.alreadyStarted");
        if (LeaveType is { DeductsBalance: true })
        {
            if (balance == null || Days <= 0 || balance.Used < Days) throw new DomainException("err.balance");
            balance.Used -= Days;
        }
        Status = RequestStatus.Cancelled;
        DecidedAt = now;
    }
}
