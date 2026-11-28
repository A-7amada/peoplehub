using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>ADR-0022 unit 3a: leave balances and request days are decimals so half days can be stored.</summary>
public class LeaveDecimalBalanceTests
{
    [Fact] public void RemainingKeepsHalfDays()
    {
        var b = new LeaveBalance { Entitled = 21, CarriedOver = 2.5m, Used = 3.5m };
        Assert.Equal(20m, b.Remaining);
        b.Used += 0.5m; Assert.Equal(19.5m, b.Remaining);
    }

    [Fact] public void RolloverCarriesAHalfDayUpToTheMaximum()
    {
        var type = new LeaveType { DefaultDays = 21, MaxCarryOver = 5 };
        var old = new LeaveBalance { EmployeeId = 1, LeaveTypeId = 1, Year = 2026, Entitled = 21, CarriedOver = 0, Used = 17.5m };
        Assert.Equal(3.5m, LeaveBalance.Rollover(old, type).CarriedOver);
        old.Used = 10; Assert.Equal(5m, LeaveBalance.Rollover(old, type).CarriedOver);
    }

    [Fact] public async Task HalfDaysSurviveTheDatabaseRoundTrip()
    {
        await using var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
        var o = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options;
        await using (var d = new AppDbContext(o)) { await d.Database.EnsureCreatedAsync(); await SeedData.SeedAccessAndWorkflowsAsync(d);
            var t = new LeaveType { Code = "ZZ", Name = "Test", NameAr = "تجربة", DefaultDays = 10 }; d.Add(t); await d.SaveChangesAsync();
            d.Add(new LeaveBalance { EmployeeId = 1, LeaveTypeId = t.Id, Year = 2031, Entitled = 10.5m, CarriedOver = 0.5m, Used = 1.5m });
            d.Add(new LeaveTransaction { EmployeeId = 1, LeaveTypeId = t.Id, Year = 2031, Delta = -0.5m, Kind = "Test", Reason = "half", Actor = "t", At = DateTime.UtcNow });
            await d.SaveChangesAsync(); }
        await using (var d = new AppDbContext(o))
        {
            var b = await d.Set<LeaveBalance>().SingleAsync(x => x.Year == 2031);
            Assert.Equal((10.5m, 0.5m, 1.5m, 9.5m), (b.Entitled, b.CarriedOver, b.Used, b.Remaining));
            Assert.Equal(-0.5m, (await d.Set<LeaveTransaction>().SingleAsync(x => x.Year == 2031)).Delta);
        }
    }
}
