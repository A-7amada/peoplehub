using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class LetterTests
{
 class U(int id,int? emp,bool hr,bool salary=true):ICurrentUser{public string Email=>$"u{id}@example.local";public int? EmployeeId=>emp;public int UserId=>id;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>p==Perm.EmpManage?hr:p==Perm.EmpSalary?salary:false;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static LetterService S(AppDbContext d,U u){var c=new AppClock(TimeProvider.System);return new(d,u,c,new AuditService(d,u,c));}
 [Fact]public async Task RequestNeedsAddresseeAndLinkedEmployee(){await using var d=await Db();var e=await d.Set<Employee>().FirstAsync();await Assert.ThrowsAsync<DomainException>(()=>S(d,new(1,e.Id,false)).RequestAsync(" ",false));await Assert.ThrowsAsync<DomainException>(()=>S(d,new(1,null,true)).RequestAsync("Bank",false));}
 [Fact]public async Task OpenRequestsAreLimited(){await using var d=await Db();var e=await d.Set<Employee>().FirstAsync();var s=S(d,new(1,e.Id,false));for(var i=0;i<LetterService.MaxOpenRequests;i++)await s.RequestAsync("Bank "+i,false);await Assert.ThrowsAsync<DomainException>(()=>s.RequestAsync("One more",false));}
 [Fact]public async Task RequesterCannotIssueOwnLetterAndNonHrCannotIssue(){await using var d=await Db();var e=await d.Set<Employee>().FirstAsync();await S(d,new(1,e.Id,true)).RequestAsync("Bank",false);var l=await d.Set<EmployeeLetter>().AsNoTracking().SingleAsync();await Assert.ThrowsAsync<DomainException>(()=>S(d,new(1,e.Id,true)).IssueAsync(l.Id));await Assert.ThrowsAsync<DomainException>(()=>S(d,new(2,null,false)).IssueAsync(l.Id));}
 [Fact]public async Task IssuedLetterIsAFrozenFactualSnapshotWithoutSalaryByDefault()
 {
  await using var d=await Db();var e=await d.Set<Employee>().Include(x=>x.JobTitle).Include(x=>x.Department).FirstAsync();
  await S(d,new(1,e.Id,false)).RequestAsync("The Bank",false);var l=await d.Set<EmployeeLetter>().AsNoTracking().SingleAsync();
  await S(d,new(2,null,true)).IssueAsync(l.Id);d.ChangeTracker.Clear();l=await d.Set<EmployeeLetter>().AsNoTracking().SingleAsync();
  Assert.Equal("Issued",l.Status);Assert.Equal($"LTR-{l.Id:D5}",l.Reference);Assert.Contains(e.FullName,l.BodyEn);Assert.Contains("The Bank",l.BodyEn);Assert.Contains(e.HireDate.ToString("yyyy-MM-dd"),l.BodyEn);Assert.DoesNotContain("salary",l.BodyEn);Assert.DoesNotContain("EGP",l.BodyEn);Assert.Contains(l.Reference,l.BodyAr);
  var before=l.BodyEn;d.ChangeTracker.Clear();var e2=await d.Set<Employee>().FirstAsync(x=>x.Id==e.Id);e2.FullName="Changed Name";await d.SaveChangesAsync();d.ChangeTracker.Clear();l=await d.Set<EmployeeLetter>().AsNoTracking().SingleAsync();Assert.Equal(before,l.BodyEn);
  await Assert.ThrowsAsync<DomainException>(()=>S(d,new(2,null,true)).IssueAsync(l.Id));
 }
 [Fact]public async Task SalaryIsIncludedOnlyWhenAskedAndIssuerMaySeeSalaries()
 {
  await using var d=await Db();var e=await d.Set<Employee>().FirstAsync();
  await S(d,new(1,e.Id,false)).RequestAsync("Landlord",true);var l=await d.Set<EmployeeLetter>().AsNoTracking().SingleAsync();
  await Assert.ThrowsAsync<DomainException>(()=>S(d,new(2,null,true,salary:false)).IssueAsync(l.Id));
  await S(d,new(3,null,true)).IssueAsync(l.Id);d.ChangeTracker.Clear();l=await d.Set<EmployeeLetter>().AsNoTracking().SingleAsync();Assert.Contains("Monthly basic salary",l.BodyEn);Assert.Contains("EGP",l.BodyEn);
 }
 [Fact]public async Task DeclineNeedsReasonAndKeepsLetterUnopenable()
 {
  await using var d=await Db();var e=await d.Set<Employee>().FirstAsync();await S(d,new(1,e.Id,false)).RequestAsync("Bank",false);var l=await d.Set<EmployeeLetter>().AsNoTracking().SingleAsync();
  await Assert.ThrowsAsync<DomainException>(()=>S(d,new(2,null,true)).DeclineAsync(l.Id," "));
  await S(d,new(2,null,true)).DeclineAsync(l.Id,"Not needed");d.ChangeTracker.Clear();l=await d.Set<EmployeeLetter>().AsNoTracking().SingleAsync();Assert.Equal("Declined",l.Status);
  var scope=new AccessScope(d,new U(1,e.Id,false));await Assert.ThrowsAsync<DomainException>(()=>S(d,new(1,e.Id,false)).OpenAsync(l.Id,scope));
 }
 [Fact]public async Task OnlyTheOwnerOrHrCanOpenAnIssuedLetter()
 {
  await using var d=await Db();var emps=await d.Set<Employee>().OrderBy(x=>x.Id).Take(2).ToListAsync();await S(d,new(1,emps[0].Id,false)).RequestAsync("Bank",false);var l=await d.Set<EmployeeLetter>().AsNoTracking().SingleAsync();await S(d,new(2,null,true)).IssueAsync(l.Id);
  var other=new U(5,emps[1].Id,false);await Assert.ThrowsAsync<DomainException>(()=>S(d,other).OpenAsync(l.Id,new AccessScope(d,other)));
  var owner=new U(1,emps[0].Id,false);var (got,_)=await S(d,owner).OpenAsync(l.Id,new AccessScope(d,owner));Assert.Equal(l.Id,got.Id);
 }
}
