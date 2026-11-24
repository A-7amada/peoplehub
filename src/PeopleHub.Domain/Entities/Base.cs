namespace PeopleHub.Domain.Entities;

public class DomainException(string code) : Exception(code);

public static class Roles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string HRManager = "HRManager";
    public const string HRSpecialist = "HRSpecialist";
    public const string AttendanceOfficer = "AttendanceOfficer";
    public const string PayrollOfficer = "PayrollOfficer";
    public const string FinanceManager = "FinanceManager";
    public const string DepartmentManager = "DepartmentManager";
    public const string TopManagement = "TopManagement";
    public const string Employee = "Employee";

    public static readonly string[] All = [SystemAdmin, HRManager, HRSpecialist, AttendanceOfficer, PayrollOfficer,
        FinanceManager, DepartmentManager, TopManagement, Employee];

    /// <summary>Roles that may see every employee (data-level permission).</summary>
    public const string Company = $"{SystemAdmin},{HRManager},{HRSpecialist},{AttendanceOfficer},{PayrollOfficer},{FinanceManager},{TopManagement}";
    public const string HR = $"{SystemAdmin},{HRManager},{HRSpecialist}";
}

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    /// <summary>Comma separated roles (a user can hold several).</summary>
    public string RoleList { get; set; } = Roles.Employee;
    public int? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? TotpSecret { get; set; }
    public bool TotpEnabled { get; set; }
    public string[] RoleArray => RoleList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TitleKey { get; set; } = "";
    public string Details { get; set; } = "";
    public string? Link { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string User { get; set; } = "";
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Details { get; set; } = "";
}

public class Holiday
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = "";
    public string NameAr { get; set; } = "";
}
