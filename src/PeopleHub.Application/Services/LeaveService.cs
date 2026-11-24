using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

namespace PeopleHub.Application.Services;

public class LeaveService(IRepository repo, AccessScope scope, ICurrentUser me, AuditService audit, NotificationService notify, AppClock clock, WorkflowService flows)
{
    public Task<List<LeaveType>> TypesAsync() => repo.ToListAsync(repo.Query<LeaveType>().OrderBy(t => t.Id));

    /// <summary>Returns balances for the year, creating missing rows from type defaults (with carry-over from the previous year).</summary>
    public async Task<List<LeaveBalance>> BalancesAsync(int employeeId, int year)
    {
        if (!await scope.CanSeeAsync(employeeId)) throw new DomainException("err.forbidden");
        var types = await TypesAsync();
        var existing = await repo.ToListAsync(repo.Query<LeaveBalance>().Where(b => b.EmployeeId == employeeId && b.Year == year));
        var prev = await repo.ToListAsync(repo.Query<LeaveBalance>().Where(b => b.EmployeeId == employeeId && b.Year == year - 1));
        var created = false;
        foreach (var t in types.Where(t => t.DeductsBalance && existing.All(b => b.LeaveTypeId != t.Id)))
        {
            var p = prev.FirstOrDefault(b => b.LeaveTypeId == t.Id);
            var nb = p != null ? LeaveBalance.Rollover(p, t) : new LeaveBalance { EmployeeId = employeeId, LeaveTypeId = t.Id, Year = year, Entitled = t.DefaultDays };
            nb.LeaveType = t;
            repo.Add(nb); existing.Add(nb); created = true;
        }
        if (created) await repo.SaveAsync();
        return existing.OrderBy(b => b.LeaveTypeId).ToList();
    }

    public async Task RequestAsync(int typeId, DateTime start, DateTime end, string? reason, string? document)
    {
        var e = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x => x.Id == (me.EmployeeId ?? -1))) ?? throw new DomainException("err.forbidden");
        var type = await repo.FirstOrDefaultAsync(repo.Query<LeaveType>().Where(t => t.Id == typeId)) ?? throw new DomainException("err.notFound");
        var balance = (await BalancesAsync(e.Id, start.Year)).FirstOrDefault(b => b.LeaveTypeId == typeId);
        var holidays = (await repo.ToListAsync(repo.Query<Holiday>())).Select(h => h.Date);
        var req = LeaveRequest.Create(e, type, balance, start, end, reason, document, clock.Today, holidays);
        req.Route = await flows.RouteForAsync("Leave", e, req.Days, !type.IsPaid);
        var mine = await repo.ToListAsync(repo.Query<LeaveRequest>().Where(l => l.EmployeeId == e.Id));
        if (mine.Any(req.Overlaps)) throw new DomainException("err.overlap");
        req.Employee = e;
        repo.Add(req);
        await notify.NotifyStageAsync(req, $"{e.FullName}: {type.Name} {start:yyyy-MM-dd} → {end:yyyy-MM-dd} ({req.Days})");
        await repo.SaveAsync();
    }

    public async Task CancelApprovedAsync(int requestId)
    {
        var l = await repo.FirstOrDefaultAsync(repo.Query<LeaveRequest>().Where(x => x.Id == requestId)) ?? throw new DomainException("err.notFound");
        if (l.EmployeeId != me.EmployeeId && !me.Can(Perm.LeaveAdmin)) throw new DomainException("err.forbidden");
        var bal = await repo.FirstOrDefaultAsync(repo.Query<LeaveBalance>().Where(b => b.EmployeeId == l.EmployeeId && b.LeaveTypeId == l.LeaveTypeId && b.Year == l.StartDate.Year));
        l.CancelApproved(bal, clock.Today, clock.UtcNow);
        repo.Add(new ApprovalAction { Kind = "Leave", RequestId = l.Id, Actor = me.Email, Action = "Cancelled", At = clock.UtcNow });
        audit.Log("Cancel", "Leave", l.Id, $"approved leave cancelled, {l.Days} days restored");
        await repo.SaveAsync();
    }

    public async Task AdjustBalanceAsync(int employeeId, int typeId, int year, int entitled, string? reason)
    {
        if (!me.Can(Perm.LeaveAdmin)) throw new DomainException("err.forbidden");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("err.reasonRequired");
        if (entitled < 0 || entitled > 90) throw new DomainException("err.balance");
        var b = (await BalancesAsync(employeeId, year)).FirstOrDefault(x => x.LeaveTypeId == typeId) ?? throw new DomainException("err.notFound");
        audit.Log("Update", "LeaveBalance", $"{employeeId}/{typeId}/{year}", $"entitled {b.Entitled} -> {entitled}; {reason}");
        b.Entitled = entitled;
        await repo.SaveAsync();
    }

    public async Task<List<(Employee Employee, LeaveRequest Leave)>> OnLeaveAsync(DateTime date)
    {
        var ids = await scope.VisibleEmployeeIdsAsync();
        var ls = await repo.ToListAsync(repo.Query<LeaveRequest>().Where(l => l.Status == RequestStatus.Approved && l.StartDate <= date && l.EndDate >= date && (ids == null || ids.Contains(l.EmployeeId))));
        return ls.Where(l => l.Employee != null).Select(l => (l.Employee!, l)).ToList();
    }
}
