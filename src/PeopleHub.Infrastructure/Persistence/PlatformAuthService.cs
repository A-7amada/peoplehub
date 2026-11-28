using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Security;
namespace PeopleHub.Infrastructure.Persistence;

/// <summary>Platform staff sign-in (ADR-0023 stage 3, ADR-0004). Same lock rule as customer users: five wrong passwords lock the account for 15 minutes.</summary>
public sealed class PlatformAuthService(AppDbContext db)
{
    public async Task<PlatformUser?> LoginAsync(string email, string password, DateTime now, string? code = null)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var u = await db.Set<PlatformUser>().FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
        if (u == null) { PasswordHasher.Verify(password ?? "", DummyHash); return null; }
        if (u.LockedUntil > now) return null;
        var ok = PasswordHasher.Verify(password ?? "", u.PasswordHash) && (!u.TotpEnabled || SecondFactorOk(u, code, now));
        if (!ok)
        {
            u.FailedLoginCount++; if (u.FailedLoginCount >= 5) { u.LockedUntil = now.AddMinutes(15); u.FailedLoginCount = 0; }
            await db.SaveChangesAsync(); return null;
        }
        u.FailedLoginCount = 0; u.LockedUntil = null; await db.SaveChangesAsync(); return u;
    }

    /// <summary>A current authenticator code (each time step usable once) or an unused recovery code (usable once).</summary>
    static bool SecondFactorOk(PlatformUser u, string? code, DateTime now)
    {
        if (u.TotpSecret == null || string.IsNullOrWhiteSpace(code)) return false;
        var step = Totp.MatchStep(u.TotpSecret, code, now);
        if (step != null) { if (step <= u.LastTotpStep) return false; u.LastTotpStep = step.Value; return true; }
        var h = HashRecovery(code); var list = u.RecoveryHashes.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (!list.Remove(h)) return false;
        u.RecoveryHashes = string.Join('\n', list); return true;
    }
    public static string HashRecovery(string code) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes((code ?? "").Replace("-", "").Replace(" ", "").ToUpperInvariant())));

    public async Task<string> BeginTotpAsync(int userId)
    {
        var u = await db.Set<PlatformUser>().FirstAsync(x => x.Id == userId && x.IsActive);
        if (u.TotpEnabled) throw new DomainException("err.forbidden");
        u.TotpSecret = Totp.NewSecret(); await db.SaveChangesAsync(); return u.TotpSecret;
    }

    /// <summary>Confirms the first code, turns 2FA on, signs out older sessions (new stamp) and returns ten recovery codes, shown once.</summary>
    public async Task<string[]> EnableTotpAsync(int userId, string code, DateTime now)
    {
        var u = await db.Set<PlatformUser>().FirstAsync(x => x.Id == userId && x.IsActive);
        var step = u.TotpSecret == null ? null : Totp.MatchStep(u.TotpSecret, code, now);
        if (u.TotpEnabled || step == null) throw new DomainException("err.badCode");
        var codes = Enumerable.Range(0, 10).Select(_ => { var t = Totp.Base32(System.Security.Cryptography.RandomNumberGenerator.GetBytes(7))[..10]; return t[..5] + "-" + t[5..]; }).ToArray();
        u.TotpEnabled = true; u.LastTotpStep = step.Value; u.RecoveryHashes = string.Join('\n', codes.Select(HashRecovery)); u.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(); return codes;
    }

    public async Task DisableTotpAsync(int userId, string password)
    {
        var u = await db.Set<PlatformUser>().FirstAsync(x => x.Id == userId && x.IsActive);
        if (!PasswordHasher.Verify(password ?? "", u.PasswordHash)) throw new DomainException("err.badCode");
        u.TotpEnabled = false; u.TotpSecret = null; u.RecoveryHashes = ""; u.LastTotpStep = 0; u.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync();
    }

    public Task<PlatformUser?> FindAsync(int userId) => db.Set<PlatformUser>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
    static readonly string DummyHash = PasswordHasher.Hash("not-a-real-password");

    /// <summary>First platform admin from optional environment settings; does nothing when they are absent or the account exists.</summary>
    public static async Task SeedFirstAsync(AppDbContext db, string? email, string? password)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        if (email.Length == 0 || string.IsNullOrEmpty(password) || password.Length < 8) return;
        if (await db.Set<PlatformUser>().AnyAsync(x => x.Email == email)) return;
        db.Add(new PlatformUser { Email = email, PasswordHash = PasswordHasher.Hash(password), CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
}
