using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Requests;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH3-008 version comparison and US-PH3-009 what-if simulation.</summary>
public class WorkflowCompareSimulateTests
{
    class U(bool grant = true) : ICurrentUser { public string Email => "flows@example.local"; public int? EmployeeId => 1; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => true; public bool Can(string p) => grant; }
    static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
    static WorkflowService S(AppDbContext d, bool grant = true) { var u = new U(grant); var c = new AppClock(TimeProvider.System); return new(d, u, new AuditService(d, u, c)); }
    const string A = "{\"Kind\":\"Leave\",\"Steps\":[{\"Order\":1,\"Approver\":0,\"RoleCode\":null,\"Threshold\":null,\"Rule\":0,\"RequiredCount\":null},{\"Order\":2,\"Approver\":1,\"RoleCode\":\"HRManager\",\"Threshold\":5,\"Rule\":0,\"RequiredCount\":null}]}";
    const string B = "{\"Kind\":\"Leave\",\"Steps\":[{\"Order\":1,\"Approver\":0,\"RoleCode\":null,\"Threshold\":null,\"Rule\":0,\"RequiredCount\":null},{\"Order\":2,\"Approver\":1,\"RoleCode\":\"HRManager\",\"Threshold\":10,\"Rule\":0,\"RequiredCount\":null},{\"Order\":3,\"Approver\":1,\"RoleCode\":\"CEO\",\"Threshold\":null,\"Rule\":0,\"RequiredCount\":null}]}";

    [Fact] public void CompareReportsChangedAddedAndRemovedStepsAndNothingForIdentical()
    {
        Assert.Empty(WorkflowService.Compare(A, A));
        var ab = WorkflowService.Compare(A, B);
        Assert.Equal(2, ab.Count);
        Assert.Contains(ab, c => c.Kind == "Changed" && c.Text.Contains("Step 2") && c.Text.Contains(">5") && c.Text.Contains(">10"));
        Assert.Contains(ab, c => c.Kind == "Added" && c.Text.Contains("Step 3"));
        var ba = WorkflowService.Compare(B, A);
        Assert.Contains(ba, c => c.Kind == "Removed" && c.Text.Contains("Step 3"));
    }
    [Fact] public async Task CompareAndSimulateNeedTheWorkflowGrant()
    {
        await using var d = await Db();
        await Assert.ThrowsAsync<DomainException>(() => S(d, false).CompareAsync("live:Leave", "live:Leave"));
        await Assert.ThrowsAsync<DomainException>(() => S(d, false).SimulateAsync("Leave", "E001", 1, false));
    }
    [Fact] public async Task CompareLiveToAnObservedRevisionAndRefuseUnknownTargets()
    {
        await using var d = await Db();var s = S(d);
        var same = await s.CompareAsync("live:Leave", "live:Leave");Assert.Empty(same.Changes);
        await Assert.ThrowsAsync<DomainException>(() => s.CompareAsync("live:Nope", "live:Leave"));
        await Assert.ThrowsAsync<DomainException>(() => s.CompareAsync("deadbeef", "live:Leave"));
    }
    [Fact] public async Task SimulationGivesARouteAndChangesNothing()
    {
        await using var d = await Db();var s = S(d);
        var code = (await d.Set<Employee>().FirstAsync(x => x.Id == 6)).Code;
        var before = (await d.Set<WorkflowRevision>().CountAsync(), await d.Set<LeaveRequest>().CountAsync(), await d.Set<AuditLog>().CountAsync());
        var sim = await s.SimulateAsync("Leave", code, 2, false);
        Assert.NotEmpty(sim.Route);
        Assert.Equal(before.Item1, await d.Set<WorkflowRevision>().CountAsync());
        Assert.Equal(before.Item2, await d.Set<LeaveRequest>().CountAsync());
        Assert.False(d.ChangeTracker.Entries().Any(e => e.State is EntityState.Added && e.Entity is WorkflowRevision or LeaveRequest or ApprovalAction));
        await Assert.ThrowsAsync<DomainException>(() => s.SimulateAsync("Leave", "NO-SUCH", 1, false));
        await Assert.ThrowsAsync<DomainException>(() => s.SimulateAsync("Bogus", code, 1, false));
        await Assert.ThrowsAsync<DomainException>(() => s.SimulateAsync("Leave", code, -1, false));
    }
    [Fact] public async Task LargerSizeOrEscalationNeverShortensTheRoute()
    {
        await using var d = await Db();var s = S(d);var code = (await d.Set<Employee>().FirstAsync(x => x.Id == 6)).Code;
        var small = await s.SimulateAsync("Leave", code, 1, false);var big = await s.SimulateAsync("Leave", code, 1000, false);var esc = await s.SimulateAsync("Leave", code, 1, true);
        Assert.True(big.Route.Count >= small.Route.Count);Assert.True(esc.Route.Count >= small.Route.Count);
    }
}
