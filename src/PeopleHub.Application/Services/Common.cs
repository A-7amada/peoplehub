using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

namespace PeopleHub.Application.Services;

public interface ICurrentUser
{
    string Email { get; }
    int? EmployeeId { get; }
    int UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool Can(string permission);
}

public static class CurrentUserExt
{
    public static bool InAny(this ICurrentUser u, string csvRoles) =>
        csvRoles.Split(',').Any(u.IsInRole);
}

/// <summary>Business clock in Cairo time.</summary>
public class AppClock(TimeProvider tp)
{
    private static readonly TimeZoneInfo Zone = Find();
    private static TimeZoneInfo Find()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"); }
        catch { return TimeZoneInfo.CreateCustomTimeZone("EG", TimeSpan.FromHours(2), "EG", "EG"); }
    }
    public DateTime UtcNow => tp.GetUtcNow().UtcDateTime;
    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, Zone);
    public DateTime Today => Now.Date;
}

public class AuditService(IRepository repo, ICurrentUser me, AppClock clock)
{
    public void Log(string action, string entity, object id, string details) =>
        repo.Add(new AuditLog { At = clock.UtcNow, User = me.IsAuthenticated ? me.Email : "system", Action = action, Entity = entity, EntityId = id.ToString() ?? "", Details = details });

    public Task<List<AuditLog>> RecentAsync(string? entity, int take = 200)
    {
        var q = repo.Query<AuditLog>().Where(a => true);
        if (!string.IsNullOrWhiteSpace(entity)) q = q.Where(a => a.Entity == entity);
        return repo.ToListAsync(q.OrderByDescending(a => a.At).Take(take));
    }
}

public class NotificationService(IRepository repo, ICurrentUser me, AppClock clock)
{
    public async Task NotifyEmployeeAsync(int employeeId, string titleKey, string details, string? link = null)
    {
        var users = await repo.ToListAsync(repo.Query<AppUser>().Where(u => u.EmployeeId == employeeId && u.IsActive));
        foreach (var u in users) Add(u, titleKey, details, link);
    }

    public async Task NotifyRolesAsync(string csvRoles, string titleKey, string details, string? link = null)
    {
        var wanted = csvRoles.Split(',');
        var users = await repo.ToListAsync(repo.Query<AppUser>().Where(u => u.IsActive));
        foreach (var u in users.Where(u => u.RoleArray.Any(wanted.Contains))) Add(u, titleKey, details, link);
    }

    public async Task NotifyPermAsync(string permission, string titleKey, string details, string? link = null)
    {
        var users = await repo.ToListAsync(repo.Query<AppUser>().Where(u => u.IsActive));
        foreach (var u in users.Where(u => PermissionCache.Has(u.RoleArray, permission))) Add(u, titleKey, details, link);
    }

    /// <summary>Tells whoever must act on the request's current stage.</summary>
    public async Task NotifyStageAsync(Approvable a, string details)
    {
        var stage = a.CurrentStage;
        if (stage == null) return;
        if(ApprovalStage.EmployeeId(stage) is {} resolved)
        {await NotifyEmployeeAsync(resolved,"notif.pendingApproval",details,"/Requests?scope=inbox");return;}
        if (stage == ApprovalStage.Manager)
        { if (a.Employee?.ManagerId is { } m) await NotifyEmployeeAsync(m, "notif.pendingApproval", details, "/Requests?scope=inbox"); }
        else await NotifyRolesAsync(stage.Replace('|', ','), "notif.pendingApproval", details, "/Requests?scope=inbox");
    }

    private void Add(AppUser u, string key, string details, string? link)
    {
        repo.Add(new Notification { UserId = u.Id, TitleKey = key, Details = details, Link = link, CreatedAt = clock.UtcNow });
        if (SettingsService.Get("email.notify") == "true")
        {
            var email = u.Email;
            if (!string.IsNullOrEmpty(email)) repo.Add(new EmailOutbox { To = email, Subject = "PeopleHub: " + key, Body = details + (link != null ? "\n" + link : ""), CreatedAt = clock.UtcNow, Note = "SMTP not configured (demo): kept in the outbox" });
        }
    }


    public Task<List<Notification>> ForMeAsync(int take = 50) =>
        repo.ToListAsync(repo.Query<Notification>().Where(n => n.UserId == me.UserId).OrderByDescending(n => n.CreatedAt).Take(take));

    public Task<int> UnreadCountAsync() => repo.CountAsync(repo.Query<Notification>().Where(n => n.UserId == me.UserId && !n.IsRead));

    public async Task MarkAllReadAsync()
    {
        foreach (var n in await repo.ToListAsync(repo.Query<Notification>().Where(n => n.UserId == me.UserId && !n.IsRead))) n.IsRead = true;
        await repo.SaveAsync();
    }

}

/// <summary>Data-level permissions: which employees the current user may see.</summary>
public class AccessScope(IRepository repo, ICurrentUser me)
{
    private List<int>? _ids; private bool _loaded;

    /// <summary>Null means everyone.</summary>
    public async Task<List<int>?> VisibleEmployeeIdsAsync()
    {
        if (_loaded) return _ids;
        _loaded = true;
        if (me.Can(Perm.ScopeCompany)) return _ids = null;
        var ids = new HashSet<int>();
        if (me.EmployeeId is { } self)
        {
            ids.Add(self);
            if (me.Can(Perm.ScopeTeam))
            {
                var depts = await repo.ToListAsync(repo.Query<Department>());
                var managed = depts.Where(d => d.ManagerId == self).Select(d => d.Id).ToHashSet();
                var grew = true;
                while (grew) { grew = false; foreach (var d in depts.Where(d => d.ParentId != null && managed.Contains(d.ParentId.Value) && managed.Add(d.Id))) grew = true; }
                var emps = await repo.ToListAsync(repo.Query<Employee>().Where(e => e.ManagerId == self || managed.Contains(e.DepartmentId)));
                foreach (var e in emps) ids.Add(e.Id);
            }
        }
        return _ids = ids.ToList();
    }

    public async Task<bool> CanSeeAsync(int employeeId)
    {
        var ids = await VisibleEmployeeIdsAsync();
        return ids == null || ids.Contains(employeeId);
    }

    public bool CanSeeSalary(int employeeId) =>
        me.Can(Perm.EmpSalary) || me.EmployeeId == employeeId;
}
