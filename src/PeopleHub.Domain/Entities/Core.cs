namespace PeopleHub.Domain.Entities;

public class DomainException(string code) : Exception(code);

public enum EmployeeStatus { Active = 0, OnLeave = 1, Terminated = 2 }
public enum LeaveType { Annual = 0, Sick = 1, Unpaid = 2 }
public enum LeaveStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NameAr { get; set; } = "";
}

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public string Email { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public DateTime HireDate { get; set; }
    public decimal MonthlySalary { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public int AnnualLeaveBalance { get; set; } = 21;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(FullName)) throw new DomainException("err.name");
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@')) throw new DomainException("err.email");
        if (MonthlySalary < 0) throw new DomainException("err.salary");
        Email = Email.Trim().ToLowerInvariant();
    }

    public void Terminate()
    {
        if (Status == EmployeeStatus.Terminated) throw new DomainException("err.alreadyTerminated");
        Status = EmployeeStatus.Terminated;
    }
}

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Days { get; set; }
    public string? Reason { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }

    /// <summary>Working days between two dates inclusive. The Egyptian weekend (Friday, Saturday) is excluded.</summary>
    public static int CountWorkingDays(DateTime start, DateTime end)
    {
        var n = 0;
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            if (d.DayOfWeek is not (DayOfWeek.Friday or DayOfWeek.Saturday)) n++;
        return n;
    }

    public static LeaveRequest Create(Employee employee, LeaveType type, DateTime start, DateTime end, string? reason, DateTime today)
    {
        if (employee.Status == EmployeeStatus.Terminated) throw new DomainException("err.terminated");
        if (end < start) throw new DomainException("err.dates");
        if (start.Date < today.Date && type != LeaveType.Sick) throw new DomainException("err.past");
        var days = CountWorkingDays(start, end);
        if (days == 0) throw new DomainException("err.noWorkingDays");
        if (type == LeaveType.Annual && days > employee.AnnualLeaveBalance) throw new DomainException("err.balance");
        return new LeaveRequest { EmployeeId = employee.Id, Type = type, StartDate = start.Date, EndDate = end.Date, Days = days, Reason = reason?.Trim() };
    }

    public bool Overlaps(LeaveRequest o) =>
        o.Status is LeaveStatus.Pending or LeaveStatus.Approved && o.StartDate <= EndDate && StartDate <= o.EndDate;

    public void Approve(Employee employee)
    {
        if (Status != LeaveStatus.Pending) throw new DomainException("err.notPending");
        if (Type == LeaveType.Annual)
        {
            if (Days > employee.AnnualLeaveBalance) throw new DomainException("err.balance");
            employee.AnnualLeaveBalance -= Days;
        }
        Status = LeaveStatus.Approved;
        DecidedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != LeaveStatus.Pending) throw new DomainException("err.notPending");
        Status = LeaveStatus.Rejected;
        DecidedAt = DateTime.UtcNow;
    }

    public void Cancel(Employee employee)
    {
        if (Status is LeaveStatus.Rejected or LeaveStatus.Cancelled) throw new DomainException("err.notPending");
        if (Status == LeaveStatus.Approved && Type == LeaveType.Annual) employee.AnnualLeaveBalance += Days;
        Status = LeaveStatus.Cancelled;
        DecidedAt = DateTime.UtcNow;
    }
}

public static class Roles
{
    public const string HR = "HR";
    public const string Employee = "Employee";
}

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.Employee;
    public int? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
}
