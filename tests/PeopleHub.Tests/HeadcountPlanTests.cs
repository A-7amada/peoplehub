using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Organization;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH3-011 read-only headcount and cost plan.</summary>
public class HeadcountPlanTests
{
 class User(bool grant = true) : ICurrentUser { public string Email => "hr1@example.local"; public int? EmployeeId => 1; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => false; public bool Can(string p) => p == Perm.OrgManage && grant; }
 static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
 [Fact] public async Task GapCostIsGapTimesAverageBasicAndNegativeGapCostsNothing()
 {
  await using var d = await Db();
  var rows = await new HeadcountPlan(d, new User()).BuildAsync();
  Assert.NotEmpty(rows);
  foreach (var r in rows)
  {
   var n = await d.Set<Employee>().CountAsync(e => e.DepartmentId == r.Dept.Id && (e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave));
   Assert.Equal(n, r.Active); Assert.Equal(r.Target - n, r.Gap);
   Assert.Equal(Math.Max(0, r.Gap) * r.AvgBasic, r.GapMonthlyCost);
  }
 }
 [Fact] public async Task NeedsOrgManage()
 {
  await using var d = await Db();
  await Assert.ThrowsAsync<DomainException>(() => new HeadcountPlan(d, new User(false)).BuildAsync());
 }
}
