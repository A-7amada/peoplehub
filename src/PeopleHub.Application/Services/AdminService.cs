using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Domain.Security;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace PeopleHub.Application.Services;

/// <summary>Approval cycles: which steps each request kind goes through and who may act on each step.</summary>
public class WorkflowService(IRepository repo, ICurrentUser me, AuditService audit)
{
    public static readonly string[] Kinds = ["Leave", "Overtime", "Correction", "Advance"];

    public async Task<List<WorkflowDefinition>> ListAsync()
    {
        var wf = await repo.ToListAsync(repo.Query<WorkflowDefinition>());
        var steps = await repo.ToListAsync(repo.Query<WorkflowStep>());
        foreach (var w in wf) w.Steps = steps.Where(s => s.WorkflowId == w.Id).OrderBy(s => s.Order).ToList();
        return Kinds.Select(k => wf.FirstOrDefault(w => w.Kind == k)).Where(w => w != null).Select(w => w!).ToList();
    }

    /// <summary>Route for a new request. Falls back to the HR manager when a cycle is missing or empty.</summary>
    public async Task<string> RouteForAsync(string kind, Employee e, decimal size, bool alwaysEscalate = false)
    {
        var w = (await ListAsync()).FirstOrDefault(x => x.Kind == kind);
        return RoutePlanner.Build(w?.Steps ?? [], e, size, alwaysEscalate);
    }

