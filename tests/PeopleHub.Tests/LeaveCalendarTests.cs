using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class LeaveCalendarTests
{
 class User(bool company=false):ICurrentUser{public string Email=>"calendar@example.local";public int? EmployeeId=>6;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>company&&p==Perm.ScopeCompany;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));d.Set<LeaveRequest>().RemoveRange(await d.Set<LeaveRequest>().ToListAsync());await d.SaveAsync();return d;}
 static LeaveCalendar S(AppDbContext d,bool company=false)=>new(d,new AccessScope(d,new User(company)));
 static LeaveRequest R(int e,RequestStatus status,DateTime start,DateTime end)=>new(){EmployeeId=e,LeaveTypeId=1,Status=status,StartDate=start,EndDate=end,Days=1};
 [Fact]public async Task CalendarShowsApprovedOnlyAndOwnScopeWithMonthClipping(){await using var d=await Db();d.AddRange(R(6,RequestStatus.Approved,new(2026,11,30),new(2026,12,2)),R(7,RequestStatus.Approved,new(2026,12,1),new(2026,12,3)),R(6,RequestStatus.Pending,new(2026,12,4),new(2026,12,5)),R(6,RequestStatus.Cancelled,new(2026,12,6),new(2026,12,7)));await d.SaveAsync();var v=await S(d).GetAsync(2026,12,null);Assert.Equal(31,v.Days.Count);Assert.All(v.Days.Take(2),x=>Assert.Equal(6,Assert.Single(x.Away).Id));Assert.All(v.Days.Skip(2),x=>Assert.Empty(x.Away));}
 [Fact]public async Task CalendarDepartmentUsesEffectiveHistoryAndNeverWidensScope(){await using var d=await Db();var e=(await d.Set<Employee>().FindAsync(6))!;var other=await d.Set<Department>().FirstAsync(x=>x.Id!=e.DepartmentId);d.Add(new EmployeeAssignment{EmployeeId=6,DepartmentId=other.Id,BranchId=e.BranchId,EffectiveFrom=new(2026,12,1),EffectiveTo=new DateTime(2026,12,2)});d.Add(R(6,RequestStatus.Approved,new(2026,12,1),new(2026,12,3)));await d.SaveAsync();var v=await S(d).GetAsync(2026,12,other.Id);Assert.Single(v.Days[0].Away);Assert.Empty(v.Days[1].Away);await Assert.ThrowsAsync<DomainException>(()=>S(d).GetAsync(2026,12,999));}
 [Fact]public async Task CalendarRejectsInvalidBoundsAndShowsCompanyScope(){await using var d=await Db();d.AddRange(R(6,RequestStatus.Approved,new(2026,12,1),new(2026,12,1)),R(7,RequestStatus.Approved,new(2026,12,1),new(2026,12,1)));await d.SaveAsync();Assert.Equal(2,(await S(d,true).GetAsync(2026,12,null)).Days[0].Away.Count);await Assert.ThrowsAsync<DomainException>(()=>S(d).GetAsync(2026,13,null));await Assert.ThrowsAsync<DomainException>(()=>S(d).GetAsync(1,1,null));}
}
