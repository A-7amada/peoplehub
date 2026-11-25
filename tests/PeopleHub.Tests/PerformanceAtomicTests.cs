using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class PerformanceAtomicTests
{
 class User:ICurrentUser{public string Email=>"atomic@example.local";public int? EmployeeId=>1;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>true;public bool Can(string p)=>true;}
 class Db(DbContextOptions options):AppDbContext(options)
 {
  public int? FailAfter;
  public override Task<int> SaveChangesAsync(CancellationToken ct=default){if(FailAfter is int n){if(n==0)throw new InvalidOperationException("Fictional save failure");FailAfter=n-1;}return base.SaveChangesAsync(ct);}
 }
 static async Task<Db> Open(){var d=new Db(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.EnsureCreatedAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static PerfService Service(Db d){var u=new User();var c=new AppClock(TimeProvider.System);return new(d,u,new AccessScope(d,u),c,new AuditService(d,u,c),new NotificationService(d,u,c));}
 [Theory][InlineData(1)][InlineData(2)]public async Task CreationFailureDoesNotLeaveOrphanCycleOrPartialAppraisals(int saves){await using var d=await Open();var before=await d.Set<PerfAppraisal>().CountAsync();d.FailAfter=saves;await Assert.ThrowsAsync<InvalidOperationException>(()=>Service(d).CreateCycleAsync("Fictional failed cycle",new(2026,1,1),new(2026,12,31)));Assert.False(await d.Set<PerfCycle>().AnyAsync(x=>x.Name=="Fictional failed cycle"));Assert.Equal(before,await d.Set<PerfAppraisal>().CountAsync());Assert.False(await d.Set<AuditLog>().AnyAsync(x=>x.EntityId=="Fictional failed cycle"));}
 [Fact]public async Task SuccessfulCreationRetainsAllWorkingEmployees(){await using var d=await Open();await Service(d).CreateCycleAsync("Fictional complete cycle",new(2026,1,1),new(2027,12,31));var c=await d.Set<PerfCycle>().SingleAsync(x=>x.Name=="Fictional complete cycle");var expected=await d.Set<Employee>().CountAsync(x=>(x.Status==EmployeeStatus.Active||x.Status==EmployeeStatus.OnLeave)&&x.HireDate<=new DateTime(2027,12,31));Assert.Equal(expected,await d.Set<PerfAppraisal>().CountAsync(x=>x.CycleId==c.Id));}
 [Fact]public async Task FailedManualCloseRetainsCalibrationAndNoReviewNotice(){await using var d=await Open();var svc=Service(d);await svc.CreateCycleAsync("Fictional manual close",new(2026,1,1),new(2027,12,31));var c=await d.Set<PerfCycle>().SingleAsync(x=>x.Name=="Fictional manual close");await svc.AdvanceCycleAsync(c.Id);var notices=await d.Set<Notification>().CountAsync();d.FailAfter=0;await Assert.ThrowsAsync<InvalidOperationException>(()=>svc.AdvanceCycleAsync(c.Id));Assert.Equal(CycleStatus.Calibration,(await d.Set<PerfCycle>().FindAsync(c.Id))!.Status);Assert.Equal(notices,await d.Set<Notification>().CountAsync());}
 [Fact]public async Task FailedMaintenanceRetainsPhaseAndNoPartialReviewNotices(){await using var d=await Open();var svc=Service(d);await svc.CreateCycleAsync("Fictional auto close",new(2020,1,1),new(2021,12,31));var c=await d.Set<PerfCycle>().SingleAsync(x=>x.Name=="Fictional auto close");c.AutoPhases=true;c.CalibrationOn=new(2022,1,1);c.CloseOn=new(2022,1,2);await d.SaveAsync();var notices=await d.Set<Notification>().CountAsync();d.FailAfter=0;await Assert.ThrowsAsync<InvalidOperationException>(()=>svc.MaintainAsync());Assert.Equal(CycleStatus.Open,(await d.Set<PerfCycle>().FindAsync(c.Id))!.Status);Assert.Equal(notices,await d.Set<Notification>().CountAsync());}
}