    public async Task RouteRequestAsync(string kind,Approvable request,Employee employee,decimal size,bool alwaysEscalate=false)
    {
        var definition=(await ListAsync()).FirstOrDefault(x=>x.Kind==kind);
        var steps=definition?.Steps??[];
        var json=JsonSerializer.Serialize(new{Kind=kind,RoutingSemantics="captured-participant-rules-v3",Steps=steps.OrderBy(x=>x.Order).Select(x=>new{x.Order,x.Approver,x.RoleCode,x.Threshold,x.Rule,x.RequiredCount})});
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var route=RoutePlanner.Build(steps,employee,size,alwaysEscalate);
        var routed=new List<string>();
        var users=await repo.ToListAsync(repo.Query<AppUser>().Where(u=>u.IsActive));
        var employees=await repo.ToListAsync(repo.Query<Employee>());
        foreach(var token in route.Split(','))
        {
            var step=steps.FirstOrDefault(s=>s.Token==token);
            if(step!=null&&step.Rule!=ApprovalRule.AnyRole)
            {
                var roles=(step.RoleCode??"").Split('|');
                var roster=users.Where(u=>u.EmployeeId!=null&&u.EmployeeId!=employee.Id&&u.RoleArray.Intersect(roles).Any()).Select(u=>u.EmployeeId!.Value).Distinct().Where(id=>employees.Any(e=>e.Id==id&&e.IsWorking)).Order().ToArray();
                var count=step.Rule==ApprovalRule.All?roster.Length:step.Rule==ApprovalRule.CapturedAny?1:step.RequiredCount??0;
                if(roster.Length==0||count<1||count>roster.Length)throw new DomainException("err.approvalGroup");
                routed.Add(ApprovalStage.GroupToken(count,roster));
            }
            else routed.Add(token);
        }
        route=string.Join(',',routed);
        foreach(var token in route.Split(','))if(ApprovalStage.EmployeeId(token) is {} resolved)
        {
            var target=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e=>e.Id==resolved));
            if(target?.IsWorking!=true||!await repo.AnyAsync(repo.Query<AppUser>().Where(u=>u.EmployeeId==resolved&&u.IsActive)))throw new DomainException("err.departmentApprover");
        }
        if(!await repo.AnyAsync(repo.Query<WorkflowRevision>().Where(x=>x.Kind==kind&&x.Hash==hash)))repo.Add(new WorkflowRevision{Kind=kind,Hash=hash,DefinitionJson=json,ObservedAt=DateTime.UtcNow});
        request.Route=route;request.WorkflowRevisionHash=hash;
    }
    public Task<List<WorkflowRevision>> RevisionsAsync(){Require();return repo.ToListAsync(repo.Query<WorkflowRevision>().OrderByDescending(x=>x.ObservedAt).Take(100));}

    public async Task<HashSet<string>> RequiredApprovalCommentsAsync() => (await repo.ToListAsync(repo.Query<AppSetting>().Where(x=>x.Key.StartsWith("Workflow.ApprovalComment.") && x.Value=="true"))).Select(x=>x.Key["Workflow.ApprovalComment.".Length..]).Where(Kinds.Contains).ToHashSet();
    public async Task SetApprovalCommentAsync(string kind,bool required)
    {
        Require();if(!Kinds.Contains(kind))throw new DomainException("err.required");
        var key="Workflow.ApprovalComment."+kind;var setting=await repo.FirstOrDefaultAsync(repo.Query<AppSetting>().Where(x=>x.Key==key));
        if(setting==null){setting=new(){Key=key};repo.Add(setting);}setting.Value=required?"true":"false";audit.Log("ApprovalCommentPolicy","Workflow",kind,required?"Required for approvals, including pending requests":"Optional for approvals; rejection reason remains required");await repo.SaveAsync();
    }

    public async Task<Dictionary<string,int>> ReminderHoursAsync(){var values=await repo.ToListAsync(repo.Query<AppSetting>().Where(x=>x.Key.StartsWith("Workflow.ReminderHours.")));return Kinds.ToDictionary(k=>k,k=>int.TryParse(values.FirstOrDefault(x=>x.Key=="Workflow.ReminderHours."+k)?.Value,out var h)&&h>=1&&h<=8760?h:0);}
    public async Task SetReminderHoursAsync(string kind,int hours){Require();if(!Kinds.Contains(kind)||hours<0||hours>8760)throw new DomainException("err.required");var key="Workflow.ReminderHours."+kind;var v=await repo.FirstOrDefaultAsync(repo.Query<AppSetting>().Where(x=>x.Key==key));if(v==null){v=new(){Key=key};repo.Add(v);}v.Value=hours.ToString(System.Globalization.CultureInfo.InvariantCulture);audit.Log("ReminderPolicy","Workflow",kind,$"{hours} elapsed hours; 0 disables; applies to pending stages; private in-app only");await repo.SaveAsync();}
    private void Require() { if (!me.Can(Perm.AdmFlows)) throw new DomainException("err.forbidden"); }

    private async Task<WorkflowDefinition> LoadAsync(string kind) =>
        (await ListAsync()).FirstOrDefault(w => w.Kind == kind) ?? throw new DomainException("err.notFound");

    public async Task AddStepAsync(string kind, ApproverType type, string[]? roles, decimal? threshold, ApprovalRule rule=ApprovalRule.AnyRole,int? requiredCount=null)
    {
        Require();
        var w = await LoadAsync(kind);
        if(!Enum.IsDefined(type)||threshold is <0 or >100000000)throw new DomainException("err.required");
        if(!Enum.IsDefined(rule)||(rule!=ApprovalRule.AnyRole&&type!=ApproverType.Role)||(rule==ApprovalRule.Quorum&&(requiredCount is null or <1 or >1000))||(rule!=ApprovalRule.Quorum&&requiredCount!=null))throw new DomainException("err.approvalGroup");
        var codes = (roles ?? []).Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToArray();
        if (type == ApproverType.Role && codes.Length == 0) throw new DomainException("err.roleRequired");
        var knownRoles=(await repo.ToListAsync(repo.Query<Role>())).Select(r=>r.Code).ToHashSet();
        if(type==ApproverType.Role&&codes.Any(r=>!knownRoles.Contains(r)))throw new DomainException("err.roleRequired");
        if(type==ApproverType.DepartmentManager&&w.Steps.Any(s=>s.Approver==type))throw new DomainException("err.dupDepartmentManager");
        if (w.Steps.Count >= 6) throw new DomainException("err.maxSteps");
        if (type == ApproverType.DirectManager && w.Steps.Any(s => s.Approver == ApproverType.DirectManager)) throw new DomainException("err.dupManager");
        if(w.Steps.Any(s=>s.Approver==type&&type==ApproverType.Role&&(rule!=ApprovalRule.AnyRole||s.Rule!=ApprovalRule.AnyRole)&&s.RoleCode==string.Join('|',codes)))throw new DomainException("err.approvalGroup");
        var step = new WorkflowStep { Rule=rule,RequiredCount=requiredCount, WorkflowId = w.Id, Order = (w.Steps.Count == 0 ? 0 : w.Steps.Max(s => s.Order)) + 1, Approver = type,
            RoleCode = type == ApproverType.Role ? string.Join('|', codes) : null, Threshold = threshold is > 0 ? threshold : null };
        repo.Add(step);
        audit.Log("Update", "Workflow", w.Id, $"{kind}: added step {step.Token}");
        await repo.SaveAsync();
    }

    public async Task RemoveStepAsync(int stepId)
    {
        Require();
        var s = await repo.FirstOrDefaultAsync(repo.Query<WorkflowStep>().Where(x => x.Id == stepId)) ?? throw new DomainException("err.notFound");
        var w = (await ListAsync()).First(x => x.Id == s.WorkflowId);
        if (w.Steps.Count <= 1) throw new DomainException("err.minSteps");
        repo.Remove(s);
        audit.Log("Update", "Workflow", w.Id, $"{w.Kind}: removed step {s.Token}");
        await repo.SaveAsync();
    }

    public async Task MoveStepAsync(int stepId, int dir)
    {
        Require();
        var s = await repo.FirstOrDefaultAsync(repo.Query<WorkflowStep>().Where(x => x.Id == stepId)) ?? throw new DomainException("err.notFound");
        var w = (await ListAsync()).First(x => x.Id == s.WorkflowId);
        var i = w.Steps.FindIndex(x => x.Id == stepId); var j = i + (dir < 0 ? -1 : 1);
        if (j < 0 || j >= w.Steps.Count) return;
        (w.Steps[i].Order, w.Steps[j].Order) = (w.Steps[j].Order, w.Steps[i].Order);
        audit.Log("Update", "Workflow", w.Id, $"{w.Kind}: reordered steps");
        await repo.SaveAsync();
    }
}

