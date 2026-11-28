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

/// <summary>US-PH1-009 optional strict separation of payroll approvers. Default off.</summary>
public class PayrollDistinctApproversTests
{
 class User(string email, bool settings = true) : ICurrentUser { public string Email => email; public int? EmployeeId => 6; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => true; public bool Can(string p) => p != Perm.AdmSettings || settings; }
 static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
 static PayrollService Pay(AppDbContext d, string email, bool settings = true) { var u = new User(email, settings); var c = new AppClock(TimeProvider.System); var a = new AuditService(d, u, c); var n = new NotificationService(d, u, c); return new(d, u, a, n, new AttendanceService(d, new AccessScope(d, u), u, a, n, c, new WorkflowService(d, u, a)), c); }

 [Fact] public async Task DefaultOffAllowsTheSameReviewerToApproveAndFinalize()
 {
  await using var d = await Db(); var prep = Pay(d, "prep@example.local");
  Assert.False(await prep.DistinctApproversAsync());
  var id = await prep.CreateAsync(2031, 3); var hr = Pay(d, "hr@example.local");
  await hr.ReviewAsync(id); await hr.FinanceApproveAsync(id); await hr.FinalizeAsync(id);
  Assert.Equal(PayrollStatus.Finalized, (await d.Set<PayrollRun>().SingleAsync(r => r.Id == id)).Status);
 }

 [Fact] public async Task StrictModeKeepsReviewerFinanceAndFinalizerApart()
 {
  await using var d = await Db(); var prep = Pay(d, "prep@example.local");
  await Pay(d, "owner@example.local").SetDistinctApproversAsync(true);
  Assert.True(await prep.DistinctApproversAsync());
  var id = await prep.CreateAsync(2031, 4); var hr = Pay(d, "hr@example.local"); var fin = Pay(d, "fin@example.local");
  await hr.ReviewAsync(id);
  await Assert.ThrowsAsync<DomainException>(() => hr.FinanceApproveAsync(id));
  await fin.FinanceApproveAsync(id);
  await Assert.ThrowsAsync<DomainException>(() => hr.FinalizeAsync(id));
  await Assert.ThrowsAsync<DomainException>(() => fin.FinalizeAsync(id));
  await Pay(d, "closer@example.local").FinalizeAsync(id);
  Assert.Equal(PayrollStatus.Finalized, (await d.Set<PayrollRun>().SingleAsync(r => r.Id == id)).Status);
 }

 [Fact] public async Task ChangingTheSettingNeedsAccessIsAuditedAndLeavesPastApprovalsAlone()
 {
  await using var d = await Db();
  await Assert.ThrowsAsync<DomainException>(() => Pay(d, "x@example.local", false).SetDistinctApproversAsync(true));
  await Pay(d, "owner@example.local").SetDistinctApproversAsync(true); await Pay(d, "owner@example.local").SetDistinctApproversAsync(false);
  Assert.Equal(2, await d.Set<AuditLog>().CountAsync(a => a.Action == "PayrollDistinctApprovers"));
  Assert.False(await Pay(d, "owner@example.local").DistinctApproversAsync());
 }
}
