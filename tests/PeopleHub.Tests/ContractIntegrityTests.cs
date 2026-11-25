using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class ContractIntegrityTests
{
 class User:ICurrentUser{public string Email=>"contracts@example.local";public int? EmployeeId=>1;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>p==Perm.EmpManage;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static EmployeeService S(AppDbContext d){var u=new User();var c=new AppClock(TimeProvider.System);return new(d,new AccessScope(d,u),u,new AuditService(d,u,c),c);}
 [Fact]public void InvalidStatusNeverMutatesEmployee(){var e=new Employee();Assert.Throws<DomainException>(()=>e.ChangeStatus((EmployeeStatus)999,"actor","reason",DateTime.UtcNow));Assert.Equal(EmployeeStatus.Active,e.Status);}
 [Fact]public void ContractEnumsAndSameDayTimesCannotBypassDates(){Assert.Throws<DomainException>(()=>Contract.Create(1,(ContractType)999,new(2026,1,1),null,null));Assert.Throws<DomainException>(()=>Contract.Create(1,ContractType.FixedTerm,new(2026,1,1,9,0,0),new(2026,1,1,17,0,0),null));var c=Contract.Create(1,ContractType.FixedTerm,new(2026,1,1),new(2026,12,31),null);Assert.Throws<DomainException>(()=>c.Renew(new(2026,12,31,17,0,0)));Assert.True(c.IsCurrent);}
 [Fact]public async Task MissingEmployeeAndInvalidTypeNeverReplaceCurrentContract(){await using var d=await Db();var before=await d.Set<Contract>().CountAsync();await Assert.ThrowsAsync<DomainException>(()=>S(d).AddContractAsync(999999,ContractType.Permanent,new(2027,1,1),null,null));await Assert.ThrowsAsync<DomainException>(()=>S(d).AddContractAsync(6,(ContractType)999,new(2027,1,1),null,null));Assert.Equal(before,await d.Set<Contract>().CountAsync());}
 [Fact]public async Task OldContractCannotBeRenewedTwice(){await using var d=await Db();await S(d).AddContractAsync(6,ContractType.FixedTerm,new(2027,1,1),new(2027,12,31),"Fictional term");var old=await d.Set<Contract>().SingleAsync(x=>x.EmployeeId==6&&x.IsCurrent);var stamp=(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp;await S(d).RenewContractAsync(old.Id,new(2028,12,31));Assert.NotEqual(stamp,(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp);await Assert.ThrowsAsync<DomainException>(()=>S(d).RenewContractAsync(old.Id,new(2029,12,31)));Assert.Single(await d.Set<Contract>().Where(x=>x.EmployeeId==6&&x.IsCurrent).ToListAsync());}
}
