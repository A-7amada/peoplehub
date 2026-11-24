namespace PeopleHub.Domain.Entities;

/// <summary>A role is data, not code: admins create roles and tick the permissions each one holds.</summary>
public class Role
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameAr { get; set; } = "";
    /// <summary>System roles cannot be deleted; SystemAdmin always holds every permission.</summary>
    public bool IsSystem { get; set; }
}

public class RolePermission
{
    public int Id { get; set; }
    public string RoleCode { get; set; } = "";
    public string Permission { get; set; } = "";
}

public record PermDef(string Key, string Group, string En, string Ar);

/// <summary>The permission catalog: every page or action that can be granted to a role.</summary>
public static class Perm
{
    public const string ScopeCompany = "scope.company", ScopeTeam = "scope.team",
        EmpManage = "emp.manage", EmpPay = "emp.pay", EmpSalary = "emp.salary", OrgManage = "org.manage",
        AttManage = "attendance.manage", AttTeam = "attendance.team", LeaveAdmin = "leave.admin", AdvManage = "advances.manage",
        PayView = "payroll.view", PayPrepare = "payroll.prepare", PayHr = "payroll.hrReview", PayFinance = "payroll.financeApprove",
        PayFinalize = "payroll.finalize", PayReopen = "payroll.reopen", PaySendBack = "payroll.sendBack", PayExport = "payroll.export", PaySlips = "payroll.slips",
        Reports = "reports.view", ReportsFin = "reports.financial", Audit = "audit.view",
        AdmUsers = "admin.users", AdmRoles = "admin.roles", AdmFlows = "admin.workflows";

    public static readonly PermDef[] Catalog =
    [
        new(ScopeCompany, "Data scope", "See every employee's data (company-wide)", "رؤية بيانات كل الموظفين (على مستوى الشركة)"),
        new(ScopeTeam, "Data scope", "See own team and department", "رؤية فريقه وإدارته"),
        new(EmpManage, "Employees", "Create and edit employees, contracts, history", "إضافة وتعديل الموظفين والعقود والسجل"),
        new(EmpPay, "Employees", "Set salary components", "تحديد مكونات الراتب"),
        new(EmpSalary, "Employees", "View salaries", "عرض الرواتب"),
        new(OrgManage, "Organization", "Manage departments, branches, job titles", "إدارة الإدارات والفروع والمسميات"),
        new(AttManage, "Attendance", "Manual entries and excuses", "إدخال يدوي وقبول الأعذار"),
        new(AttTeam, "Attendance", "Daily attendance board", "لوحة الحضور اليومية"),
        new(LeaveAdmin, "Leave", "Adjust balances, cancel any request", "تعديل الأرصدة وإلغاء أي طلب"),
        new(AdvManage, "Payroll", "Manage and settle advances", "إدارة وتسوية السلف"),
        new(PayView, "Payroll", "View payroll runs", "عرض دورات المرتبات"),
        new(PayPrepare, "Payroll", "Create and recalculate runs", "إنشاء وإعادة حساب الدورات"),
        new(PayHr, "Payroll", "HR review of a run", "مراجعة الموارد البشرية للدورة"),
        new(PayFinance, "Payroll", "Finance approval of a run", "اعتماد المالية للدورة"),
        new(PayFinalize, "Payroll", "Final approval (finalize)", "الاعتماد النهائي للدورة"),
        new(PayReopen, "Payroll", "Reopen a finalized run", "إعادة فتح دورة معتمدة"),
        new(PaySendBack, "Payroll", "Send a run back", "إرجاع الدورة للتعديل"),
        new(PayExport, "Payroll", "Bank transfer file", "ملف التحويل البنكي"),
        new(PaySlips, "Payroll", "View anyone's payslip", "عرض قسيمة أي موظف"),
        new(Reports, "Reports", "Reports and analytics", "التقارير والتحليلات"),
        new(ReportsFin, "Reports", "Financial reports", "التقارير المالية"),
        new(Audit, "Administration", "Audit log", "سجل التدقيق"),
        new(AdmUsers, "Administration", "Manage users", "إدارة المستخدمين"),
        new(AdmRoles, "Administration", "Manage roles and permissions", "إدارة الأدوار والصلاحيات"),
        new(AdmFlows, "Administration", "Configure approval workflows", "ضبط مسارات الموافقات"),
    ];

