using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Requests;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH3-008 draft and publish lifecycle for approval workflow edits.</summary>
public class WorkflowDraftTests
{
    class U(bool grant = true) : ICurrentUser { public string Email => "drafts@example.local"; public int? EmployeeId => 1; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => true; public bool Can(string p) => grant; }
    static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
    static (WorkflowDrafts D, WorkflowService W) S(AppDbContext d, bool grant = true) { var u = new U(grant); var c = new AppClock(TimeProvider.System); var w = new WorkflowService(d, u, new AuditService(d, u, c)); return (new(d, u, new AuditService(d, u, c), c, w), w); }
    static async Task<List<string>> LiveTokens(WorkflowService w, string kind) => (await w.ListAsync()).First(x => x.Kind == kind).Steps.Select(s => s.Token + "|" + s.Threshold).ToList();

    [Fact] public async Task DraftEditsDoNotTouchLiveUntilPublished()
    {
        await using var d = await Db(); var (dr, w) = S(d);
        var before = await LiveTokens(w, "Leave");
        await dr.StartAsync("Leave");
        await Assert.ThrowsAsync<DomainException>(() => dr.StartAsync("Leave"));
        await dr.AddStepAsync("Leave", ApproverType.Role, [Roles.FinanceManager], 30);
        var view = Assert.Single(await dr.ListAsync());
        Assert.Contains(view.Changes, c => c.Kind == "Added" && c.Text.Contains(Roles.FinanceManager));
        Assert.Equal(before, await LiveTokens(w, "Leave"));
        await dr.PublishAsync("Leave");
        var after = await LiveTokens(w, "Leave");
        Assert.Equal(before.Count + 1, after.Count); Assert.Contains(Roles.FinanceManager+"|30", after);
        Assert.Empty(await dr.ListAsync());
        Assert.True(await d.Set<AuditLog>().AnyAsync(a => a.Action == "Publish" && a.Entity == "Workflow"));
    }

    [Fact] public async Task PublishNeedsAChangeAndValidationMatchesLiveEditing()
    {
        await using var d = await Db(); var (dr, w) = S(d);
        await dr.StartAsync("Overtime");
        await Assert.ThrowsAsync<DomainException>(() => dr.PublishAsync("Overtime"));
        await Assert.ThrowsAsync<DomainException>(() => dr.AddStepAsync("Overtime", ApproverType.Role, ["NoSuchRole"], null));
        await Assert.ThrowsAsync<DomainException>(() => dr.AddStepAsync("Overtime", ApproverType.Role, [], null));
        await dr.AddStepAsync("Overtime", ApproverType.Role, [Roles.FinanceManager], null);
        await dr.MoveStepAsync("Overtime", 2, -1);
        await dr.RemoveStepAsync("Overtime", 1);
        await Assert.ThrowsAsync<DomainException>(() => dr.RemoveStepAsync("Overtime", 5));
        await dr.PublishAsync("Overtime");
        Assert.Contains(Roles.FinanceManager+"|", string.Join(",", await LiveTokens(w, "Overtime")));
    }

    [Fact] public async Task DiscardKeepsLiveAndAccessIsRequired()
    {
        await using var d = await Db(); var (dr, w) = S(d); var before = await LiveTokens(w, "Advance");
        await Assert.ThrowsAsync<DomainException>(() => S(d, false).D.StartAsync("Advance"));
        await Assert.ThrowsAsync<DomainException>(() => S(d, false).D.ListAsync());
        await Assert.ThrowsAsync<DomainException>(() => dr.StartAsync("Nope"));
        await dr.StartAsync("Advance"); await dr.AddStepAsync("Advance", ApproverType.Role, [Roles.FinanceManager], null);
        await Assert.ThrowsAsync<DomainException>(() => S(d, false).D.PublishAsync("Advance"));
        await dr.DiscardAsync("Advance");
        Assert.Equal(before, await LiveTokens(w, "Advance")); Assert.Empty(await dr.ListAsync());
        await Assert.ThrowsAsync<DomainException>(() => dr.PublishAsync("Advance"));
    }
}
