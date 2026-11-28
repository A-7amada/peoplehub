using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Leave;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH2-004 leave encashment as a days-only approval record.</summary>
public class LeaveEncashmentTests
{
 class User(string email = "hr1@example.local", bool grant = true) : ICurrentUser { public string Email => email; public int? EmployeeId => 1; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => false; public bool Can(string p) => p == Perm.LeaveAdmin && grant; }
 class Time : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero); }
 static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
 static LeaveEncashments S(AppDbContext d, string email = "hr1@example.local", bool grant = true) { var u = new User(email, grant); var c = new AppClock(new Time()); return new(d, u, new AuditService(d, u, c), c); }
 static async Task<(int Emp, int Type, int Year, decimal Remaining)> Bal(AppDbContext d) { var b = await d.Set<LeaveBalance>().Where(x => x.Entitled + x.CarriedOver - x.Used >= 5).OrderBy(x => x.Id).FirstAsync(); return (b.EmployeeId, b.LeaveTypeId, b.Year, b.Remaining); }

 [Fact] public async Task OffByDefaultAndMaxIsConfigurable()
 {
  await using var d = await Db(); var (e, t, y, _) = await Bal(d);
  await Assert.ThrowsAsync<DomainException>(() => S(d).RequestAsync(e, t, y, 1));
  await S(d).SetMaxDaysAsync(t, 3);
  await S(d).RequestAsync(e, t, y, 2);
  await Assert.ThrowsAsync<DomainException>(() => S(d).RequestAsync(e, t, y, 2));
  await Assert.ThrowsAsync<DomainException>(() => S(d).SetMaxDaysAsync(t, -1));
  Assert.True(await d.Set<AuditLog>().AnyAsync(a => a.Action == "LeaveEncashMax"));
 }

 [Fact] public async Task ApprovalNeedsAnotherPersonDeductsDaysAndWritesLedgerOnly()
 {
  await using var d = await Db(); var (e, t, y, rem) = await Bal(d); await S(d).SetMaxDaysAsync(t, 5);
  await S(d).RequestAsync(e, t, y, 2); var r = await d.Set<LeaveEncashment>().SingleAsync();
  await Assert.ThrowsAsync<DomainException>(() => S(d).DecideAsync(r.Id, true, "ok"));
  await Assert.ThrowsAsync<DomainException>(() => S(d, "hr2@example.local").DecideAsync(r.Id, true, " "));
  var payrollLines = await d.Set<PayrollLine>().CountAsync();
  await S(d, "hr2@example.local").DecideAsync(r.Id, true, "ok");
  Assert.Equal(rem - 2, (await d.Set<LeaveBalance>().SingleAsync(x => x.EmployeeId == e && x.LeaveTypeId == t && x.Year == y)).Remaining);
  Assert.True(await d.Set<LeaveTransaction>().AnyAsync(x => x.EmployeeId == e && x.Kind == "Encashed" && x.Delta == -2));
  Assert.Equal(payrollLines, await d.Set<PayrollLine>().CountAsync());
  await Assert.ThrowsAsync<DomainException>(() => S(d, "hr2@example.local").DecideAsync(r.Id, false, "again"));
 }

 [Fact] public async Task RejectCancelAndAccessLeaveBalanceAlone()
 {
  await using var d = await Db(); var (e, t, y, rem) = await Bal(d); await S(d).SetMaxDaysAsync(t, 5);
  await S(d).RequestAsync(e, t, y, 1); await S(d).RequestAsync(e, t, y, 1);
  var rows = await d.Set<LeaveEncashment>().OrderBy(x => x.Id).ToListAsync();
  await S(d, "hr2@example.local").DecideAsync(rows[0].Id, false, "no"); await S(d).CancelAsync(rows[1].Id);
  Assert.Equal(rem, (await d.Set<LeaveBalance>().SingleAsync(x => x.EmployeeId == e && x.LeaveTypeId == t && x.Year == y)).Remaining);
  await Assert.ThrowsAsync<DomainException>(() => S(d, "x@example.local", false).ListAsync());
  await Assert.ThrowsAsync<DomainException>(() => S(d).RequestAsync(e, t, y, rem + 1));
 }
}
