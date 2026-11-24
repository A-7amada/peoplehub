using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

namespace PeopleHub.Application.Services;

public class LeaveService(IRepository repo, AccessScope scope, ICurrentUser me, AuditService audit, NotificationService notify, AppClock clock, WorkflowService flows)
{
    public async Task<List<LeaveTransaction>> TransactionsAsync(int id,int year)
    {
        if (!await scope.CanSeeAsync(id)) throw new DomainException("err.forbidden");
        if(me.EmployeeId!=id&&!me.Can(Perm.LeaveEvidence))return [];
        return await repo.ToListAsync(repo.Query<LeaveTransaction>().Where(x=>x.EmployeeId==id && x.Year==year).OrderByDescending(x=>x.At));
    }
    private void Entry(int employeeId,int typeId,int year,int delta,string kind,string reason,int? request=null) => repo.Add(new LeaveTransaction {EmployeeId=employeeId,LeaveTypeId=typeId,Year=year,Delta=delta,Kind=kind,Reason=reason,RequestId=request,Actor=me.Email,At=clock.UtcNow});
    public Task<List<LeaveType>> TypesAsync() => repo.ToListAsync(repo.Query<LeaveType>().OrderBy(t => t.Id));

    /// <summary>Returns balances for the year, creating missing rows from type defaults (with carry-over from the previous year).</summary>
    public async Task<List<LeaveBalance>> BalancesAsync(int employeeId, int year)
    {
        if (!await scope.CanSeeAsync(employeeId)) throw new DomainException("err.forbidden");
        if(me.EmployeeId!=employeeId&&!me.Can(Perm.LeaveEvidence))throw new DomainException("err.forbidden");
        var types = await TypesAsync();
        var existing = await repo.ToListAsync(repo.Query<LeaveBalance>().Where(b => b.EmployeeId == employeeId && b.Year == year));
        var prev = await repo.ToListAsync(repo.Query<LeaveBalance>().Where(b => b.EmployeeId == employeeId && b.Year == year - 1));
        var created = false;
        var addedTypes=new HashSet<int>();
        foreach (var t in types.Where(t => t.DeductsBalance && existing.All(b => b.LeaveTypeId != t.Id)))
        {
            var p = prev.FirstOrDefault(b => b.LeaveTypeId == t.Id);
            var nb = p != null ? LeaveBalance.Rollover(p, t) : new LeaveBalance { EmployeeId = employeeId, LeaveTypeId = t.Id, Year = year, Entitled = t.DefaultDays };
            nb.LeaveType = t;
            repo.Add(nb); existing.Add(nb); created = true; addedTypes.Add(t.Id);
            Entry(employeeId,t.Id,year,nb.Remaining,"Opening","Annual entitlement and carry-forward");
        }
        foreach (var b in existing)
            if (!await repo.AnyAsync(repo.Query<LeaveTransaction>().Where(x=>x.EmployeeId==employeeId && x.LeaveTypeId==b.LeaveTypeId && x.Year==year)) && !addedTypes.Contains(b.LeaveTypeId))
            {Entry(employeeId,b.LeaveTypeId,year,b.Remaining,"Baseline","Pre-upgrade balance; earlier transactions unknown");created=true;}
        if (created) await repo.SaveAsync();
        return existing.OrderBy(b => b.LeaveTypeId).ToList();
    }

