using System.Security.Cryptography;
using System.Text;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Identity;
public class SessionService(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 public static string Hash(string token)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
 public async Task<string> StartAsync(int userId,string agent,string ip)
 {
  if(!await repo.AnyAsync(repo.Query<AppUser>().Where(x=>x.Id==userId&&x.IsActive)))throw new DomainException("err.forbidden");
  string token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));repo.Add(new LoginSession{UserId=userId,TokenHash=Hash(token),StartedAt=clock.UtcNow,ExpiresAt=clock.UtcNow.AddHours(8),Agent=agent[..Math.Min(agent.Length,300)],Ip=ip[..Math.Min(ip.Length,100)]});await repo.SaveAsync();return token;
 }
 public Task<bool> ValidAsync(int userId,string? token){if(string.IsNullOrEmpty(token))return Task.FromResult(false);var hash=Hash(token);return repo.AnyAsync(repo.Query<LoginSession>().Where(x=>x.UserId==userId&&x.TokenHash==hash&&x.RevokedAt==null&&x.ExpiresAt>clock.UtcNow));}
 public async Task<List<LoginSession>> MineAsync(){return await repo.ToListAsync(repo.Query<LoginSession>().Where(x=>x.UserId==me.UserId&&x.RevokedAt==null&&x.ExpiresAt>clock.UtcNow).OrderByDescending(x=>x.StartedAt));}
 public async Task RevokeAsync(int id){var s=await repo.FirstOrDefaultAsync(repo.Query<LoginSession>().Where(x=>x.Id==id&&x.UserId==me.UserId))??throw new DomainException("err.forbidden");if(s.RevokedAt==null){s.RevokedAt=clock.UtcNow;audit.Log("RevokeSession","LoginSession",id,"Revoked by owner");await repo.SaveAsync();}}
 public async Task RevokeCurrentAsync(string? token){if(string.IsNullOrEmpty(token))return;var hash=Hash(token);var s=await repo.FirstOrDefaultAsync(repo.Query<LoginSession>().Where(x=>x.UserId==me.UserId&&x.TokenHash==hash));if(s!=null)await RevokeAsync(s.Id);}
 public async Task<bool> ElevatedAsync(string? token){if(string.IsNullOrEmpty(token))return false;var hash=Hash(token);return await repo.AnyAsync(repo.Query<LoginSession>().Where(x=>x.UserId==me.UserId&&x.TokenHash==hash&&x.RevokedAt==null&&x.ExpiresAt>clock.UtcNow&&x.ElevatedUntil>clock.UtcNow));}
 public async Task<bool> ElevateAsync(string? token,string password,string? code)
 {bool success=false;await repo.AtomicAsync(async()=>
 {
  if(string.IsNullOrEmpty(token))return;
  var user=await new AuthService(repo,clock).LoginAsync(me.Email,password);if(user==null||user.Id!=me.UserId)return;
  if(user.TotpEnabled&&(user.TotpSecret==null||!PeopleHub.Domain.Security.Totp.Verify(user.TotpSecret,code??"",clock.UtcNow))){audit.Log("StepUpFailed","AppUser",user.Id,"Authenticator check failed");await repo.SaveAsync();return;}
  var hash=Hash(token);var session=await repo.FirstOrDefaultAsync(repo.Query<LoginSession>().Where(x=>x.UserId==me.UserId&&x.TokenHash==hash&&x.RevokedAt==null&&x.ExpiresAt>clock.UtcNow));if(session==null)return;
  session.ElevatedUntil=clock.UtcNow.AddMinutes(5);audit.Log("StepUp","LoginSession",session.Id,"Fresh password and enabled authenticator checked; five-minute session-bound elevation");await repo.SaveAsync();success=true;
 });return success;
 }
}
