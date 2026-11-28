using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Benefits;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH2-015 claims record. Internal record only: no payment, no insurer, no payroll effect.</summary>
public class BenefitClaimsTests
{
 class User(int emp = 1, bool grant = true) : ICurrentUser { public string Email => "claims@example.local"; public int? EmployeeId => emp; public int UserId => emp; public bool IsAuthenticated => true; public bool IsInRole(string r) => false; public bool Can(string p) => p == Perm.BenefitsManage && grant; }
 class Time : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero); }
 static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
 static BenefitClaims C(AppDbContext d, User? u = null) { u ??= new(); var c = new AppClock(new Time()); return new(d, u, c, new AuditService(d, u, c)); }
 static BenefitService B(AppDbContext d) { var u = new User(); var c = new AppClock(new Time()); return new(d, u, c, new AuditService(d, u, c)); }
 static async Task<int> Enrolment(AppDbContext d) { await B(d).AddPlanAsync("Fictional medical plan", "Fictional provider", "Reviewed", "Fictional coverage", false, 500, 100, "EGP"); var plan = await d.Set<BenefitPlan>().Select(x => x.Id).SingleAsync(); await B(d).EnrollAsync(plan, 6, null, new(2026, 11, 24), new(2027, 11, 23), "Reviewed fictional eligibility REF-1", "DEMO-OPS"); return await d.Set<BenefitEnrollment>().Select(x => x.Id).SingleAsync(); }

 [Fact] public async Task RecordNeedsGrantAndValidInputWithinCoverage()
 {
  await using var d = await Db(); var e = await Enrolment(d);
  await Assert.ThrowsAsync<DomainException>(() => C(d, new(6, false)).RecordAsync(e, new(2026, 11, 24), 100, "x"));
  await Assert.ThrowsAsync<DomainException>(() => C(d).RecordAsync(e, new(2026, 11, 24), 0, "x"));
  await Assert.ThrowsAsync<DomainException>(() => C(d).RecordAsync(e, new(2026, 11, 24), 10, " "));
  await Assert.ThrowsAsync<DomainException>(() => C(d).RecordAsync(e, new(2026, 11, 23), 10, "before coverage"));
  await Assert.ThrowsAsync<DomainException>(() => C(d).RecordAsync(e, new(2026, 11, 25), 10, "in the future"));
  await C(d).RecordAsync(e, new(2026, 11, 24), 123.456m, "Fictional clinic visit");
  var c = await d.Set<BenefitClaim>().SingleAsync(); Assert.Equal(123.46m, c.Amount); Assert.Equal("EGP", c.Currency); Assert.Equal("Recorded", c.Status);
 }
 [Fact] public async Task StatusMovesForwardOnlyAndClosingNeedsAnOutcomeAndIsAudited()
 {
  await using var d = await Db(); var e = await Enrolment(d); await C(d).RecordAsync(e, new(2026, 11, 24), 50, "Fictional visit");
  var id = await d.Set<BenefitClaim>().Select(x => x.Id).SingleAsync();
  await Assert.ThrowsAsync<DomainException>(() => C(d, new(6, false)).AdvanceAsync(id, null));
  await C(d).AdvanceAsync(id, null); Assert.Equal("Reviewed", (await d.Set<BenefitClaim>().SingleAsync()).Status);
  await Assert.ThrowsAsync<DomainException>(() => C(d).AdvanceAsync(id, " "));
  await C(d).AdvanceAsync(id, "Fictional outcome note"); Assert.Equal("Closed", (await d.Set<BenefitClaim>().SingleAsync()).Status);
  await Assert.ThrowsAsync<DomainException>(() => C(d).AdvanceAsync(id, "again"));
  Assert.True(await d.Set<AuditLog>().CountAsync(a => a.Entity == "BenefitClaim") >= 3);
 }
 [Fact] public async Task EmployeeSeesOnlyOwnClaimsAndPayrollIsUntouched()
 {
  await using var d = await Db(); var e = await Enrolment(d); var lines = await d.Set<PayrollLine>().CountAsync();
  await C(d).RecordAsync(e, new(2026, 11, 24), 50, "Fictional visit");
  Assert.Single(await C(d, new(6, false)).ListAsync()); Assert.Empty(await C(d, new(7, false)).ListAsync()); Assert.Single(await C(d).ListAsync());
  Assert.Equal(lines, await d.Set<PayrollLine>().CountAsync());
 }
}