    public Task RequestAsync(int typeId, DateTime start, DateTime end, string? reason, string? document, string? attToken = null) => repo.AtomicAsync(()=>RequestCoreAsync(typeId,start,end,reason,document,attToken));
    private async Task RequestCoreAsync(int typeId, DateTime start, DateTime end, string? reason, string? document, string? attToken = null)
    {
        var e = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x => x.Id == (me.EmployeeId ?? -1))) ?? throw new DomainException("err.forbidden");
        var type = await repo.FirstOrDefaultAsync(repo.Query<LeaveType>().Where(t => t.Id == typeId)) ?? throw new DomainException("err.notFound");
        var balance = (await BalancesAsync(e.Id, start.Year)).FirstOrDefault(b => b.LeaveTypeId == typeId);
        var assignments=await repo.ToListAsync(repo.Query<EmployeeAssignment>().Where(x=>x.EmployeeId==e.Id));
        var holidays = (await repo.ToListAsync(repo.Query<Holiday>())).Where(h=>HolidayCalendar.Applies(h,e,h.Date,assignments)).Select(h => h.Date);
        var req = LeaveRequest.Create(e, type, balance, start, end, reason, document, clock.Today, holidays);
        await flows.RouteRequestAsync("Leave",req,e,req.Days,!type.IsPaid);
        var mine = await repo.ToListAsync(repo.Query<LeaveRequest>().Where(l => l.EmployeeId == e.Id));
        if (mine.Any(req.Overlaps)) throw new DomainException("err.overlap");
        e.LeaveRequestStamp=Guid.NewGuid().ToString("N"); // concurrent requests for one employee contend on the employee revision
        req.Employee = e; req.AttachmentToken = attToken; req.AttachmentName = attToken == null ? null : document;
        repo.Add(req);
        await notify.NotifyStageAsync(req, $"{e.FullName}: {type.Name} {start:yyyy-MM-dd} → {end:yyyy-MM-dd} ({req.Days})");
        await repo.SaveAsync();
    }

    public Task CancelApprovedAsync(int requestId,string? reason=null) => repo.AtomicAsync(()=>CancelApprovedCoreAsync(requestId,reason));
    private async Task CancelApprovedCoreAsync(int requestId, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("err.reasonRequired");
        var l = await repo.FirstOrDefaultAsync(repo.Query<LeaveRequest>().Where(x => x.Id == requestId)) ?? throw new DomainException("err.notFound");
        if (l.EmployeeId != me.EmployeeId && !me.Can(Perm.LeaveAdmin)) throw new DomainException("err.forbidden");
        var bal = await repo.FirstOrDefaultAsync(repo.Query<LeaveBalance>().Where(b => b.EmployeeId == l.EmployeeId && b.LeaveTypeId == l.LeaveTypeId && b.Year == l.StartDate.Year));
        if(l.LeaveType?.DeductsBalance==true && bal!=null) {
            if (!await repo.AnyAsync(repo.Query<LeaveTransaction>().Where(x=>x.EmployeeId==l.EmployeeId && x.LeaveTypeId==l.LeaveTypeId && x.Year==l.StartDate.Year))) Entry(l.EmployeeId,l.LeaveTypeId,l.StartDate.Year,bal.Remaining,"Baseline","Pre-upgrade balance; earlier transactions unknown");
        }
        l.CancelApproved(bal, clock.Today, clock.UtcNow);
        if(l.LeaveType?.DeductsBalance==true) Entry(l.EmployeeId,l.LeaveTypeId,l.StartDate.Year,l.Days,"Cancellation",reason!,l.Id);
        await notify.NotifyEmployeeAsync(l.EmployeeId,"notif.cancelled",$"Leave #{l.Id}: {reason}","/Leave");
        if(l.Employee?.ManagerId is {} manager) await notify.NotifyEmployeeAsync(manager,"notif.cancelled",$"Leave #{l.Id}: {reason}","/Requests");
        repo.Add(new ApprovalAction { Kind = "Leave", RequestId = l.Id, Actor = me.Email, Action = "Cancelled", Comment=reason!.Trim(), At = clock.UtcNow });
        audit.Log("Cancel", "Leave", l.Id, $"approved leave cancelled, {l.Days} days restored");
        await repo.SaveAsync();
    }

    public Task AdjustBalanceAsync(int employeeId, int typeId, int year, int entitled, string? reason) => repo.AtomicAsync(()=>AdjustBalanceCoreAsync(employeeId,typeId,year,entitled,reason));
    private async Task AdjustBalanceCoreAsync(int employeeId, int typeId, int year, int entitled, string? reason)
    {
        if (!me.Can(Perm.LeaveAdmin)) throw new DomainException("err.forbidden");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("err.reasonRequired");
        if (entitled < 0 || entitled > 90) throw new DomainException("err.balance");
        var b = (await BalancesAsync(employeeId, year)).FirstOrDefault(x => x.LeaveTypeId == typeId) ?? throw new DomainException("err.notFound");
        audit.Log("Update", "LeaveBalance", $"{employeeId}/{typeId}/{year}", $"entitled {b.Entitled} -> {entitled}; {reason}");
        Entry(employeeId,typeId,year,entitled-b.Entitled,"Adjustment",reason!.Trim());
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
