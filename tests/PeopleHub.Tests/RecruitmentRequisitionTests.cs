using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Talent;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class RecruitmentRequisitionTests
{
 class User(int id=1,bool hr=true):ICurrentUser{public string Email=>$"planner{id}@example.local";public int? EmployeeId=>id;public int UserId=>id;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>hr;public bool Can(string p)=>hr;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static TalentService S(AppDbContext d,int id=1,bool hr=true){var u=new User(id,hr);var c=new AppClock(TimeProvider.System);return new(d,u,new AccessScope(d,u),c,new AuditService(d,u,c),new NotificationService(d,u,c));}

 [Fact]public async Task RequisitionNeedsIndependentApprovalBeforeCandidates(){await using var d=await Db();await S(d).AddJobAsync("Fictional requisition",null,1,2,"Need two roles for fictional expansion");var j=await d.Set<JobOpening>().SingleAsync(x=>x.Title=="Fictional requisition");Assert.Equal("Pending",j.ApprovalStatus);Assert.False(j.IsOpen);await Assert.ThrowsAsync<DomainException>(()=>S(d).AddCandidateAsync(j.Id,"Demo applicant",null,null));await Assert.ThrowsAsync<DomainException>(()=>S(d).DecideJobAsync(j.Id,true,"Self"));await S(d,2).DecideJobAsync(j.Id,true,"Reviewed fictional role need");await S(d).AddCandidateAsync(j.Id,"Demo applicant",null,null);Assert.True(await d.Set<Candidate>().AnyAsync(x=>x.JobOpeningId==j.Id));await Assert.ThrowsAsync<DomainException>(()=>S(d,2).DecideJobAsync(j.Id,false,"Rewrite"));}
 [Fact]public async Task RejectedRequisitionCannotBeReopenedOrReceiveCandidates(){await using var d=await Db();await S(d).AddJobAsync("Rejected requisition",null,null,1,"Need");var j=await d.Set<JobOpening>().SingleAsync(x=>x.Title=="Rejected requisition");await Assert.ThrowsAsync<DomainException>(()=>S(d,2).DecideJobAsync(j.Id,false,""));await S(d,2).DecideJobAsync(j.Id,false,"Not required");await Assert.ThrowsAsync<DomainException>(()=>S(d).CloseJobAsync(j.Id));await Assert.ThrowsAsync<DomainException>(()=>S(d).AddCandidateAsync(j.Id,"Demo",null,null));}
 [Fact]public async Task RequisitionInputsAndPermissionValidated(){await using var d=await Db();await Assert.ThrowsAsync<DomainException>(()=>S(d,6,false).AddJobAsync("Denied",null,null,1,"Need"));await Assert.ThrowsAsync<DomainException>(()=>S(d).AddJobAsync("No reason",null,null,1));await Assert.ThrowsAsync<DomainException>(()=>S(d).AddJobAsync("No positions",null,null,0,"Need"));await Assert.ThrowsAsync<DomainException>(()=>S(d).AddJobAsync("Bad department",null,99999,1,"Need"));Assert.All(await d.Set<JobOpening>().ToListAsync(),x=>{Assert.Equal("Legacy",x.ApprovalStatus);Assert.Null(x.DecidedAt);});}
}
