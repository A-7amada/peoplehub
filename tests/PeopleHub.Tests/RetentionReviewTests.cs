using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Operations;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH1-005 retention schedule and legal hold. Review list only: nothing is deleted.</summary>
public class RetentionReviewTests
{
 class User(bool grant = true) : ICurrentUser { public string Email => "ret@example.local"; public int? EmployeeId => 1; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => false; public bool Can(string p) => p == Perm.AdmSettings && grant; }
 class Time : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero); }
 static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
 static RetentionReview S(AppDbContext d, bool grant = true) { var u = new User(grant); var c = new AppClock(new Time()); return new(d, u, new AuditService(d, u, c), c); }
 static async Task<(int Emp, int Cand)> Old(AppDbContext d)
 {
  var e = await d.Set<Employee>().OrderBy(x => x.Id).FirstAsync(); e.Status = EmployeeStatus.Resigned;
  d.Add(new StatusHistory { EmployeeId = e.Id, From = EmployeeStatus.Active, To = EmployeeStatus.Resigned, At = new DateTime(2020, 1, 1), ChangedBy = "t" });
  var c = await d.Set<Candidate>().OrderBy(x => x.Id).FirstAsync(); c.Stage = CandidateStage.Rejected; c.CreatedAt = new DateTime(2020, 1, 1);
  await d.SaveChangesAsync(); return (e.Id, c.Id);
 }

 [Fact] public async Task DefaultKeepsEverythingAndRulesAreConfigurableAndAudited()
 {
  await using var d = await Db(); await Old(d);
  Assert.All(await S(d).RulesAsync(), r => Assert.Equal(0, r.Months));
  Assert.Empty(await S(d).ReviewListAsync());
  await S(d).SetRuleAsync("Employees", 24); await S(d).SetRuleAsync("Candidates", 12);
  var list = await S(d).ReviewListAsync();
  Assert.Contains(list, x => x.Category == "Employees"); Assert.Contains(list, x => x.Category == "Candidates");
  await S(d).SetRuleAsync("Employees", 0);
  Assert.DoesNotContain(await S(d).ReviewListAsync(), x => x.Category == "Employees");
  Assert.True(await d.Set<AuditLog>().CountAsync(a => a.Action == "RetentionRule") >= 3);
  await Assert.ThrowsAsync<DomainException>(() => S(d).SetRuleAsync("Payroll", 5));
  await Assert.ThrowsAsync<DomainException>(() => S(d).SetRuleAsync("Employees", -1));
 }

 [Fact] public async Task LegalHoldExcludesEmployeeUntilReleasedAndNothingIsDeleted()
 {
  await using var d = await Db(); var (emp, _) = await Old(d); await S(d).SetRuleAsync("Employees", 24);
  var employeesBefore = await d.Set<Employee>().CountAsync();
  await S(d).PlaceHoldAsync(emp, "Pending dispute");
  await Assert.ThrowsAsync<DomainException>(() => S(d).PlaceHoldAsync(emp, "again"));
  Assert.DoesNotContain(await S(d).ReviewListAsync(), x => x.Category == "Employees");
  var hold = Assert.Single(await S(d).HoldsAsync()); await S(d).ReleaseHoldAsync(hold.Id, "Closed");
  await Assert.ThrowsAsync<DomainException>(() => S(d).ReleaseHoldAsync(hold.Id, "twice"));
  Assert.Contains(await S(d).ReviewListAsync(), x => x.Category == "Employees");
  Assert.Equal(employeesBefore, await d.Set<Employee>().CountAsync());
  Assert.True(await d.Set<AuditLog>().AnyAsync(a => a.Action == "LegalHoldPlaced")); Assert.True(await d.Set<AuditLog>().AnyAsync(a => a.Action == "LegalHoldReleased"));
 }

 [Fact] public async Task AccessAndInputAreChecked()
 {
  await using var d = await Db();
  await Assert.ThrowsAsync<DomainException>(() => S(d, false).RulesAsync());
  await Assert.ThrowsAsync<DomainException>(() => S(d, false).PlaceHoldAsync(1, "x"));
  await Assert.ThrowsAsync<DomainException>(() => S(d).PlaceHoldAsync(1, " "));
  await Assert.ThrowsAsync<DomainException>(() => S(d).PlaceHoldAsync(999999, "x"));
 }
}
