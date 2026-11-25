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
    public const string EmpIdentity="emp.identity",EmpBank="emp.bank",EmpBankEdit="emp.bank.edit",LeaveEvidence="leave.evidence";
    public const string PayAdjust="payroll.adjust.references";
    public const string PayReconcile="payroll.reconcile";
    public const string RelationsManage="hr.relations.manage";
    public const string BenefitsManage="hr.benefits.manage";
    public const string FeedbackIdentity="perf.feedback.identity", FeedbackComments="perf.feedback.comments", FeedbackAggregate="perf.feedback.aggregate";
    public const string ScopeCompany = "scope.company", ScopeTeam = "scope.team",
        EmpManage = "emp.manage", EmpPay = "emp.pay", EmpSalary = "emp.salary", OrgManage = "org.manage",
        AttManage = "attendance.manage", AttTeam = "attendance.team", LeaveAdmin = "leave.admin", AdvManage = "advances.manage",
        PayView = "payroll.view", PayPrepare = "payroll.prepare", PayHr = "payroll.hrReview", PayFinance = "payroll.financeApprove",
        PayFinalize = "payroll.finalize", PayReopen = "payroll.reopen", PaySendBack = "payroll.sendBack", PayExport = "payroll.export", PaySlips = "payroll.slips",
        Reports = "reports.view", ReportsFin = "reports.financial", Audit = "audit.view",
        AdmUsers = "admin.users", AdmRoles = "admin.roles", AdmFlows = "admin.workflows", AdmSettings = "admin.settings", TalentManage = "talent.manage";

    public static readonly PermDef[] Catalog =
    [
        new(EmpIdentity,"Sensitive fields","View and edit identity, birth date, address and insurance number","عرض وتعديل الهوية والميلاد والعنوان والرقم التأميني"),
        new(EmpBank,"Sensitive fields","View employee bank details","عرض بيانات البنك للموظف"),
        new(EmpBankEdit,"Sensitive fields","Edit employee bank details (employee editing also required)","تعديل بيانات البنك (مع صلاحية تعديل الموظفين)"),
        new(LeaveEvidence,"Sensitive fields","View private leave reasons and supporting documents","عرض أسباب الإجازات الخاصة ومستنداتها"),
        new(PayAdjust,"Payroll","Manage referenced payroll correction proposals","إدارة مقترحات تصحيح المرتبات المرجعية"),
        new(PayReconcile,"Payroll","Record and view manual bank reconciliation evidence","تسجيل وعرض أدلة مطابقة البنك اليدوية"),
        new(RelationsManage,"Private HR","Manage all confidential employee relations cases","إدارة جميع قضايا علاقات الموظفين السرية"),
        new(BenefitsManage,"Private HR","Manage employee and dependent benefits","إدارة مزايا الموظفين والمعالين"),
        new(FeedbackIdentity,"Performance privacy","View 360 reviewer identity","عرض هوية مقيمي 360"),
        new(FeedbackComments,"Performance privacy","View individual 360 comments and scores","عرض تعليقات ودرجات مقيمي 360"),
        new(FeedbackAggregate,"Performance privacy","View 360 aggregates (threshold applies)","عرض متوسطات 360 بعد اكتمال الحد الأدنى"),
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
        new(AdmSettings, "Administration", "System settings (attendance source, device, email, policies)", "إعدادات النظام (مصدر الحضور والجهاز والبريد والسياسات)"),
        new(TalentManage, "Talent", "Manage recruitment, performance reviews and training", "إدارة التوظيف وتقييم الأداء والتدريب"),
        new(AdmFlows, "Administration", "Configure approval workflows", "ضبط مسارات الموافقات"),
    ];

    /// <summary>Starting permissions of the built-in roles. Admins can change all of these in the UI.</summary>
    public static readonly Dictionary<string, string[]> Defaults = new()
    {
        [Roles.HRManager] = [ScopeCompany, EmpManage, EmpPay, EmpSalary, OrgManage, AttManage, AttTeam, LeaveAdmin, PayView, PayHr, PaySendBack, PaySlips, Reports, ReportsFin, Audit, TalentManage],
        [Roles.HRSpecialist] = [ScopeCompany, EmpManage, OrgManage, AttManage, AttTeam, LeaveAdmin, Reports, TalentManage],
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

public enum ApproverType { DirectManager = 0, Role = 1, DepartmentManager = 2, SkipLevelManager = 3 }

/// <summary>One approval cycle per request kind (Leave, Overtime, Correction, Advance).</summary>
public class WorkflowDefinition
{
    public int Id { get; set; }
    public string Kind { get; set; } = "";
    public string? Description { get; set; }
    public List<WorkflowStep> Steps { get; set; } = [];
}

public enum ApprovalRule { AnyRole = 0, CapturedAny = 1, All = 2, Quorum = 3 }

public class WorkflowStep
{
    public int Id { get; set; }
    public int WorkflowId { get; set; }
    public ApprovalRule Rule { get; set; }
    public int? RequiredCount { get; set; }
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
    public string Token => Approver switch {ApproverType.DirectManager=>ApprovalStage.Manager,ApproverType.DepartmentManager=>"DepartmentManager",ApproverType.SkipLevelManager=>"SkipLevelManager",_=>RoleCode??""};
}

/// <summary>Builds the route of a new request from the configured steps. Pure, so it is unit-tested.</summary>
public static class RoutePlanner
{
    public static string Build(IEnumerable<WorkflowStep> steps, Employee e, decimal size, bool alwaysEscalate = false, IReadOnlyDictionary<int,int?>? managers = null)
    {
        var ordered = steps.OrderBy(s => s.Order).ToList();
        var route = new List<string>();
        foreach (var s in ordered)
        {
            if (s.Approver == ApproverType.DirectManager && e.ManagerId == null) continue;      // no manager: skip
            if (s.Threshold is { } t && !(size > t || alwaysEscalate)) continue;               // below the threshold
            var token=s.Token;
            if(s.Approver==ApproverType.DepartmentManager)
            {
                var manager=e.Department?.ManagerId;
                if(manager is not >0||manager==e.Id)throw new DomainException("err.departmentApprover");
                token=ApprovalStage.EmployeeToken(manager.Value);
            }
            if(s.Approver==ApproverType.SkipLevelManager)
            {
                // The manager's manager, captured now. A missing, self or looping chain blocks the request instead of skipping the step.
                int? skip=e.ManagerId is {} m&&managers!=null&&managers.TryGetValue(m,out var up)?up:null;
                if(skip is not >0||skip==e.Id||skip==e.ManagerId)throw new DomainException("err.skipLevelApprover");
                token=ApprovalStage.EmployeeToken(skip.Value);
            }
            if (!route.Contains(token)) route.Add(token);
        }
        if (route.Count == 0)
        {
            var fallback = ordered.FirstOrDefault(s => s.Approver == ApproverType.Role)?.Token ?? Roles.HRManager;
            route.Add(fallback);
        }
        return string.Join(',', route);
    }
}

/// <summary>Temporary hand-over of approval rights (for example while the manager is on leave).</summary>
public class Delegation
{
    public int Id { get; set; }
    public int FromEmployeeId { get; set; }
    public int ToEmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool Revoked { get; set; }
    public bool IsActiveOn(DateTime day) => !Revoked && StartDate.Date <= day.Date && day.Date <= EndDate.Date;
}

/// <summary>Uploaded supporting document (PDF or image up to 2 MB), kept in the database.</summary>
public class Attachment
{
    public int Id { get; set; }
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public byte[] Data { get; set; } = [];
    public int OwnerEmployeeId { get; set; }
    public string UploadedBy { get; set; } = "";
    public DateTime At { get; set; } = DateTime.UtcNow;
}

/// <summary>Content-addressed immutable workflow definition observed when routing a request.</summary>
public class WorkflowRevision
{
 public int Id {get;set;}
 public string Kind {get;set;}="";
 public string Hash {get;set;}="";
 public string DefinitionJson {get;set;}="";
 public DateTime ObservedAt {get;set;}
}