/// <summary>Roles, their permissions and user accounts, all editable by an administrator.</summary>
public class AdminService(IRepository repo, ICurrentUser me, AuditService audit)
{
    private void Require(string perm) { if (!me.Can(perm)) throw new DomainException("err.forbidden"); }

    public static async Task ReloadCacheAsync(IRepository repo) =>
        PermissionCache.Load(await repo.ToListAsync(repo.Query<Role>()), await repo.ToListAsync(repo.Query<RolePermission>()));

    public Task<List<Role>> RolesAsync() => repo.ToListAsync(repo.Query<Role>().OrderByDescending(r => r.IsSystem).ThenBy(r => r.Id));
    public Task<List<RolePermission>> PermissionsAsync() => repo.ToListAsync(repo.Query<RolePermission>());

    /// <summary>Replaces the grants of every role except SystemAdmin with the submitted "role|permission" pairs.</summary>
    public async Task SavePermissionsAsync(IEnumerable<string> pairs)
    {
        Require(Perm.AdmRoles);
        var roles = (await RolesAsync()).Where(r => r.Code != Roles.SystemAdmin).Select(r => r.Code).ToHashSet();
        var valid = Perm.Catalog.Select(p => p.Key).ToHashSet();
        var wanted = pairs.Select(p => p.Split('|')).Where(a => a.Length == 2 && roles.Contains(a[0]) && valid.Contains(a[1])).Select(a => (a[0], a[1])).ToHashSet();
        var current = await PermissionsAsync();
        foreach (var c in current.Where(c => roles.Contains(c.RoleCode) && !wanted.Contains((c.RoleCode, c.Permission)))) repo.Remove(c);
        foreach (var w in wanted.Where(w => !current.Any(c => c.RoleCode == w.Item1 && c.Permission == w.Item2)))
            repo.Add(new RolePermission { RoleCode = w.Item1, Permission = w.Item2 });
        audit.Log("Update", "Permissions", "matrix", $"{wanted.Count} grants saved");
        await repo.SaveAsync();
        await ReloadCacheAsync(repo);
    }

