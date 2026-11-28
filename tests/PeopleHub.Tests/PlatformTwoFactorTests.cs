using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Common.Tenancy;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Security;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH1-006 platform staff two-step verification: optional per account, replay-guarded, recovery codes stored hashed and usable once.</summary>
public class PlatformTwoFactorTests
{
    static async Task<(SqliteConnection, AppDbContext, PlatformAuthService, int)> Setup()
    {
        var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
        var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options, new FixedTenantContext(1));
        await d.Database.EnsureCreatedAsync();
        await PlatformAuthService.SeedFirstAsync(d, "staff@p.test", "Passw0rdX");
        return (c, d, new PlatformAuthService(d), await d.Set<PlatformUser>().Select(x => x.Id).SingleAsync());
    }
    static string Code(string secret, DateTime t) => Totp.Code(secret, new DateTimeOffset(t).ToUnixTimeSeconds());

    [Fact] public async Task WithoutTwoFactorPasswordAloneStillWorks()
    {
        var (c, d, s, _) = await Setup(); await using var _c = c; await using var _d = d;
        Assert.NotNull(await s.LoginAsync("staff@p.test", "Passw0rdX", DateTime.UtcNow));
    }

    [Fact] public async Task EnableNeedsAValidCodeThenLoginNeedsPasswordAndCode()
    {
        var (c, d, s, id) = await Setup(); await using var _c = c; await using var _d = d;
        var now = new DateTime(2026, 11, 28, 10, 0, 0, DateTimeKind.Utc);
        var secret = await s.BeginTotpAsync(id);
        await Assert.ThrowsAsync<DomainException>(() => s.EnableTotpAsync(id, "000000", now));
        var stamp0 = (await d.Set<PlatformUser>().AsNoTracking().SingleAsync()).SecurityStamp;
        var codes = await s.EnableTotpAsync(id, Code(secret, now), now);
        Assert.Equal(10, codes.Length);
        var u = await d.Set<PlatformUser>().AsNoTracking().SingleAsync(); Assert.True(u.TotpEnabled); Assert.NotEqual(stamp0, u.SecurityStamp);
        Assert.DoesNotContain(codes[0].Replace("-", ""), u.RecoveryHashes);
        var later = now.AddMinutes(2);
        Assert.Null(await s.LoginAsync("staff@p.test", "Passw0rdX", later));
        Assert.Null(await s.LoginAsync("staff@p.test", "Passw0rdX", later, "123456"));
        Assert.Null(await s.LoginAsync("staff@p.test", "wrong", later, Code(secret, later)));
        Assert.NotNull(await s.LoginAsync("staff@p.test", "Passw0rdX", later, Code(secret, later)));
        Assert.Null(await s.LoginAsync("staff@p.test", "Passw0rdX", later, Code(secret, later)));
    }

    [Fact] public async Task RecoveryCodeWorksOnceAndWrongSecondFactorCountsTowardLock()
    {
        var (c, d, s, id) = await Setup(); await using var _c = c; await using var _d = d;
        var now = new DateTime(2026, 11, 28, 10, 0, 0, DateTimeKind.Utc);
        var secret = await s.BeginTotpAsync(id); var codes = await s.EnableTotpAsync(id, Code(secret, now), now);
        Assert.NotNull(await s.LoginAsync("staff@p.test", "Passw0rdX", now, codes[0]));
        Assert.Null(await s.LoginAsync("staff@p.test", "Passw0rdX", now, codes[0]));
        Assert.NotNull(await s.LoginAsync("staff@p.test", "Passw0rdX", now, codes[1].ToLowerInvariant()));
        for (var i = 0; i < 5; i++) await s.LoginAsync("staff@p.test", "Passw0rdX", now, "111111");
        Assert.Null(await s.LoginAsync("staff@p.test", "Passw0rdX", now, codes[2]));
        Assert.NotNull(await s.LoginAsync("staff@p.test", "Passw0rdX", now.AddMinutes(16), codes[2]));
    }

    [Fact] public async Task DisableNeedsThePasswordAndClearsEverything()
    {
        var (c, d, s, id) = await Setup(); await using var _c = c; await using var _d = d;
        var now = new DateTime(2026, 11, 28, 10, 0, 0, DateTimeKind.Utc);
        var secret = await s.BeginTotpAsync(id); await s.EnableTotpAsync(id, Code(secret, now), now);
        await Assert.ThrowsAsync<DomainException>(() => s.BeginTotpAsync(id));
        await Assert.ThrowsAsync<DomainException>(() => s.DisableTotpAsync(id, "wrong"));
        await s.DisableTotpAsync(id, "Passw0rdX");
        var u = await d.Set<PlatformUser>().AsNoTracking().SingleAsync(); Assert.False(u.TotpEnabled); Assert.Null(u.TotpSecret); Assert.Equal("", u.RecoveryHashes);
        Assert.NotNull(await s.LoginAsync("staff@p.test", "Passw0rdX", now));
    }
}
