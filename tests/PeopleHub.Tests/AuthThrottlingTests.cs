using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Identity;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Domain.Entities;

public class AuthThrottlingTests
{
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 sealed class Tp(DateTimeOffset now):TimeProvider{public DateTimeOffset N=now;public override DateTimeOffset GetUtcNow()=>N;}
 static async Task Fail(AuthService s,int n){for(int i=0;i<n;i++)Assert.Null(await s.LoginAsync("hr@peoplehub.local","wrong"));}
 [Fact]public async Task LockedAccountRejectsTheCorrectPasswordUntilTheLockEnds(){await using var d=await Db();var tp=new Tp(DateTimeOffset.UtcNow);var s=new AuthService(d,new AppClock(tp));await Fail(s,5);Assert.Null(await s.LoginAsync("hr@peoplehub.local","Admin123!"));tp.N=tp.N.AddMinutes(14);Assert.Null(await s.LoginAsync("hr@peoplehub.local","Admin123!"));tp.N=tp.N.AddMinutes(2);Assert.NotNull(await s.LoginAsync("hr@peoplehub.local","Admin123!"));}
 [Fact]public async Task FourFailuresDoNotLock(){await using var d=await Db();var tp=new Tp(DateTimeOffset.UtcNow);var s=new AuthService(d,new AppClock(tp));await Fail(s,4);Assert.NotNull(await s.LoginAsync("hr@peoplehub.local","Admin123!"));}
 [Fact]public async Task FailuresOlderThanTheWindowDoNotAccumulate(){await using var d=await Db();var tp=new Tp(DateTimeOffset.UtcNow);var s=new AuthService(d,new AppClock(tp));await Fail(s,4);tp.N=tp.N.AddMinutes(16);await Fail(s,4);Assert.NotNull(await s.LoginAsync("hr@peoplehub.local","Admin123!"));}
 [Fact]public async Task UnknownEmailIsRejectedWithoutCreatingState(){await using var d=await Db();var s=new AuthService(d,new AppClock(TimeProvider.System));var before=await d.Set<AuditLog>().CountAsync();for(int i=0;i<10;i++)Assert.Null(await s.LoginAsync("nobody@example.local","x"));Assert.Equal(before,await d.Set<AuditLog>().CountAsync());}
 [Fact]public async Task LockoutAuditDoesNotContainThePassword(){await using var d=await Db();var s=new AuthService(d,new AppClock(TimeProvider.System));await Fail(s,5);var a=await d.Set<AuditLog>().SingleAsync(x=>x.Action=="AccountLocked");Assert.DoesNotContain("wrong",a.Details);}
 [Fact]public void AuthEndpointsCarryTheRateLimitPolicy(){var t=typeof(PeopleHub.Web.Controllers.AccountController);var n=t.GetMethods().Count(m=>m.GetCustomAttributes(typeof(Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute),false).Length>0);Assert.True(n>=4);}
}