    /// <summary>Starting permissions of the built-in roles. Admins can change all of these in the UI.</summary>
    public static readonly Dictionary<string, string[]> Defaults = new()
    {
        [Roles.HRManager] = [ScopeCompany, EmpManage, EmpPay, EmpSalary, OrgManage, AttManage, AttTeam, LeaveAdmin, PayView, PayHr, PaySendBack, PaySlips, Reports, ReportsFin, Audit],
        [Roles.HRSpecialist] = [ScopeCompany, EmpManage, OrgManage, AttManage, AttTeam, LeaveAdmin, Reports],
        [Roles.AttendanceOfficer] = [ScopeCompany, AttManage, AttTeam, Reports],
        [Roles.PayrollOfficer] = [ScopeCompany, EmpPay, EmpSalary, AdvManage, PayView, PayPrepare, PayExport, PaySlips, Reports, ReportsFin],
        [Roles.FinanceManager] = [ScopeCompany, EmpSalary, AdvManage, PayView, PayFinance, PaySendBack, PayExport, PaySlips, Reports, ReportsFin],
        [Roles.DepartmentManager] = [ScopeTeam, AttTeam, Reports],
        [Roles.TopManagement] = [ScopeCompany, AttTeam, PayView, PayFinalize, Reports, ReportsFin, Audit],
        [Roles.Employee] = [],
    };
}

/// <summary>
/// In-memory copy of role permissions and names. Reloaded after every admin edit, so changes apply at once.
/// A single static instance is enough for this single-process demo.
/// </summary>
public static class PermissionCache
{
    private static volatile Dictionary<string, HashSet<string>> _perms = new();
    private static volatile Dictionary<string, (string En, string Ar)> _names = new();

    public static void Load(IEnumerable<Role> roles, IEnumerable<RolePermission> perms)
    {
        _names = roles.ToDictionary(r => r.Code, r => (r.Name, r.NameAr));
        _perms = perms.GroupBy(p => p.RoleCode).ToDictionary(g => g.Key, g => g.Select(p => p.Permission).ToHashSet());
    }

    public static bool Has(IEnumerable<string> roles, string perm)
    {
        foreach (var r in roles)
        {
            if (r == Roles.SystemAdmin) return true;
            if (_perms.TryGetValue(r, out var set) && set.Contains(perm)) return true;
        }
        return false;
    }

    public static bool RoleHas(string role, string perm) => role == Roles.SystemAdmin || (_perms.TryGetValue(role, out var s) && s.Contains(perm));
    public static (string En, string Ar)? NameOf(string code) => _names.TryGetValue(code, out var n) ? n : null;
}

public enum ApproverType { DirectManager = 0, Role = 1 }

/// <summary>One approval cycle per request kind (Leave, Overtime, Correction, Advance).</summary>
public class WorkflowDefinition
{
    public int Id { get; set; }
    public string Kind { get; set; } = "";
    public string? Description { get; set; }
    public List<WorkflowStep> Steps { get; set; } = [];
}

public class WorkflowStep
{
    public int Id { get; set; }
    public int WorkflowId { get; set; }
    public int Order { get; set; }
    public ApproverType Approver { get; set; }
    /// <summary>Role code when Approver is Role.</summary>
    public string? RoleCode { get; set; }
    /// <summary>
    /// Optional condition: the step applies only when the request is bigger than this
    /// (leave: days, overtime: hours, advance: amount). Unpaid leave always applies it. Empty means always.
    /// </summary>
    public decimal? Threshold { get; set; }
    /// <summary>The route token stored on requests: "Manager" or the role code.</summary>
    public string Token => Approver == ApproverType.DirectManager ? ApprovalStage.Manager : RoleCode ?? "";
}

/// <summary>Builds the route of a new request from the configured steps. Pure, so it is unit-tested.</summary>
public static class RoutePlanner
{
    public static string Build(IEnumerable<WorkflowStep> steps, Employee e, decimal size, bool alwaysEscalate = false)
    {
        var ordered = steps.OrderBy(s => s.Order).ToList();
        var route = new List<string>();
        foreach (var s in ordered)
        {
            if (s.Approver == ApproverType.DirectManager && e.ManagerId == null) continue;      // no manager: skip
            if (s.Threshold is { } t && !(size > t || alwaysEscalate)) continue;               // below the threshold
            if (!route.Contains(s.Token)) route.Add(s.Token);
        }
        if (route.Count == 0)
        {
            var fallback = ordered.FirstOrDefault(s => s.Approver == ApproverType.Role)?.Token ?? Roles.HRManager;
            route.Add(fallback);
        }
        return string.Join(',', route);
    }
}
