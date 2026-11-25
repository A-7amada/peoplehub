using PeopleHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Application.Modules.Requests;
namespace PeopleHub.Tests;
public class AdvanceValidationTests
{
 [Theory][InlineData(0,2027,1)][InlineData(-1,2027,1)][InlineData(1.001,2027,1)][InlineData(100,2027,0)][InlineData(100,2027,13)][InlineData(100,1999,1)][InlineData(100,9999,1)]public void InvalidAmountOrPeriodIsRefused(decimal amount,int year,int month)=>Assert.Throws<DomainException>(()=>AdvanceRequest.Create(new Employee{BasicSalary=10000},amount,3,year,month,"test"));
 [Fact]public void DecimalCentsAndExplicitPeriodAreRetained(){var a=AdvanceRequest.Create(new Employee{BasicSalary=10000},601.25m,3,2027,1,"Fictional");Assert.Equal(601.25m,a.Amount);Assert.Equal(200.42m,a.Monthly);Assert.Equal(1,a.StartMonth);}
 class User:ICurrentUser{public string Email=>"employee@peoplehub.local";public int? EmployeeId=>7;public int UserId=>7;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>false;}
 [Fact]public async Task RequestValidatesEmploymentAndPeriodAndIsAtomic(){await using var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));var u=new User();var c=new AppClock(TimeProvider.System);var a=new AuditService(d,u,c);var svc=new ApprovalService(d,new AccessScope(d,u),u,a,new NotificationService(d,u,c),c,new WorkflowService(d,u,a),new DelegationService(d,u,a,c));await Assert.ThrowsAsync<DomainException>(()=>svc.RequestAdvanceAsync(100,3,2001,1,"old"));var e=(await d.Set<Employee>().FindAsync(7))!;e.Status=EmployeeStatus.Suspended;await d.SaveAsync();await Assert.ThrowsAsync<DomainException>(()=>svc.RequestAdvanceAsync(100,3,2030,1,"not working"));e=(await d.Set<Employee>().FindAsync(7))!;e.Status=EmployeeStatus.Active;await d.SaveAsync();await svc.RequestAdvanceAsync(601.25m,3,2030,1,"Fictional");await Assert.ThrowsAsync<DomainException>(()=>svc.RequestAdvanceAsync(500,3,2030,1,"duplicate open"));Assert.Single(await d.Set<AdvanceRequest>().Where(x=>x.EmployeeId==7).ToListAsync());Assert.NotEmpty((await d.Set<Employee>().FindAsync(7))!.LeaveRequestStamp);}

}
