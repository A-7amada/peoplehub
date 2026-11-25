using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
using PeopleHub.Application.Modules.Organization;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class CalendarDefinitionAtomicTests
{
 class User(bool allowed=true):ICurrentUser{public string Email=>"calendaratomic@example.local";public int? EmployeeId=>null;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>allowed&&p==Perm.OrgManage;}
 class Db(DbContextOptions options):AppDbContext(options){public bool Fail;public override async Task<int> SaveChangesAsync(CancellationToken ct=default){var n=await base.SaveChangesAsync(ct);if(Fail)throw new InvalidOperationException("Fictional post-write failure");return n;}}
 static async Task<Db> Open(){var d=new Db(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.EnsureCreatedAsync();return d;}
 static Task Add(AppDbContext d,string kind,bool allowed=true,bool valid=true){var u=new User(allowed);var c=new AppClock(TimeProvider.System);var a=new AuditService(d,u,c);return kind=="Holiday"?new HolidayService(d,u,c,a).AddAsync(c.Today.AddDays(valid?10:0),"Fictional holiday",null,null,null,"Fictional future calendar"):new ShiftSchedule(d,u,c,a).AddShiftAsync("Fictional shift",null,new(8,0,0),new(16,0,0),valid?5:-1,60);}
 static async Task AssertEmpty(AppDbContext d){Assert.Empty(await d.Set<Holiday>().ToListAsync());Assert.Empty(await d.Set<Shift>().ToListAsync());Assert.Empty(await d.Set<AuditLog>().ToListAsync());Assert.False(d.ChangeTracker.HasChanges());}
 [Theory][InlineData("Holiday")][InlineData("Shift")]public async Task PostSaveFailureRollsBackDefinitionAndAuditThenRetriesOnce(string kind){await using var d=await Open();d.Fail=true;await Assert.ThrowsAsync<InvalidOperationException>(()=>Add(d,kind));d.Fail=false;await AssertEmpty(d);await Add(d,kind);Assert.Equal(kind=="Holiday"?1:0,await d.Set<Holiday>().CountAsync());Assert.Equal(kind=="Shift"?1:0,await d.Set<Shift>().CountAsync());Assert.Single(await d.Set<AuditLog>().ToListAsync());}
 [Theory][InlineData("Holiday")][InlineData("Shift")]public async Task OuterFailureRollsBackDefinitionAndAudit(string kind){await using var d=await Open();await Assert.ThrowsAsync<DomainException>(()=>d.AtomicAsync(async()=>{await Add(d,kind);throw new DomainException("Fictional outer failure");}));await AssertEmpty(d);}
 [Theory][InlineData("Holiday")][InlineData("Shift")]public async Task PermissionAndInvalidInputLeaveNoStagedOrPersistedChanges(string kind){await using var d=await Open();await Assert.ThrowsAsync<DomainException>(()=>Add(d,kind,false));await AssertEmpty(d);await Assert.ThrowsAsync<DomainException>(()=>Add(d,kind,true,false));await AssertEmpty(d);}
}
