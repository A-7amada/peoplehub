using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

namespace PeopleHub.Application.Services;

public record RequestRow(string Kind, int Id, Employee Employee, string TypeName, string TypeNameAr, string Detail,
    RequestStatus Status, string? Stage, string Route, int StageIndex, DateTime CreatedAt, string? Comment, bool CanAct, bool CanCancel, string? AttachmentToken, string? AttachmentName,
    List<ApprovalAction> History);

public class ApprovalService(IRepository repo, AccessScope scope, ICurrentUser me, AuditService audit, NotificationService notify, AppClock clock, WorkflowService flows, DelegationService delegations)
{
    public static readonly string[] Kinds = ["Leave", "Overtime", "Correction", "Advance"];

    private List<(int FromEmployeeId, string[] Roles)>? _deleg;
    private async Task EnsureDelegationsAsync() => _deleg ??= await delegations.ActiveToMeAsync();

    private bool CanActOn(Approvable a)
    {
        if (a.Status != RequestStatus.Pending || a.CurrentStage is not { } stage) return false;
        if (a.EmployeeId == me.EmployeeId) return false; // nobody approves their own request
        if (me.IsInRole(Roles.SystemAdmin)) return true;
        if (stage == ApprovalStage.Manager ? a.Employee?.ManagerId != null && a.Employee.ManagerId == me.EmployeeId : stage.Split('|').Any(me.IsInRole)) return true;
        // delegated rights: the delegator would have been allowed to act on this step
        foreach (var d in _deleg ?? [])
            if (stage == ApprovalStage.Manager ? a.Employee?.ManagerId == d.FromEmployeeId : stage.Split('|').Intersect(d.Roles).Any()) return true;
        return false;
    }

    public async Task<List<RequestRow>> ListAsync(string? kind, RequestStatus? status, bool inboxOnly)
    {
        var ids = await scope.VisibleEmployeeIdsAsync();
        await EnsureDelegationsAsync();
        bool Vis(Approvable a) => ids == null || ids.Contains(a.EmployeeId) || CanActOn(a);
        var items = new List<(string Kind, Approvable A, string T, string TAr, string Detail)>();
        if (kind is null or "Leave")
            items.AddRange((await repo.ToListAsync(repo.Query<LeaveRequest>())).Where(Vis).Select(l => ("Leave", (Approvable)l, l.LeaveType?.Name ?? "", l.LeaveType?.NameAr ?? "",
                $"{l.StartDate:yyyy-MM-dd} → {l.EndDate:yyyy-MM-dd} · {l.Days}")));
        if (kind is null or "Overtime")
            items.AddRange((await repo.ToListAsync(repo.Query<OvertimeRequest>())).Where(Vis).Select(o => ("Overtime", (Approvable)o, "", "", $"{o.Date:yyyy-MM-dd} · {o.Hours}h × {o.Multiplier} · {o.Reason}")));
        if (kind is null or "Correction")
            items.AddRange((await repo.ToListAsync(repo.Query<AttendanceCorrection>())).Where(Vis).Select(c => ("Correction", (Approvable)c, "", "",
                $"{c.Date:yyyy-MM-dd} · {c.OldCheckIn:HH:mm}-{c.OldCheckOut:HH:mm} → {c.NewCheckIn:HH:mm}-{c.NewCheckOut:HH:mm} · {c.Reason}")));
        if (kind is null or "Advance")
            items.AddRange((await repo.ToListAsync(repo.Query<AdvanceRequest>())).Where(Vis).Select(a => ("Advance", (Approvable)a, "", "", $"{a.Amount:N0} / {a.Installments}")));

        var actions = (await repo.ToListAsync(repo.Query<ApprovalAction>().OrderBy(x => x.At))).ToLookup(x => (x.Kind, x.RequestId));
        var rows = items.Select(i => new RequestRow(i.Kind, i.A.Id, i.A.Employee!, i.T, i.TAr, i.Detail, i.A.Status, i.A.CurrentStage, i.A.Route, i.A.StageIndex,
                i.A.CreatedAt, i.A.Comment, CanActOn(i.A), i.A.Status == RequestStatus.Pending && (i.A.EmployeeId == me.EmployeeId || me.Can(Perm.LeaveAdmin)), i.A.AttachmentToken, i.A.AttachmentName,
                actions[(i.Kind, i.A.Id)].ToList()))
            .Where(r => status == null || r.Status == status)
            .Where(r => !inboxOnly || r.CanAct)
            .OrderByDescending(r => r.CreatedAt).ToList();
        return rows;
    }

    public async Task<int> PendingForMeAsync() => (await ListAsync(null, RequestStatus.Pending, true)).Count;

    private async Task<Approvable> LoadAsync(string kind, int id) { await EnsureDelegationsAsync(); return await LoadCoreAsync(kind, id); }
    private async Task<Approvable> LoadCoreAsync(string kind, int id) => (kind switch
    {
        "Leave" => await repo.FirstOrDefaultAsync(repo.Query<LeaveRequest>().Where(x => x.Id == id)) as Approvable,
        "Overtime" => await repo.FirstOrDefaultAsync(repo.Query<OvertimeRequest>().Where(x => x.Id == id)),
        "Correction" => await repo.FirstOrDefaultAsync(repo.Query<AttendanceCorrection>().Where(x => x.Id == id)),
        "Advance" => await repo.FirstOrDefaultAsync(repo.Query<AdvanceRequest>().Where(x => x.Id == id)),
        _ => null
    }) ?? throw new DomainException("err.notFound");

