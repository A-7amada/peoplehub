using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class FlexibleShiftServiceTests
{
 class User(bool grant=true):ICurrentUser{public string Email=>"attendance@example.local";public int? EmployeeId=>1;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>grant;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static ShiftSchedule S(AppDbContext d,bool grant=true){var u=new User(grant);var c=new AppClock(TimeProvider.System);return new(d,u,c,new AuditService(d,u,c));}
 [Fact]public async Task ExplicitValidShiftPersistsWithoutChangingExistingRecords(){await using var d=await Db();var before=await d.Set<AttendanceRecord>().Select(x=>x.LateMinutes).ToListAsync();await S(d).AddShiftAsync("Fictional flexible","مرنة",new(8,0,0),new(18,0,0),0,60,true,new(10,0,0),480);var s=await d.Set<Shift>().SingleAsync(x=>x.Name=="Fictional flexible");Assert.True(s.Flexible);Assert.Equal(480,s.RequiredMinutes);Assert.Equal(before,await d.Set<AttendanceRecord>().Select(x=>x.LateMinutes).ToListAsync());}
 [Fact]public async Task PermissionAndInconsistentWindowAreRejected(){await using var d=await Db();await Assert.ThrowsAsync<DomainException>(()=>S(d,false).AddShiftAsync("N","",new(8,0,0),new(18,0,0),0,60,true,new(10,0,0),480));await Assert.ThrowsAsync<DomainException>(()=>S(d).AddShiftAsync("N","",new(8,0,0),new(17,0,0),0,60,true,new(10,0,0),480));await Assert.ThrowsAsync<DomainException>(()=>S(d).AddShiftAsync("N","",new(20,0,0),new(8,0,0),0,60,true,new(22,0,0),600));Assert.DoesNotContain(await d.Set<Shift>().ToListAsync(),x=>x.Flexible);}
}
