using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class AttendanceSubmissionTests
{
 class User:ICurrentUser{public string Email=>"submission@example.local";public int? EmployeeId=>6;public int UserId=>7;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>false;}
 class Db(DbContextOptions options):AppDbContext(options)
 {
  public bool FailAfterSave;
  public override async Task<int> SaveChangesAsync(CancellationToken ct=default){var count=await base.SaveChangesAsync(ct);if(FailAfterSave)throw new InvalidOperationException("Fictional failure after database write");return count;}
 }
 static async Task<Db> Open(){var d=new Db(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.EnsureCreatedAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static AttendanceService S(Db d){var u=new User();var c=new AppClock(TimeProvider.System);var a=new AuditService(d,u,c);return new(d,new(d,u),u,a,new(d,u,c),c,new(d,u,a));}
 [Theory][InlineData(false)][InlineData(true)]public async Task FailureAfterWriteRollsBackRequestRevisionNotificationsAndCanRetry(bool correction)
 {
  await using var d=await Open();var c=new AppClock(TimeProvider.System);var day=c.Today.AddDays(-40);var beforeRequests=correction?await d.Set<AttendanceCorrection>().CountAsync():await d.Set<OvertimeRequest>().CountAsync();var beforeNotices=await d.Set<Notification>().CountAsync();var beforeRevisions=await d.Set<WorkflowRevision>().CountAsync();var before=(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp;
  async Task Submit(){if(correction)await S(d).RequestCorrectionAsync(day,day.AddHours(9),day.AddHours(17),"Fictional atomic correction");else await S(d).RequestOvertimeAsync(day,1,"Fictional atomic overtime");}
  d.FailAfterSave=true;await Assert.ThrowsAsync<InvalidOperationException>(Submit);d.FailAfterSave=false;
  Assert.Equal(beforeRequests,correction?await d.Set<AttendanceCorrection>().CountAsync():await d.Set<OvertimeRequest>().CountAsync());Assert.Equal(beforeNotices,await d.Set<Notification>().CountAsync());Assert.Equal(beforeRevisions,await d.Set<WorkflowRevision>().CountAsync());Assert.Equal(before,(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp);Assert.Empty(d.ChangeTracker.Entries().Where(e=>e.State==EntityState.Added));
  await Submit();Assert.Equal(beforeRequests+1,correction?await d.Set<AttendanceCorrection>().CountAsync():await d.Set<OvertimeRequest>().CountAsync());Assert.NotEqual(before,(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp);
 }
 [Fact]public async Task OvertimeRetryCannotCreateDuplicateOrChangeEmployeeRevision(){await using var d=await Open();var day=new AppClock(TimeProvider.System).Today.AddDays(-45);await S(d).RequestOvertimeAsync(day,1,"Fictional once");var count=await d.Set<OvertimeRequest>().CountAsync();var notices=await d.Set<Notification>().CountAsync();var before=(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp;await Assert.ThrowsAsync<DomainException>(()=>S(d).RequestOvertimeAsync(day,1,"Fictional duplicate"));Assert.Equal(count,await d.Set<OvertimeRequest>().CountAsync());Assert.Equal(notices,await d.Set<Notification>().CountAsync());Assert.Equal(before,(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp);}
 [Fact]public async Task FailedValidationClearsStagedCorrectionEmployeeAndAttachmentChanges(){await using var d=await Open();var before=(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp;var day=new AppClock(TimeProvider.System).Today.AddDays(-42);await Assert.ThrowsAsync<DomainException>(()=>S(d).RequestCorrectionAsync(day,null,null,"Fictional invalid correction"));Assert.False(d.ChangeTracker.HasChanges());Assert.Equal(before,(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp);}
 [Theory][InlineData(false)][InlineData(true)]public async Task NestedCallerFailureRollsBackSubmittedRequests(bool correction){await using var d=await Open();var day=new AppClock(TimeProvider.System).Today.AddDays(-46);var before=correction?await d.Set<AttendanceCorrection>().CountAsync():await d.Set<OvertimeRequest>().CountAsync();await Assert.ThrowsAsync<DomainException>(()=>d.AtomicAsync(async()=>{if(correction)await S(d).RequestCorrectionAsync(day,day.AddHours(9),null,"Fictional nested");else await S(d).RequestOvertimeAsync(day,1,"Fictional nested");throw new DomainException("Fictional outer failure");}));Assert.Equal(before,correction?await d.Set<AttendanceCorrection>().CountAsync():await d.Set<OvertimeRequest>().CountAsync());}
}
