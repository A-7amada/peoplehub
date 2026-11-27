using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Security;
namespace PeopleHub.Infrastructure.Persistence;

/// <summary>Platform staff sign-in (ADR-0023 stage 3, ADR-0004). Same lock rule as customer users: five wrong passwords lock the account for 15 minutes.</summary>
public sealed class PlatformAuthService(AppDbContext db)
{
    public async Task<PlatformUser?> LoginAsync(string email, string password, DateTime now)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var u = await db.Set<PlatformUser>().FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
        if (u == null) { PasswordHasher.Verify(password ?? "", DummyHash); return null; }
        if (u.LockedUntil > now) return null;
        if (!PasswordHasher.Verify(password ?? "", u.PasswordHash))
        {
            u.FailedLoginCount++; if (u.FailedLoginCount >= 5) { u.LockedUntil = now.AddMinutes(15); u.FailedLoginCount = 0; }
            await db.SaveChangesAsync(); return null;
        }
        u.FailedLoginCount = 0; u.LockedUntil = null; await db.SaveChangesAsync(); return u;
    }
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
