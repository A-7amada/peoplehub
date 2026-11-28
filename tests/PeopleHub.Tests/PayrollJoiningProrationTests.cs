using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Application.Modules.Payroll;
using PeopleHub.Application.Modules.Requests;
using PeopleHub.Domain.Entities;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH2-009 optional joining-month proration. Default off keeps the previous full amounts.</summary>
public class PayrollJoiningProrationTests
{
 class User(string email, bool settings = true) : ICurrentUser { public string Email => email; public int? EmployeeId => 6; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => true; public bool Can(string p) => p != Perm.AdmSettings || settings; }
 static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
 static PayrollService Pay(AppDbContext d, string email = "prep@example.local", bool settings = true) { var u = new User(email, settings); var c = new AppClock(TimeProvider.System); var a = new AuditService(d, u, c); var n = new NotificationService(d, u, c); return new(d, u, a, n, new AttendanceService(d, new AccessScope(d, u), u, a, n, c, new WorkflowService(d, u, a)), c); }

 [Theory] [InlineData(true, 2031, 5, 16, 16.0 / 30)] [InlineData(true, 2031, 5, 1, 1.0)] [InlineData(true, 2031, 6, 20, 1.0)] [InlineData(true, 2031, 5, 2, 30.0 / 30)] [InlineData(true, 2031, 4, 20, 11.0 / 30)] [InlineData(false, 2031, 5, 16, 1.0)]
 public void FactorIsCalendarDaysOverThirtyNeverAboveOne(bool on, int y, int m, int day, double expected)
 {
  // the 6/20 row hires in May but runs June (already employed all month); the others hire inside the run month
  var hire = new DateTime(2031, m == 6 ? 5 : m, day);
  Assert.Equal(expected, (double)PayrollService.JoiningFactor(on, hire, y, m), 6);
 }

 [Fact] public async Task OffKeepsFullBasicOnProratesAMidMonthHireAndRecalculates()
 {
  await using var d = await Db(); var p = Pay(d);
  var e = await d.Set<Employee>().Where(x => x.Status == EmployeeStatus.Active).OrderBy(x => x.Id).FirstAsync(); e.HireDate = new DateTime(2031, 5, 16); await d.SaveChangesAsync();
  var id = await p.CreateAsync(2031, 5);
  Assert.Equal(e.BasicSalary, (await d.Set<PayrollLine>().SingleAsync(l => l.RunId == id && l.EmployeeId == e.Id)).Basic);
  await Pay(d, "owner@example.local").SetProrateJoiningAsync(true); Assert.True(await p.ProrateJoiningAsync());
  await p.RecalculateAsync(id);
  Assert.Equal(PayrollCalculator.Round(e.BasicSalary * 16m / 30m), (await d.Set<PayrollLine>().SingleAsync(l => l.RunId == id && l.EmployeeId == e.Id)).Basic);
  await Pay(d, "owner@example.local").SetProrateJoiningAsync(false); await p.RecalculateAsync(id);
  Assert.Equal(e.BasicSalary, (await d.Set<PayrollLine>().SingleAsync(l => l.RunId == id && l.EmployeeId == e.Id)).Basic);
  Assert.Equal(2, await d.Set<AuditLog>().CountAsync(a => a.Action == "PayrollProrateJoining"));
 }

 [Fact] public async Task ChangingTheSettingNeedsAccess()
 {
  await using var d = await Db();
  await Assert.ThrowsAsync<DomainException>(() => Pay(d, "x@example.local", false).SetProrateJoiningAsync(true));
  Assert.False(await Pay(d).ProrateJoiningAsync());
 }
}
