using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common.Tenancy;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>ADR-0023 stage 3b: platform staff sign-in is separate from customer sign-in.</summary>
public class PlatformAuthTests
{
    static async Task<(SqliteConnection, AppDbContext)> Db()
    {
        var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
        var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options, new FixedTenantContext(1));
        await d.Database.EnsureCreatedAsync(); return (c, d);
    }
    [Fact] public async Task SeedIsOptionalAndIdempotent_AndLoginWorks()
    {
        var (c, d) = await Db(); await using var _ = c; await using var __ = d;
        await PlatformAuthService.SeedFirstAsync(d, null, null); await PlatformAuthService.SeedFirstAsync(d, "x@p.test", "short");
        Assert.Empty(await d.Set<PlatformUser>().ToListAsync());
        await PlatformAuthService.SeedFirstAsync(d, "Staff@P.test", "Passw0rdX"); await PlatformAuthService.SeedFirstAsync(d, "staff@p.test", "Other0rdX");
        Assert.Single(await d.Set<PlatformUser>().ToListAsync());
        var s = new PlatformAuthService(d); var now = DateTime.UtcNow;
        Assert.NotNull(await s.LoginAsync("STAFF@p.test", "Passw0rdX", now));
        Assert.Null(await s.LoginAsync("staff@p.test", "wrong", now)); Assert.Null(await s.LoginAsync("nobody@p.test", "Passw0rdX", now));
    }
    [Fact] public async Task FiveWrongPasswordsLockForFifteenMinutes()
    {
        var (c, d) = await Db(); await using var _ = c; await using var __ = d;
        await PlatformAuthService.SeedFirstAsync(d, "staff@p.test", "Passw0rdX"); var s = new PlatformAuthService(d); var now = DateTime.UtcNow;
        for (var i = 0; i < 5; i++) Assert.Null(await s.LoginAsync("staff@p.test", "bad", now));
        Assert.Null(await s.LoginAsync("staff@p.test", "Passw0rdX", now.AddMinutes(5)));
        Assert.NotNull(await s.LoginAsync("staff@p.test", "Passw0rdX", now.AddMinutes(16)));
    }
    [Fact] public async Task PlatformStaffAreNotCustomerUsers()
    {
        var (c, d) = await Db(); await using var _ = c; await using var __ = d;
        await PlatformAuthService.SeedFirstAsync(d, "staff@p.test", "Passw0rdX");
        Assert.Empty(await d.Set<AppUser>().Where(u => u.Email == "staff@p.test").ToListAsync());
    }
}
