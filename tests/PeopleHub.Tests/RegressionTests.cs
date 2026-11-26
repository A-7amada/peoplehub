using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Application.Modules.Leave;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Application.Modules.Reporting;
using PeopleHub.Application.Modules.Requests;
using PeopleHub.Application.Modules.Talent;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class RegressionTests {
 class User(bool allowed):ICurrentUser {public string Email=>"test@example.local";public int? EmployeeId=>6;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>allowed;public bool Can(string p)=>allowed;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();return d;}
 static ReportService Reports(AppDbContext d,bool allowed){var u=new User(allowed);var c=new AppClock(TimeProvider.System);var s=new AccessScope(d,u);var a=new AuditService(d,u,c);var n=new NotificationService(d,u,c);var w=new WorkflowService(d,u,a);var at=new AttendanceService(d,s,u,a,n,c,w);return new(d,s,u,at,new ApprovalService(d,s,u,a,n,c,w,new DelegationService(d,u,a,c)),new EmployeeService(d,s,u,a,c),new LeaveService(d,s,u,a,n,c,w),c);}
 [Theory][InlineData("employees")][InlineData("attendance")][InlineData("leave")][InlineData("payroll")][InlineData("turnover")][InlineData("headcount")][InlineData("payrolltrend")][InlineData("approvalstats")]
 public async Task ReportsRejectUsersWithoutReportPermission(string key){await using var d=await Db();await Assert.ThrowsAsync<DomainException>(()=>Reports(d,false).BuildAsync(key,2026,11));}
 [Theory][InlineData("bad",2026,11)][InlineData("attendance",2026,13)][InlineData("employees",10000,1)][InlineData("leave",0,1)]
 public async Task ReportsRejectInvalidInputs(string key,int y,int m){await using var d=await Db();await Assert.ThrowsAsync<DomainException>(()=>Reports(d,true).BuildAsync(key,y,m));}
 [Fact]public async Task BuilderRejectsUnknownDataset(){await using var d=await Db();var u=new User(true);await Assert.ThrowsAsync<DomainException>(()=>new ReportBuilderService(d,new AccessScope(d,u),u).RunAsync("bad",null,null));}
 [Fact]public async Task TrainingRejectsMissingCourseAndEmployee(){await using var d=await Db();var u=new User(true);var c=new AppClock(TimeProvider.System);var t=new TalentService(d,u,new AccessScope(d,u),c,new AuditService(d,u,c),new NotificationService(d,u,c));await Assert.ThrowsAsync<DomainException>(()=>t.EnrollAsync(999,999));}
 [Fact]public async Task CandidateRejectsMissingOpening(){await using var d=await Db();var u=new User(true);var c=new AppClock(TimeProvider.System);var t=new TalentService(d,u,new AccessScope(d,u),c,new AuditService(d,u,c),new NotificationService(d,u,c));await Assert.ThrowsAsync<DomainException>(()=>t.AddCandidateAsync(999,"Fictional",null,null));}
 [Theory][InlineData(0)][InlineData(13)]public async Task AttendanceRejectsInvalidMonth(int month){await using var d=await Db();var u=new User(true);var c=new AppClock(TimeProvider.System);var a=new AuditService(d,u,c);var s=new AttendanceService(d,new AccessScope(d,u),u,a,new NotificationService(d,u,c),c,new WorkflowService(d,u,a));await Assert.ThrowsAsync<DomainException>(()=>s.SummariesAsync([],2026,month));}
 [Fact]public async Task EmployeeOptionalBlankFieldsSaveAndRoundTrip(){await using var d=await Db();var u=new User(true);var c=new AppClock(TimeProvider.System);var dept=new Department{Name="Test"};var branch=new Branch{Name="Test"};var title=new JobTitle{Name="Test"};d.Add(dept);d.Add(branch);d.Add(title);await d.SaveAsync();var e=new Employee{FullName="Fictional",Email="fictional@example.local",DepartmentId=dept.Id,BranchId=branch.Id,JobTitleId=title.Id,HireDate=c.Today,NationalId=null!,Phone=null!,BankName=null!,FullNameAr=null!};await new EmployeeService(d,new AccessScope(d,u),u,new AuditService(d,u,c),c).SaveAsync(e,null);d.ChangeTracker.Clear();var saved=await d.Set<Employee>().SingleAsync();Assert.Equal("",saved.NationalId);Assert.Equal("",saved.Phone);}
}

public class RolePermissionRegressionTests {
 [Theory]
 [InlineData("Employee","payroll.prepare",false)]
 [InlineData("DepartmentManager","payroll.view",false)]
 [InlineData("HRSpecialist","emp.salary",false)]
 [InlineData("AttendanceOfficer","attendance.manage",true)]
 [InlineData("PayrollOfficer","payroll.prepare",true)]
 [InlineData("PayrollOfficer","payroll.financeApprove",false)]
 [InlineData("FinanceManager","payroll.financeApprove",true)]
 [InlineData("FinanceManager","payroll.finalize",false)]
 [InlineData("TopManagement","payroll.finalize",true)]
 [InlineData("TopManagement","payroll.export",false)]
 [InlineData("HRManager","payroll.hrReview",true)]
 [InlineData("HRManager","admin.users",false)]
 public void DefaultRolesHaveSeparatedDuties(string role,string permission,bool expected) {
  Assert.Equal(expected,Perm.Defaults.TryGetValue(role,out var p)&&p.Contains(permission));
 }
}

public class SettlementRegressionTests {
 [Theory][InlineData(24,24,1000)][InlineData(15,24,10000)][InlineData(31,31,1000)]
 public void NewHireOnlyPaidFromHireDay(int hireDay,int lastDay,decimal expected) {
  var r=Settlement.Calculate(30000,new DateTime(2026,1,hireDay),new DateTime(2026,1,lastDay),0,15);
  Assert.Equal(expected,r.ProratedSalary);
 }
 [Fact] public void LastDayBeforeHireRejected()=>Assert.Throws<DomainException>(()=>Settlement.Calculate(30000,new(2026,11,24),new(1900,1,1),0,15));
 [Theory][InlineData(-1,0,15)][InlineData(30000,-1,15)][InlineData(30000,0,-1)]
 public void NegativeSettlementValuesRejected(decimal basic,decimal leave,decimal gratuity)=>Assert.Throws<DomainException>(()=>Settlement.Calculate(basic,new(2020,1,1),new(2026,1,15),leave,gratuity));
}
