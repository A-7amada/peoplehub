using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class ComponentValidationTests
{
 class User(bool pay=true):ICurrentUser{public string Email=>"component@example.local";public int? EmployeeId=>1;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>pay&&p==Perm.EmpPay;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static EmployeeService S(AppDbContext d,bool pay=true){var u=new User(pay);var c=new AppClock(TimeProvider.System);return new(d,new AccessScope(d,u),u,new AuditService(d,u,c),c);}
 [Fact]public async Task MissingEmployeeOrComponentCannotCreateOrphan(){await using var d=await Db();var before=await d.Set<EmployeeSalaryComponent>().CountAsync();var component=await d.Set<SalaryComponent>().Select(x=>x.Id).FirstAsync();await Assert.ThrowsAsync<DomainException>(()=>S(d).SetComponentAsync(999999,component,10));await Assert.ThrowsAsync<DomainException>(()=>S(d).SetComponentAsync(6,999999,10));Assert.Equal(before,await d.Set<EmployeeSalaryComponent>().CountAsync());}
 [Theory][InlineData("-1")][InlineData("1.001")][InlineData("100000001")]public async Task AmountMustBeBoundedNonnegativeCents(string amount){await using var d=await Db();var component=await d.Set<SalaryComponent>().Select(x=>x.Id).FirstAsync();await Assert.ThrowsAsync<DomainException>(()=>S(d).SetComponentAsync(6,component,decimal.Parse(amount,System.Globalization.CultureInfo.InvariantCulture)));}
 [Fact]public async Task ValidChangeTouchesEmployeeAndDoesNotChangePayroll(){await using var d=await Db();var component=await d.Set<SalaryComponent>().Select(x=>x.Id).FirstAsync();var old=(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp;var pay=await d.Set<PayrollLine>().Select(x=>x.Net).ToListAsync();await S(d).SetComponentAsync(6,component,123.45m);var rows=await d.Set<EmployeeSalaryComponent>().Where(x=>x.EmployeeId==6&&x.ComponentId==component).ToListAsync();Assert.Equal(123.45m,Assert.Single(rows).Amount);Assert.NotEqual(old,(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp);Assert.Equal(pay,await d.Set<PayrollLine>().Select(x=>x.Net).ToListAsync());await S(d).SetComponentAsync(6,component,0);Assert.Equal(0,(await d.Set<EmployeeSalaryComponent>().SingleAsync(x=>x.EmployeeId==6&&x.ComponentId==component)).Amount);}
 [Fact]public async Task NoPayPermissionCannotWrite(){await using var d=await Db();await Assert.ThrowsAsync<DomainException>(()=>S(d,false).SetComponentAsync(6,1,10));}
}
