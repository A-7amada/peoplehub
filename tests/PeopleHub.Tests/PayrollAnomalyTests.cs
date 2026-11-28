using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Payroll;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH3-015 rule-based review flags with configurable thresholds. Read-only on payroll.</summary>
public class PayrollAnomalyTests
{
 class U(bool view = true, bool set = true) : ICurrentUser { public string Email => "anom@example.local"; public int? EmployeeId => 1; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => false; public bool Can(string p) => (p == Perm.PayView && view) || (p == Perm.AdmSettings && set); }
 class Time : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero); }
 static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
 static PayrollAnomalies S(AppDbContext d, U? u = null) { u ??= new(); return new(d, u, new AuditService(d, u, new AppClock(new Time()))); }
 static async Task<(int Prev, int Cur)> Runs(AppDbContext d)
 {
  var a = new PayrollRun { Year = 2031, Month = 1, CreatedBy = "t" }; var b = new PayrollRun { Year = 2031, Month = 2, CreatedBy = "t" }; d.Add(a); d.Add(b); await d.SaveChangesAsync();
  d.Add(new PayrollLine { RunId = a.Id, EmployeeId = 1, EmployeeCodeSnapshot = "E1", EmployeeNameSnapshot = "Demo One", Gross = 1000, Net = 1000 });
  d.Add(new PayrollLine { RunId = b.Id, EmployeeId = 1, EmployeeCodeSnapshot = "E1", EmployeeNameSnapshot = "Demo One", Gross = 1000, Net = 1500, LateMinutes = 300, AbsentDays = 4, OvertimeHours = 50 });
  d.Add(new PayrollLine { RunId = b.Id, EmployeeId = 2, EmployeeCodeSnapshot = "E2", EmployeeNameSnapshot = "Demo Two", Gross = 500, TotalDeductions = 700, Net = -200 });
  await d.SaveChangesAsync(); return (a.Id, b.Id);
 }

 [Fact] public async Task DefaultsFlagEachRuleAndNeverChangePayroll()
 {
  await using var d = await Db(); var (_, cur) = await Runs(d); var before = await d.Set<PayrollLine>().CountAsync();
  var found = await S(d).ScanAsync(cur);
  foreach (var r in new[] { "NetChange", "LateMinutes", "AbsentDays", "OvertimeHours", "NetNotPositive", "DeductionsAboveGross" }) Assert.Contains(found, f => f.Rule == r);
  Assert.Equal(before, await d.Set<PayrollLine>().CountAsync()); Assert.Equal(PayrollStatus.Draft, (await d.Set<PayrollRun>().FindAsync(cur))!.Status);
 }

 [Fact] public async Task ThresholdsAreConfigurableZeroTurnsRuleOffAndChangeIsAudited()
 {
  await using var d = await Db(); var (_, cur) = await Runs(d);
  Assert.Equal(new AnomalyThresholds(), await S(d).ThresholdsAsync());
  await S(d).SetThresholdsAsync(0, 0, 0, 0);
  var found = await S(d).ScanAsync(cur);
  Assert.DoesNotContain(found, f => f.Rule is "NetChange" or "LateMinutes" or "AbsentDays" or "OvertimeHours");
  Assert.Contains(found, f => f.Rule == "NetNotPositive");
  await S(d).SetThresholdsAsync(80, 400, 5, 60);
  Assert.Empty((await S(d).ScanAsync(cur)).Where(f => f.Rule is "NetChange" or "LateMinutes" or "AbsentDays" or "OvertimeHours"));
  Assert.True(await d.Set<AuditLog>().AnyAsync(a => a.Action == "AnomalyThresholds"));
 }

 [Fact] public async Task AccessAndInputAreChecked()
 {
  await using var d = await Db(); var (_, cur) = await Runs(d);
  await Assert.ThrowsAsync<DomainException>(() => S(d, new(false, true)).ScanAsync(cur));
  await Assert.ThrowsAsync<DomainException>(() => S(d, new(true, false)).SetThresholdsAsync(10, 10, 1, 1));
  await Assert.ThrowsAsync<DomainException>(() => S(d).SetThresholdsAsync(-1, 10, 1, 1));
  await Assert.ThrowsAsync<DomainException>(() => S(d).SetThresholdsAsync(10, 10, 40, 1));
  await Assert.ThrowsAsync<DomainException>(() => S(d).ScanAsync(9999));
 }
}
