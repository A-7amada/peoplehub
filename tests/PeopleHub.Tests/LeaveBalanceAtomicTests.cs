using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Leave;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class LeaveBalanceAtomicTests
{
 class User:ICurrentUser{public string Email=>"balance@example.local";public int? EmployeeId=>6;public int UserId=>7;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>false;}
 class Db(DbContextOptions options):AppDbContext(options){public bool FailAfterSave;public override async Task<int> SaveChangesAsync(CancellationToken ct=default){var n=await base.SaveChangesAsync(ct);if(FailAfterSave)throw new InvalidOperationException("Fictional post-save fault");return n;}}
 static async Task<Db> Open(){var d=new Db(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.EnsureCreatedAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static LeaveService S(Db d){var u=new User();var c=new AppClock(TimeProvider.System);var a=new AuditService(d,u,c);return new(d,new(d,u),u,a,new(d,u,c),c,new(d,u,a));}
 [Fact]public async Task PostSaveFailureRollsBackOpeningBalancesLedgerAndEmployeeThenRetriesOnce(){await using var d=await Open();var before=(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp;d.FailAfterSave=true;await Assert.ThrowsAsync<InvalidOperationException>(()=>S(d).BalancesAsync(6,2028));d.FailAfterSave=false;Assert.Empty(await d.Set<LeaveBalance>().Where(x=>x.Year==2028).ToListAsync());Assert.Empty(await d.Set<LeaveTransaction>().Where(x=>x.Year==2028).ToListAsync());Assert.Equal(before,(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp);var rows=await S(d).BalancesAsync(6,2028);var stamp=(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp;Assert.NotEqual(before,stamp);Assert.Equal(rows.Count,await d.Set<LeaveTransaction>().CountAsync(x=>x.EmployeeId==6&&x.Year==2028));var again=await S(d).BalancesAsync(6,2028);Assert.Equal(rows.Count,again.Count);Assert.Equal(stamp,(await d.Set<Employee>().FindAsync(6))!.LeaveRequestStamp);Assert.Equal(rows.Count,await d.Set<LeaveTransaction>().CountAsync(x=>x.EmployeeId==6&&x.Year==2028));}
 [Fact]public async Task FailedNestedRequestLeavesNoAnnualOpeningArtifacts(){await using var d=await Open();await Assert.ThrowsAsync<DomainException>(()=>S(d).RequestAsync(1,new(2028,4,2),new(2028,4,1),"Fictional invalid dates",null));Assert.Empty(await d.Set<LeaveBalance>().Where(x=>x.Year==2028).ToListAsync());Assert.Empty(await d.Set<LeaveTransaction>().Where(x=>x.Year==2028).ToListAsync());}
 [Theory][InlineData(0)][InlineData(10000)]public async Task UnsupportedYearsDoNotCreateData(int year){await using var d=await Open();await Assert.ThrowsAsync<DomainException>(()=>S(d).BalancesAsync(6,year));Assert.False(d.ChangeTracker.HasChanges());}
}