    public async Task ApproveAsync(string kind, int id, string? comment)
    {
        var a = await LoadAsync(kind, id);
        if (!CanActOn(a)) throw new DomainException("err.forbidden");
        var stage = a.CurrentStage!;
        var done = a.ApproveStage(clock.UtcNow);
        repo.Add(new ApprovalAction { Kind = kind, RequestId = id, Actor = me.Email, Action = $"Approved:{stage}", Comment = comment?.Trim(), At = clock.UtcNow });
        audit.Log("Approve", kind, id, $"stage {stage}{(done ? " (final)" : "")}");
        var name = a.Employee?.FullName ?? "";
        if (done)
        {
            await ApplyAsync(a);
            await notify.NotifyEmployeeAsync(a.EmployeeId, "notif.approved", $"{kind} #{id}", "/Requests");
        }
        else await notify.NotifyStageAsync(a, $"{name}: {kind} #{id}");
        await repo.SaveAsync();
    }

    public async Task RejectAsync(string kind, int id, string? comment)
    {
        var a = await LoadAsync(kind, id);
        if (!CanActOn(a)) throw new DomainException("err.forbidden");
        a.RejectRequest(comment, clock.UtcNow);
        repo.Add(new ApprovalAction { Kind = kind, RequestId = id, Actor = me.Email, Action = $"Rejected:{a.CurrentStage}", Comment = comment, At = clock.UtcNow });
        audit.Log("Reject", kind, id, comment ?? "");
        await notify.NotifyEmployeeAsync(a.EmployeeId, "notif.rejected", $"{kind} #{id}: {comment}", "/Requests");
        await repo.SaveAsync();
    }

    public async Task CancelAsync(string kind, int id)
    {
        var a = await LoadAsync(kind, id);
        if (a.EmployeeId != me.EmployeeId && !me.Can(Perm.LeaveAdmin)) throw new DomainException("err.forbidden");
        a.CancelRequest(clock.UtcNow);
        repo.Add(new ApprovalAction { Kind = kind, RequestId = id, Actor = me.Email, Action = "Cancelled", At = clock.UtcNow });
        audit.Log("Cancel", kind, id, "pending request cancelled");
        await repo.SaveAsync();
    }

    /// <summary>Side effects once the last stage is approved.</summary>
    private async Task ApplyAsync(Approvable a)
    {
        switch (a)
        {
            case LeaveRequest l:
                var bal = await repo.FirstOrDefaultAsync(repo.Query<LeaveBalance>().Where(b => b.EmployeeId == l.EmployeeId && b.LeaveTypeId == l.LeaveTypeId && b.Year == l.StartDate.Year));
                l.ApplyToBalance(bal);
                audit.Log("Update", "LeaveBalance", l.EmployeeId, $"-{l.Days} days ({l.LeaveType?.Name})");
                break;
            case AttendanceCorrection c:
                var rec = await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == c.EmployeeId && r.Date == c.Date));
                var shift = (await repo.ToListAsync(repo.Query<Shift>())).FirstOrDefault(s => s.Id == c.Employee!.ShiftId) ?? (await repo.ToListAsync(repo.Query<Shift>())).First();
                var isNew = rec == null;
                rec ??= new AttendanceRecord { EmployeeId = c.EmployeeId, Date = c.Date };
                var before = $"{rec.CheckIn:HH:mm}-{rec.CheckOut:HH:mm}";
                rec.CheckIn = c.NewCheckIn; rec.CheckOut = c.NewCheckOut; rec.Source = AttendanceSource.Manual; rec.IsExcused = false; rec.ExcuseNote = null;
                rec.Recompute(shift);
                if (isNew) repo.Add(rec);
                audit.Log("Correct", "Attendance", $"{c.Employee!.Code}/{c.Date:yyyy-MM-dd}", $"{before} -> {c.NewCheckIn:HH:mm}-{c.NewCheckOut:HH:mm}; {c.Reason}");
                break;
        }
    }

    public async Task RequestAdvanceAsync(decimal amount, int installments, int startYear, int startMonth, string? reason)
    {
        var e = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x => x.Id == (me.EmployeeId ?? -1))) ?? throw new DomainException("err.forbidden");
        if (await repo.AnyAsync(repo.Query<AdvanceRequest>().Where(x => x.EmployeeId == e.Id && (x.Status == RequestStatus.Pending || (x.Status == RequestStatus.Approved && x.Paid < x.Amount)))))
            throw new DomainException("err.advanceOpen");
        var adv = AdvanceRequest.Create(e, amount, installments, startYear, startMonth, reason);
        adv.Route = await flows.RouteForAsync("Advance", e, amount);
        adv.Employee = e;
        repo.Add(adv);
        await notify.NotifyStageAsync(adv, $"{e.FullName}: {amount:N0}");
        await repo.SaveAsync();
    }

    public async Task SettleAdvanceAsync(int id)
    {
        if (!me.Can(Perm.AdvManage)) throw new DomainException("err.forbidden");
        var a = await repo.FirstOrDefaultAsync(repo.Query<AdvanceRequest>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
        var left = a.Remaining;
        a.Settle();
        audit.Log("Settle", "Advance", id, $"early settlement of {left:N2}");
        await repo.SaveAsync();
    }

    public async Task<List<AdvanceRequest>> AdvancesAsync()
    {
        var ids = await scope.VisibleEmployeeIdsAsync();
        return (await repo.ToListAsync(repo.Query<AdvanceRequest>().OrderByDescending(a => a.CreatedAt))).Where(a => ids == null || ids.Contains(a.EmployeeId)).ToList();
    }
}
