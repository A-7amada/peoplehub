using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Identity;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class SessionTests
{
 class User(int id=1):ICurrentUser{public string Email=>"session@example.local";public int? EmployeeId=>8;public int UserId=>id;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>true;public bool Can(string p)=>true;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static SessionService S(AppDbContext d,int user=1){var u=new User(user);var c=new AppClock(TimeProvider.System);return new(d,u,c,new AuditService(d,u,c));}
 [Fact]public async Task UniqueTokensAreStoredOnlyAsHashesAndScopedToAccount(){await using var d=await Db();var one=await S(d).StartAsync(1,"Fictional browser","127.0.0.1");var two=await S(d).StartAsync(1,"Second browser","127.0.0.2");Assert.NotEqual(one,two);Assert.True(await S(d).ValidAsync(1,one));Assert.False(await S(d).ValidAsync(2,one));Assert.False(await S(d).ValidAsync(1,null));Assert.False(await S(d).ValidAsync(1,"wrong"));Assert.All(await d.Set<LoginSession>().ToListAsync(),x=>Assert.DoesNotContain(x.TokenHash,new[]{one,two}));Assert.Equal(2,(await S(d).MineAsync()).Count);Assert.Empty(await S(d,2).MineAsync());}
 [Fact]public async Task OwnerCanRevokeOneSessionButNotSomeoneElses(){await using var d=await Db();var token=await S(d).StartAsync(1,"Browser","ip");var other=await S(d).StartAsync(2,"Other browser","ip2");var id=(await d.Set<LoginSession>().SingleAsync(x=>x.UserId==1)).Id;await Assert.ThrowsAsync<DomainException>(()=>S(d,2).RevokeAsync(id));await S(d).RevokeAsync(id);Assert.False(await S(d).ValidAsync(1,token));Assert.True(await S(d,2).ValidAsync(2,other));Assert.Single(await d.Set<AuditLog>().Where(x=>x.Action=="RevokeSession").ToListAsync());}
 [Fact]public async Task ExpiredSessionFailsAndLogoutRevokesOnlyCurrent(){await using var d=await Db();var token=await S(d).StartAsync(1,"Browser","ip");var two=await S(d).StartAsync(1,"Second","ip");await S(d).RevokeCurrentAsync(token);Assert.False(await S(d).ValidAsync(1,token));Assert.True(await S(d).ValidAsync(1,two));var x=await d.Set<LoginSession>().SingleAsync(x=>x.RevokedAt==null);x.ExpiresAt=DateTime.UtcNow.AddMinutes(-1);await d.SaveAsync();Assert.False(await S(d).ValidAsync(1,two));Assert.Empty(await S(d).MineAsync());}
 [Fact]public async Task PasswordChangeRevokesSessionRecords(){await using var d=await Db();var token=await S(d).StartAsync(1,"Browser","ip");var u=new User();var c=new AppClock(TimeProvider.System);var a=new AuditService(d,u,c);var svc=new AccountService(d,u,c,new EmailService(d,c),a);await svc.ChangePasswordAsync("Admin123!","Changed123!");Assert.False(await S(d).ValidAsync(1,token));}
}
