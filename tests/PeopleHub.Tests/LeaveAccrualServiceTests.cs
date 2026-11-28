using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Leave;
using PeopleHub.Application.Modules.Requests;
using PeopleHub.Domain.Entities;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>ADR-0022 unit 3c service wiring: accrual and carry expiry are ledgered, keep the ledger equal to the balance, and are idempotent.</summary>
public class LeaveAccrualServiceTests
{
    class User : ICurrentUser { public string Email => "leave@example.local"; public int? EmployeeId => 6; public int UserId => 1; public bool IsAuthenticated => true; public bool IsInRole(string r) => true; public bool Can(string p) => true; }
    static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
    static LeaveService S(AppDbContext d) { var u = new User(); var c = new AppClock(TimeProvider.System); var a = new AuditService(d, u, c); return new(d, new AccessScope(d, u), u, a, new NotificationService(d, u, c), c, new WorkflowService(d, u, a)); }

    [Fact] public async Task ExpiryRemovesUnusedCarryOnce_LedgerStaysEqualToBalance_AndRepeatIsANoOp()
    {
        await using var d = await Db();
        var type = await d.Set<LeaveType>().FirstAsync(t => t.DeductsBalance);
        d.Add(new LeavePolicy { LeaveTypeId = type.Id, Enabled = true, Accrual = "Annual", CarryExpiryMonths = 3, WaitingMonths = 0 });
        await d.SaveChangesAsync();
        var s = S(d);
        var b = (await s.BalancesAsync(6, 2025)).First(x => x.LeaveTypeId == type.Id);
        b.CarriedOver = 5; b.Used = 2; await d.SaveChangesAsync();
        var before = b.Remaining;
        var again = (await s.BalancesAsync(6, 2025)).First(x => x.LeaveTypeId == type.Id);
        Assert.Equal(2, again.CarriedOver);               // 5 carried, 2 used first, 3 lapse
        Assert.Equal(before - 3, again.Remaining);
        var rows = await d.Set<LeaveTransaction>().Where(x => x.EmployeeId == 6 && x.Year == 2025 && x.LeaveTypeId == type.Id).ToListAsync();
        Assert.Single(rows, x => x.Kind == "Expiry" && x.Delta == -3);
        var count = rows.Count;
        await s.BalancesAsync(6, 2025);
        Assert.Equal(count, await d.Set<LeaveTransaction>().CountAsync(x => x.EmployeeId == 6 && x.Year == 2025 && x.LeaveTypeId == type.Id));
    }

    [Fact] public async Task MonthlyPolicyOpensWithAccruedNotFullEntitlement_AndNeverLowersAnExistingBalance()
    {
        await using var d = await Db();
        var type = await d.Set<LeaveType>().FirstAsync(t => t.DeductsBalance);
        d.Add(new LeavePolicy { LeaveTypeId = type.Id, Enabled = true, Accrual = "Monthly", CarryExpiryMonths = 0, WaitingMonths = 0 });
        await d.SaveChangesAsync();
        var s = S(d);
        var future = (await s.BalancesAsync(6, 2030)).First(x => x.LeaveTypeId == type.Id);
        Assert.Equal(0m, future.Entitled);                // 1 January 2030: nothing accrued yet
        var ledger = await d.Set<LeaveTransaction>().Where(x => x.EmployeeId == 6 && x.Year == 2030 && x.LeaveTypeId == type.Id).SumAsync(x => x.Delta);
        Assert.Equal(future.Remaining, ledger);
        var existing = (await d.Set<LeaveBalance>().FirstAsync(x => x.EmployeeId == 6 && x.LeaveTypeId == type.Id && x.Year == 2026));
        var e0 = existing.Entitled;
        await s.BalancesAsync(6, 2026);
        Assert.True((await d.Set<LeaveBalance>().FirstAsync(x => x.Id == existing.Id)).Entitled >= e0);
    }
}