    public async Task AddRoleAsync(string code, string name, string? nameAr)
    {
        Require(Perm.AdmRoles);
        code = new string((code ?? "").Where(char.IsLetterOrDigit).ToArray());
        if (code.Length < 3 || string.IsNullOrWhiteSpace(name)) throw new DomainException("err.roleInvalid");
        if (await repo.AnyAsync(repo.Query<Role>().Where(r => r.Code == code))) throw new DomainException("err.roleExists");
        repo.Add(new Role { Code = code, Name = name.Trim(), NameAr = string.IsNullOrWhiteSpace(nameAr) ? name.Trim() : nameAr.Trim() });
        audit.Log("Create", "Role", code, name);
        await repo.SaveAsync();
        await ReloadCacheAsync(repo);
    }

    public async Task DeleteRoleAsync(string code)
    {
        Require(Perm.AdmRoles);
        var r = await repo.FirstOrDefaultAsync(repo.Query<Role>().Where(x => x.Code == code)) ?? throw new DomainException("err.notFound");
        if (r.IsSystem) throw new DomainException("err.roleSystem");
        if ((await repo.ToListAsync(repo.Query<AppUser>())).Any(u => u.RoleArray.Contains(code))) throw new DomainException("err.roleInUse");
        var steps = await repo.ToListAsync(repo.Query<WorkflowStep>());
        if (steps.Any(s => (s.RoleCode ?? "").Split('|').Contains(code))) throw new DomainException("err.roleInUse");
        foreach (var p in await repo.ToListAsync(repo.Query<RolePermission>().Where(p => p.RoleCode == code))) repo.Remove(p);
        repo.Remove(r);
        audit.Log("Delete", "Role", code, r.Name);
        await repo.SaveAsync();
        await ReloadCacheAsync(repo);
    }

    public Task<List<AppUser>> UsersAsync() { Require(Perm.AdmUsers); return repo.ToListAsync(repo.Query<AppUser>().OrderBy(u => u.Email)); }

    public async Task SetUserRolesAsync(int id, string[]? roles, bool active)
    {
        Require(Perm.AdmUsers);
        var u = await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
        var known = (await RolesAsync()).Select(r => r.Code).ToHashSet();
        var list = (roles ?? []).Where(known.Contains).Distinct().ToList();
        if (list.Count == 0) list.Add(Roles.Employee);
        if (u.Id == me.UserId && (!list.Contains(Roles.SystemAdmin) || !active)) throw new DomainException("err.lockout");
        u.RoleList = string.Join(',', list); u.IsActive = active;
        audit.Log("Update", "User", id, $"{u.Email}: roles {u.RoleList}, active {active}");
        await repo.SaveAsync();
    }

    public async Task CreateUserAsync(string email, string password, string[]? roles, int? employeeId)
    {
        Require(Perm.AdmUsers);
        email = (email ?? "").Trim().ToLowerInvariant();
        if (!email.Contains('@') || (password ?? "").Length < 8) throw new DomainException("err.userInvalid");
        if (await repo.AnyAsync(repo.Query<AppUser>().Where(u => u.Email == email))) throw new DomainException("err.userExists");
        var known = (await RolesAsync()).Select(r => r.Code).ToHashSet();
        var list = (roles ?? []).Where(known.Contains).DefaultIfEmpty(Roles.Employee).ToList();
        repo.Add(new AppUser { Email = email, PasswordHash = PasswordHasher.Hash(password!), RoleList = string.Join(',', list), EmployeeId = employeeId });
        audit.Log("Create", "User", email, string.Join(',', list));
        await repo.SaveAsync();
    }
}
