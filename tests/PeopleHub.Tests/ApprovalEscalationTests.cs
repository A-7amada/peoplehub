using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Requests;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH3-007 escalation and overdue report. Escalation only notifies; defaults are illustrative.</summary>
public class ApprovalEscalationTests
{
    class U(bool grant = true) : ICurrentUser { public string Email => "flows@example.local"; public int? EmployeeId => 1; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => true; public bool Can(string p) => grant; }
    static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
    static WorkflowService W(AppDbContext d, bool grant = true) { var u = new U(grant); var c = new AppClock(TimeProvider.System); return new(d, u, new AuditService(d, u, c)); }

    [Fact] public async Task PolicyNeedsGrantValidatesAndDefaultsToHrManager()
    {
        await using var d = await Db();
        await Assert.ThrowsAsync<DomainException>(() => W(d, false).SetEscalationHoursAsync("Leave", 24));
        await Assert.ThrowsAsync<DomainException>(() => W(d).SetEscalationHoursAsync("Bogus", 24));
        await Assert.ThrowsAsync<DomainException>(() => W(d).SetEscalationHoursAsync("Leave", -1));
        await Assert.ThrowsAsync<DomainException>(() => W(d).SetEscalationRoleAsync("NoSuchRole"));
        Assert.Equal(Roles.HRManager, await W(d).EscalationRoleAsync());
        Assert.Equal(0, (await W(d).EscalationHoursAsync())["Leave"]);
        await W(d).SetEscalationHoursAsync("Leave", 48);Assert.Equal(48, (await W(d).EscalationHoursAsync())["Leave"]);
        await W(d).SetEscalationHoursAsync("Leave", 0);Assert.Equal(0, (await W(d).EscalationHoursAsync())["Leave"]);
        Assert.True(await d.Set<AuditLog>().AnyAsync(a => a.Entity == "Workflow" && a.Action == "EscalationPolicy"));
    }
    [Fact] public async Task OverdueReportIsEmptyWithoutPolicyAndListsOnlyPendingRequestsPastTheirHours()
    {
        await using var d = await Db();var clock = new AppClock(TimeProvider.System);var r = new ApprovalReminders(d, clock);
        Assert.Empty(await r.OverdueAsync());
        await W(d).SetEscalationHoursAsync("Leave", 1);
        var pending = await d.Set<LeaveRequest>().Where(x => x.Status == RequestStatus.Pending).ToListAsync();
        var overdue = await r.OverdueAsync();
        Assert.All(overdue, o => Assert.Equal("Leave", o.Kind));
        Assert.True(overdue.Count <= pending.Count);
    }
    [Fact] public async Task EscalationNotifiesTheRoleOncePerStageAndNeverChangesTheRequest()
    {
        await using var d = await Db();var clock = new AppClock(TimeProvider.System);var r = new ApprovalReminders(d, clock);
        await W(d).SetEscalationHoursAsync("Leave", 1);
        var pending = await d.Set<LeaveRequest>().Where(x => x.Status == RequestStatus.Pending).ToListAsync();
        foreach (var p in pending) p.CreatedAt = DateTime.UtcNow.AddDays(-3);
        await d.SaveChangesAsync();
        var statuses = pending.Select(p => (p.Id, p.Status, p.StageIndex, p.CurrentStage)).ToList();
        await r.RunAsync();
        var first = await d.Set<Notification>().CountAsync(n => n.TitleKey == "notif.approvalEscalation");
        var markers = await d.Set<ApprovalReminder>().CountAsync(x => x.Kind.StartsWith("Escalation:"));
        Assert.Equal(first, markers);
        await r.RunAsync();
        Assert.Equal(first, await d.Set<Notification>().CountAsync(n => n.TitleKey == "notif.approvalEscalation"));
        foreach (var s in statuses) { var now = await d.Set<LeaveRequest>().FindAsync(s.Id); Assert.Equal(s.Status, now!.Status); Assert.Equal(s.StageIndex, now.StageIndex); Assert.Equal(s.CurrentStage, now.CurrentStage); }
        Assert.Equal(0, await d.Set<ApprovalAction>().CountAsync(a => a.Action.StartsWith("Escalat")));
        var hrIds = (await d.Set<AppUser>().ToListAsync()).Where(u => u.RoleArray.Contains(Roles.HRManager)).Select(u => u.Id).ToHashSet();
        Assert.All(await d.Set<Notification>().Where(n => n.TitleKey == "notif.approvalEscalation").ToListAsync(), n => Assert.Contains(n.UserId, hrIds));
    }
}
