using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;
public class CategoryGraceTests
{
    static readonly DateTime D = new(2026, 3, 2);
    static Shift S() => new() { Start = new(9, 0, 0), End = new(17, 0, 0), GraceMinutes = 10, MaxLateMinutes = 120 };
    [Fact] public void NoOverrideUsesShiftGrace() => Assert.Equal(AttendanceStatus.Late, AttendanceCalculator.Calculate(S(), D, D.AddHours(9).AddMinutes(20), D.AddHours(17)).Status);
    [Fact] public void LongerCategoryGraceForgivesLateness() { var r = AttendanceCalculator.Calculate(S(), D, D.AddHours(9).AddMinutes(20), D.AddHours(17), 30); Assert.Equal(0, r.LateMinutes); Assert.Equal(AttendanceStatus.Present, r.Status); }
    [Fact] public void ShorterCategoryGraceIsStricter() => Assert.Equal(8, AttendanceCalculator.Calculate(S(), D, D.AddHours(9).AddMinutes(8), D.AddHours(17), 5).LateMinutes);
    [Fact] public void RecomputeAppliesOverrideAndKeepsEvidenceFields()
    {
        var rec = new AttendanceRecord { Date = D, CheckIn = D.AddHours(9).AddMinutes(25), CheckOut = D.AddHours(17) };
        rec.Recompute(S(), 30); Assert.Equal(0, rec.LateMinutes); rec.Recompute(S()); Assert.Equal(25, rec.LateMinutes);
    }
    class U(bool grant = true) : ICurrentUser { public string Email => "flows@example.local"; public int? EmployeeId => 1; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => true; public bool Can(string p) => grant; }
    static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
    static ShiftSchedule Sch(AppDbContext d, bool grant = true) { var u = new U(grant); var c = new AppClock(TimeProvider.System); return new(d, u, c, new AuditService(d, u, c)); }
    [Fact] public async Task SettingNeedsGrantValidatesRoundTripsAndIsAudited()
    {
        await using var d = await Db();
        await Assert.ThrowsAsync<DomainException>(() => Sch(d, false).SetGraceAsync(EmploymentType.PartTime, 20));
        await Assert.ThrowsAsync<DomainException>(() => Sch(d).SetGraceAsync(EmploymentType.PartTime, 241));
        Assert.Empty(await Sch(d).GracesAsync());
        await Sch(d).SetGraceAsync(EmploymentType.PartTime, 20);Assert.Equal(20, (await Sch(d).GracesAsync())[EmploymentType.PartTime]);
        await Sch(d).SetGraceAsync(EmploymentType.PartTime, null);Assert.Empty(await Sch(d).GracesAsync());
        Assert.True(await d.Set<AuditLog>().AnyAsync(a => a.Action == "AttendanceGrace"));
    }
}
