using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Domain.Security;

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

    private void Require() { if (!me.Can(Perm.AdmFlows)) throw new DomainException("err.forbidden"); }

    private async Task<WorkflowDefinition> LoadAsync(string kind) =>
        (await ListAsync()).FirstOrDefault(w => w.Kind == kind) ?? throw new DomainException("err.notFound");

    public async Task AddStepAsync(string kind, ApproverType type, string[]? roles, decimal? threshold)
    {
        Require();
        var w = await LoadAsync(kind);
        var codes = (roles ?? []).Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToArray();
        if (type == ApproverType.Role && codes.Length == 0) throw new DomainException("err.roleRequired");
        if (w.Steps.Count >= 6) throw new DomainException("err.maxSteps");
        if (type == ApproverType.DirectManager && w.Steps.Any(s => s.Approver == ApproverType.DirectManager)) throw new DomainException("err.dupManager");
        var step = new WorkflowStep { WorkflowId = w.Id, Order = (w.Steps.Count == 0 ? 0 : w.Steps.Max(s => s.Order)) + 1, Approver = type,
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
