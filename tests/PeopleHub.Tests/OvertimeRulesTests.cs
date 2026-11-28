using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Application.Modules.Requests;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH2-008 configurable overtime caps and rates. Defaults equal the previous fixed values; illustrative, not legally validated.</summary>
public class OvertimeRulesTests
{
    class U(bool grant = true) : ICurrentUser { public string Email => "flows@example.local"; public int? EmployeeId => 1; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => true; public bool Can(string p) => grant; }
    static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }

    static AttendanceService Att(AppDbContext d, bool grant = true) { var u = new U(grant); var c = new AppClock(TimeProvider.System); var a = new AuditService(d, u, c); var n = new NotificationService(d, u, c); return new(d, new AccessScope(d, u), u, a, n, c, new WorkflowService(d, u, a)); }
    static readonly DateTime Mon = new(2026, 3, 2), Sat = new(2026, 3, 7), Today = new(2026, 3, 20);
    static Employee E() => new() { Id = 1 };

    [Fact] public void DefaultsKeepThePreviousBehaviour()
    {
        Assert.Equal(1.5m, OvertimeRequest.Create(E(), Mon, 8, "x", Today).Multiplier);
        Assert.Equal(2m, OvertimeRequest.Create(E(), Sat, 1, "x", Today).Multiplier);
        Assert.Throws<DomainException>(() => OvertimeRequest.Create(E(), Mon, 8.5m, "x", Today));
    }
    [Fact] public void ConfiguredDailyCapAndRatesApply()
    {
        var rules = new OvertimeRules(4m, null, 1.25m, 1.75m);
        Assert.Throws<DomainException>(() => OvertimeRequest.Create(E(), Mon, 5, "x", Today, rules));
        Assert.Equal(1.25m, OvertimeRequest.Create(E(), Mon, 4, "x", Today, rules).Multiplier);
        Assert.Equal(1.75m, OvertimeRequest.Create(E(), Sat, 1, "x", Today, rules).Multiplier);
    }
    [Fact] public async Task RulesNeedGrantValidateRoundTripAndAreAudited()
    {
        await using var d = await Db();
        await Assert.ThrowsAsync<DomainException>(() => Att(d, false).SetOvertimeRulesAsync(EmploymentType.FullTime, 4, 20, 1.25m, 1.75m));
        await Assert.ThrowsAsync<DomainException>(() => Att(d).SetOvertimeRulesAsync(EmploymentType.FullTime, 30, null, null, null));
        await Assert.ThrowsAsync<DomainException>(() => Att(d).SetOvertimeRulesAsync(EmploymentType.FullTime, null, null, 0.5m, null));
        Assert.Equal(new OvertimeRules(), await Att(d).OvertimeRulesAsync(EmploymentType.FullTime));
        await Att(d).SetOvertimeRulesAsync(EmploymentType.FullTime, 4, 20, 1.25m, 1.75m);
        Assert.Equal(new OvertimeRules(4m, 20m, 1.25m, 1.75m), await Att(d).OvertimeRulesAsync(EmploymentType.FullTime));
        Assert.Equal(new OvertimeRules(), await Att(d).OvertimeRulesAsync(EmploymentType.PartTime));
        await Att(d).SetOvertimeRulesAsync(EmploymentType.FullTime, null, null, null, null);
        Assert.Equal(new OvertimeRules(), await Att(d).OvertimeRulesAsync(EmploymentType.FullTime));
        Assert.True(await d.Set<AuditLog>().AnyAsync(a => a.Action == "OvertimeRules"));
    }
}
