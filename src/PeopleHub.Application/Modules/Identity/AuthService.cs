using PeopleHub.Application.Modules.Platform;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Domain.Security;

using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Identity;

public class AuthService(IRepository repo, AppClock clock)
{
    public async Task<AppUser?> LoginAsync(string email, string password)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var u = await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x => x.Email == email && x.IsActive));
        if(u==null) return null;
        if(u.LockedUntil>clock.UtcNow) return null;
        if(!PasswordHasher.Verify(password ?? "",u.PasswordHash))
        {
            RegisterFailure(u);
            await repo.SaveAsync();return null;
        }
        if(!u.TotpEnabled && (u.FailedLoginCount!=0 || u.LockedUntil!=null)) {u.FailedLoginCount=0;u.LockedUntil=null;await repo.SaveAsync();}
        return u;
    }

    public Task<List<AppUser>> UsersAsync() => repo.ToListAsync(repo.Query<AppUser>().OrderBy(u => u.Email));

    /// <summary>Counts one failed attempt (wrong password or wrong second-factor code) toward the configured lock.</summary>
    private void RegisterFailure(AppUser u)
    {
        int maxFailed=SettingsService.GetInt("auth.maxFailed"),lockMin=SettingsService.GetInt("auth.lockMinutes"),windowMin=SettingsService.GetInt("auth.failWindowMinutes");
        if(u.LastFailedLoginAt==null || u.LastFailedLoginAt<clock.UtcNow.AddMinutes(-windowMin)) u.FailedLoginCount=0;
        u.FailedLoginCount++;u.LastFailedLoginAt=clock.UtcNow;
        if(u.FailedLoginCount>=maxFailed) {u.LockedUntil=clock.UtcNow.AddMinutes(lockMin);u.FailedLoginCount=0;repo.Add(new AuditLog {At=clock.UtcNow,User=u.Email,Action="AccountLocked",Entity="AppUser",EntityId=u.Id.ToString(),Details=$"{maxFailed} failed attempts; {lockMin}-minute lock"});}
    }

    /// <summary>Checks the authenticator code at sign-in. Wrong codes count toward the lock, a locked account is refused, and a code from a step already used is refused (no replay).</summary>
    public async Task<bool> VerifySecondFactorAsync(AppUser u, string? code)
    {
        if(u.LockedUntil>clock.UtcNow || u.TotpSecret==null) return false;
        var step=Totp.MatchStep(u.TotpSecret,code,clock.UtcNow);
        if(step==null || step<=u.LastTotpStep){RegisterFailure(u);await repo.SaveAsync();return false;}
        u.LastTotpStep=step.Value;u.FailedLoginCount=0;u.LockedUntil=null;await repo.SaveAsync();return true;
    }
}
