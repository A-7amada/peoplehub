using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Application.Modules.Relations;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class EmployeeRelationsTests
{
 class User(int user=1,int emp=1,bool grant=true):ICurrentUser{public string Email=>$"case{user}@example.local";public int? EmployeeId=>emp;public int UserId=>user;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>grant?p==Perm.RelationsManage:p is Perm.TalentManage or Perm.ScopeCompany or Perm.EmpManage or Perm.Audit;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static EmployeeRelations S(AppDbContext d,User? u=null){u??=new();var c=new AppClock(TimeProvider.System);return new(d,u,c,new AuditService(d,u,c));}
 static async Task<int> Case(AppDbContext d){await S(d).CreateAsync(6,"Grievance","Fictional private case","Private fictional source statement CASE-48");return await d.Set<EmployeeCase>().Select(x=>x.Id).SingleAsync();}
 [Fact]public async Task GeneralHrScopeAndAuditDoNotGrantAccessOrExposePrivateContentsInAudit(){await using var d=await Db();int id=await Case(d);await Assert.ThrowsAsync<DomainException>(()=>S(d,new(2,2,false)).GetAsync());await Assert.ThrowsAsync<DomainException>(()=>S(d,new(2,2,false)).RecordAsync(id,"Investigation","Attempt"));Assert.DoesNotContain(await d.Set<AuditLog>().ToListAsync(),x=>x.Details.Contains("CASE-48"));}
 [Fact]public async Task DisciplineNeedsIndependentActorAndReasonWithoutAnyPayrollOrStatusEffect(){await using var d=await Db();int id=await Case(d);var before=(await d.Set<Employee>().FindAsync(6))!.Status;int lines=await d.Set<PayrollLine>().CountAsync();await S(d,new(2,2)).RecordAsync(id,"ProposedDiscipline","Fictional action proposal, not executed");await Assert.ThrowsAsync<DomainException>(()=>S(d).RecordAsync(id,"ApprovedDiscipline","Creator cannot approve"));await Assert.ThrowsAsync<DomainException>(()=>S(d,new(2,2)).RecordAsync(id,"ApprovedDiscipline","Proposer cannot approve"));await Assert.ThrowsAsync<DomainException>(()=>S(d,new(6,6)).RecordAsync(id,"ApprovedDiscipline","Subject cannot approve"));await Assert.ThrowsAsync<DomainException>(()=>S(d,new(3,3)).RecordAsync(id,"ApprovedDiscipline",""));await S(d,new(3,3)).RecordAsync(id,"ApprovedDiscipline","Reviewed fictional evidence, internal record only");Assert.Equal(before,(await d.Set<Employee>().FindAsync(6))!.Status);Assert.Equal(lines,await d.Set<PayrollLine>().CountAsync());}
 [Fact]public async Task PendingProposalMustBeDecidedBeforeCloseAndClosedRecordRejectsFurtherEvents(){await using var d=await Db();int id=await Case(d);await S(d).RecordAsync(id,"Investigation","Recorded source reference");await S(d,new(2,2)).RecordAsync(id,"ProposedDiscipline","Proposal");await Assert.ThrowsAsync<DomainException>(()=>S(d).RecordAsync(id,"Closed","Pending"));await S(d,new(3,3)).RecordAsync(id,"RejectedDiscipline","Not supported");await S(d).RecordAsync(id,"Closed","Case concluded, no action");await Assert.ThrowsAsync<DomainException>(()=>S(d).RecordAsync(id,"Investigation","Reopen silently"));}
 [Fact]public async Task CaseSourcesAndEventsCannotBeRewritten(){await using var d=await Db();int id=await Case(d);await S(d).RecordAsync(id,"Investigation","Original evidence");(await d.Set<EmployeeCaseEvent>().SingleAsync()).Note="Overwrite";await Assert.ThrowsAsync<InvalidOperationException>(()=>d.SaveAsync());d.ChangeTracker.Clear();(await d.Set<EmployeeCase>().FindAsync(id))!.Statement="Overwrite source";await Assert.ThrowsAsync<InvalidOperationException>(()=>d.SaveAsync());}
 [Fact]public async Task InvalidKindsAndEmptyStatementsCannotCreateCases(){await using var d=await Db();await Assert.ThrowsAsync<DomainException>(()=>S(d).CreateAsync(6,"Terminate","No","No"));await Assert.ThrowsAsync<DomainException>(()=>S(d).CreateAsync(6,"Warning","Title",""));int id=await Case(d);await Assert.ThrowsAsync<DomainException>(()=>S(d,new(3,3)).RecordAsync(id,"ApprovedDiscipline","No proposal"));Assert.Empty(await d.Set<EmployeeCaseEvent>().ToListAsync());}

 [Fact]public async Task ExplicitInvestigatorReadsOnlyAssignedCaseAndCannotChangeDisciplineOrAccess()
 {
  await using var d=await Db();int id=await Case(d);await S(d).CreateAsync(5,"Warning","Other confidential case","Other source");
  await S(d).SetAccessAsync(id,3,true,"Fictional investigator appointment");var inv=S(d,new(3,15,false));
  Assert.True(await inv.CanEnterAsync());var v=await inv.GetAsync();Assert.Single(v.Cases);Assert.Equal(id,v.Cases[0].Id);Assert.Empty(v.Access!);
  await inv.RecordAsync(id,"Investigation","Assigned evidence");Assert.Single((await inv.GetAsync()).Events);
  foreach(var kind in new[]{"ProposedDiscipline","ApprovedDiscipline","RejectedDiscipline","Closed"})await Assert.ThrowsAsync<DomainException>(()=>inv.RecordAsync(id,kind,"Forbidden action"));
  await Assert.ThrowsAsync<DomainException>(()=>inv.RecordAsync(id+1,"Investigation","Other case"));
  await Assert.ThrowsAsync<DomainException>(()=>inv.CreateAsync(5,"Warning","No","No"));
  await Assert.ThrowsAsync<DomainException>(()=>inv.SetAccessAsync(id,4,true,"No"));
  await Assert.ThrowsAsync<DomainException>(()=>inv.CandidatesAsync());
 }
 [Fact]public async Task RevocationAndRegrantAreAppendOnlyWithImmediateFutureAccessChange()
 {
  await using var d=await Db();int id=await Case(d);var inv=S(d,new(3,15,false));
  await S(d).SetAccessAsync(id,3,true,"First appointment");await S(d).SetAccessAsync(id,3,false,"Review finished");Assert.False(await inv.CanEnterAsync());
  await Assert.ThrowsAsync<DomainException>(()=>inv.GetAsync());await Assert.ThrowsAsync<DomainException>(()=>inv.RecordAsync(id,"Investigation","After revocation"));
  await S(d).SetAccessAsync(id,3,true,"Second appointment");Assert.Single((await inv.GetAsync()).Cases);Assert.Equal(3,await d.Set<EmployeeCaseAccess>().CountAsync());
  (await d.Set<EmployeeCaseAccess>().FirstAsync()).Reason="Rewrite";await Assert.ThrowsAsync<InvalidOperationException>(()=>d.SaveAsync());
  d.ChangeTracker.Clear();d.Remove(await d.Set<EmployeeCaseAccess>().FirstAsync());await Assert.ThrowsAsync<InvalidOperationException>(()=>d.SaveAsync());
 }
 [Fact]public async Task InactiveOrDepartedInvestigatorLosesAccessButCanStillBeRevoked()
 {
  await using var d=await Db();int id=await Case(d);await S(d).SetAccessAsync(id,3,true,"Appointment");var u=(await d.Set<AppUser>().FindAsync(3))!;u.IsActive=false;await d.SaveAsync();var inv=S(d,new(3,15,false));Assert.False(await inv.CanEnterAsync());
  await Assert.ThrowsAsync<DomainException>(()=>inv.RecordAsync(id,"Investigation","Inactive"));await S(d).SetAccessAsync(id,3,false,"Inactive account removed");
  u=(await d.Set<AppUser>().FindAsync(3))!;u.IsActive=true;await d.SaveAsync();await S(d).SetAccessAsync(id,3,true,"Active again");var e=(await d.Set<Employee>().FindAsync(u.EmployeeId))!;e.Status=EmployeeStatus.Terminated;await d.SaveAsync();Assert.False(await inv.CanEnterAsync());await S(d).SetAccessAsync(id,3,false,"Employment ended");
 }
 [Fact]public async Task SubjectAccountAliasesCannotReceiveInvestigatorAccess()
 {
  await using var d=await Db();int id=await Case(d);await Assert.ThrowsAsync<DomainException>(()=>S(d).SetAccessAsync(id,7,true,"Subject excluded"));
  var u=(await d.Set<AppUser>().FindAsync(3))!;u.EmployeeId=6;await d.SaveAsync();await Assert.ThrowsAsync<DomainException>(()=>S(d).SetAccessAsync(id,3,true,"Linked subject excluded"));Assert.Empty(await d.Set<EmployeeCaseAccess>().ToListAsync());
 }
 [Fact]public async Task InvestigatorRelinkedToSubjectLosesPriorGrant()
 {
  await using var d=await Db();int id=await Case(d);await S(d).SetAccessAsync(id,3,true,"Appointment");(await d.Set<AppUser>().FindAsync(3))!.EmployeeId=6;await d.SaveAsync();Assert.False(await S(d,new(3,15,false)).CanEnterAsync());
 }
 [Fact]public async Task AccessRequiresReasonExistingEntitiesAndActualStateChange()
 {
  await using var d=await Db();int id=await Case(d);
  await Assert.ThrowsAsync<DomainException>(()=>S(d).SetAccessAsync(id,3,true,""));await Assert.ThrowsAsync<DomainException>(()=>S(d).SetAccessAsync(id,3,false,"Never assigned"));
  await Assert.ThrowsAsync<DomainException>(()=>S(d).SetAccessAsync(999,3,true,"No case"));await Assert.ThrowsAsync<DomainException>(()=>S(d).SetAccessAsync(id,999,true,"No account"));
  await S(d).SetAccessAsync(id,3,true,"Appointment");await Assert.ThrowsAsync<DomainException>(()=>S(d).SetAccessAsync(id,3,true,"Duplicate"));Assert.Single(await d.Set<EmployeeCaseAccess>().ToListAsync());Assert.DoesNotContain(await d.Set<AuditLog>().ToListAsync(),x=>x.Details.Contains("Appointment"));
 }
 [Fact]public async Task InactiveAndDepartedAccountsCannotBeNewInvestigators()
 {
  await using var d=await Db();int id=await Case(d);var u=(await d.Set<AppUser>().FindAsync(3))!;u.IsActive=false;await d.SaveAsync();await Assert.ThrowsAsync<DomainException>(()=>S(d).SetAccessAsync(id,3,true,"Inactive"));
  u=(await d.Set<AppUser>().FindAsync(3))!;u.IsActive=true;var e=(await d.Set<Employee>().FindAsync(u.EmployeeId))!;e.Status=EmployeeStatus.Terminated;await d.SaveAsync();await Assert.ThrowsAsync<DomainException>(()=>S(d).SetAccessAsync(id,3,true,"Departed"));Assert.DoesNotContain(await S(d).CandidatesAsync(),x=>x.UserId==3);
 }
 [Fact]public async Task ClosedCaseStillAllowsReadButNotNewInvestigationEvidence()
 {
  await using var d=await Db();int id=await Case(d);await S(d).SetAccessAsync(id,3,true,"Archive review");await S(d).RecordAsync(id,"Closed","Concluded");var inv=S(d,new(3,15,false));Assert.Single((await inv.GetAsync()).Cases);await Assert.ThrowsAsync<DomainException>(()=>inv.RecordAsync(id,"Investigation","Too late"));
 }

 static byte[] Pdf=>"%PDF-1.4 Fictional case evidence only"u8.ToArray();
 [Fact]public async Task CaseFilesAreRestrictedToManagerOrAssignedInvestigatorNeverEmployeeOwnership()
 {
  await using var d=await Db();int id=await Case(d);await S(d).AddFileAsync(id,"Private source CASEFILE-72","case.pdf",Pdf);
  var f=await d.Set<EmployeeCaseFile>().SingleAsync();Assert.Equal(Pdf,(await S(d).DownloadAsync(f.Token)).Data);Assert.Empty(await d.Set<Attachment>().ToListAsync());
  Assert.Single((await S(d).GetAsync()).Files!);Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Pdf)),f.Sha256);
  foreach(var u in new[]{new User(7,6,false),new User(6,4,false),new User(3,15,false)})await Assert.ThrowsAsync<DomainException>(()=>S(d,u).DownloadAsync(f.Token));
  var c=new AppClock(TimeProvider.System);var owner=new User(7,6,false);await Assert.ThrowsAsync<DomainException>(()=>new AttachmentService(d,new(d,owner),owner,c).GetAsync(f.Token));
  await S(d).SetAccessAsync(id,3,true,"Appointed");var inv=S(d,new(3,15,false));Assert.Equal(Pdf,(await inv.DownloadAsync(f.Token)).Data);Assert.Single((await inv.GetAsync()).Files!);
  await inv.AddFileAsync(id,"Investigator source","second.pdf",Pdf);await S(d).SetAccessAsync(id,3,false,"Finished");await Assert.ThrowsAsync<DomainException>(()=>inv.DownloadAsync(f.Token));
  Assert.DoesNotContain(await d.Set<AuditLog>().ToListAsync(),x=>x.Details.Contains("CASEFILE-72")||x.Details.Contains("case.pdf"));
 }
 [Fact]public async Task CaseFilesHaveImmutableBytesMetadataAndSource()
 {
  await using var d=await Db();int id=await Case(d);await S(d).AddFileAsync(id,"Original source","case.pdf",Pdf);var f=await d.Set<EmployeeCaseFile>().SingleAsync();f.Data="%PDF-changed"u8.ToArray();await Assert.ThrowsAsync<InvalidOperationException>(()=>d.SaveAsync());
  d.ChangeTracker.Clear();f=await d.Set<EmployeeCaseFile>().SingleAsync();f.SourceReference="Rewrite";await Assert.ThrowsAsync<InvalidOperationException>(()=>d.SaveAsync());
  d.ChangeTracker.Clear();d.Remove(await d.Set<EmployeeCaseFile>().SingleAsync());await Assert.ThrowsAsync<InvalidOperationException>(()=>d.SaveAsync());
 }
 [Fact]public async Task InvalidOrClosedCaseFilesLeaveNoRowsOrRevisionChanges()
 {
  await using var d=await Db();int id=await Case(d);var before=(await d.Set<EmployeeCase>().FindAsync(id))!.ChangeStamp;
  foreach(var input in new[]{("case.exe",Pdf),("case.pdf",new byte[]{1,2,3}),("case.pdf",new byte[AttachmentService.MaxBytes+1]),("case.pdf",Array.Empty<byte>())})await Assert.ThrowsAsync<DomainException>(()=>S(d).AddFileAsync(id,"Source",input.Item1,input.Item2));
  await Assert.ThrowsAsync<DomainException>(()=>S(d).AddFileAsync(id,"","case.pdf",Pdf));await Assert.ThrowsAsync<DomainException>(()=>S(d,new(3,15,false)).AddFileAsync(id,"Not assigned","case.pdf",Pdf));
  Assert.Empty(await d.Set<EmployeeCaseFile>().ToListAsync());Assert.Equal(before,(await d.Set<EmployeeCase>().FindAsync(id))!.ChangeStamp);
  await S(d).RecordAsync(id,"Closed","Concluded");await Assert.ThrowsAsync<DomainException>(()=>S(d).AddFileAsync(id,"Too late","case.pdf",Pdf));
 }
 [Fact]public async Task ClosedCaseFilesRemainReadableButInactiveAssignedAccountLosesDownload()
 {
  await using var d=await Db();int id=await Case(d);await S(d).SetAccessAsync(id,3,true,"Appointment");await S(d).AddFileAsync(id,"Source","case.pdf",Pdf);var token=(await d.Set<EmployeeCaseFile>().SingleAsync()).Token;await S(d).RecordAsync(id,"Closed","Concluded");var inv=S(d,new(3,15,false));Assert.Equal(Pdf,(await inv.DownloadAsync(token)).Data);
  (await d.Set<AppUser>().FindAsync(3))!.IsActive=false;await d.SaveAsync();await Assert.ThrowsAsync<DomainException>(()=>inv.DownloadAsync(token));
 }
 [Fact]public async Task OuterFailureRollsBackCaseFileAndRevisionAndAudit()
 {
  await using var d=await Db();int id=await Case(d);var before=(await d.Set<EmployeeCase>().FindAsync(id))!.ChangeStamp;var audit=await d.Set<AuditLog>().CountAsync();
  await Assert.ThrowsAsync<DomainException>(()=>d.AtomicAsync(async()=>{await S(d).AddFileAsync(id,"Source","case.pdf",Pdf);throw new DomainException("Fictional outer failure");}));
  Assert.Empty(await d.Set<EmployeeCaseFile>().ToListAsync());Assert.Equal(before,(await d.Set<EmployeeCase>().FindAsync(id))!.ChangeStamp);Assert.Equal(audit,await d.Set<AuditLog>().CountAsync());await S(d).AddFileAsync(id,"Retry","case.pdf",Pdf);Assert.Single(await d.Set<EmployeeCaseFile>().ToListAsync());
 }
}
