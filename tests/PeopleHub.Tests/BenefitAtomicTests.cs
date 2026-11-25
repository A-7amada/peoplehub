using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Benefits;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class BenefitAtomicTests
{
 class User:ICurrentUser{public string Email=>"benefitatomic@example.local";public int? EmployeeId=>8;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>p==Perm.BenefitsManage;}
 class Db(DbContextOptions options):AppDbContext(options){public bool Fail;public override async Task<int> SaveChangesAsync(CancellationToken ct=default){var n=await base.SaveChangesAsync(ct);if(Fail)throw new InvalidOperationException("Fictional post-write failure");return n;}}
 static async Task<Db> Open(){var d=new Db(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.EnsureCreatedAsync();return d;}
 static BenefitService S(AppDbContext d){var u=new User();var c=new AppClock(TimeProvider.System);return new(d,u,c,new AuditService(d,u,c));}
 static Task Add(AppDbContext d)=>S(d).AddPlanAsync("Fictional plan","Fictional provider","Explicit review needed","Internal record only",false,100,25,"EGP");
 [Fact]public async Task PostSaveFailureRollsBackPlanAndAuditThenRetriesOnce(){await using var d=await Open();d.Fail=true;await Assert.ThrowsAsync<InvalidOperationException>(()=>Add(d));d.Fail=false;Assert.Empty(await d.Set<BenefitPlan>().ToListAsync());Assert.Empty(await d.Set<AuditLog>().ToListAsync());Assert.False(d.ChangeTracker.HasChanges());await Add(d);Assert.Single(await d.Set<BenefitPlan>().ToListAsync());Assert.Single(await d.Set<AuditLog>().ToListAsync());}
 [Fact]public async Task OuterFailureRollsBackPlanAndAudit(){await using var d=await Open();await Assert.ThrowsAsync<DomainException>(()=>d.AtomicAsync(async()=>{await Add(d);throw new DomainException("Fictional outer failure");}));Assert.Empty(await d.Set<BenefitPlan>().ToListAsync());Assert.Empty(await d.Set<AuditLog>().ToListAsync());}
 [Fact]public async Task InvalidPlanLeavesNoStagedOrPersistedChanges(){await using var d=await Open();await Assert.ThrowsAsync<DomainException>(()=>S(d).AddPlanAsync("Fictional","Provider","Review","No coverage",false,-1,0,"EGP"));Assert.Empty(await d.Set<BenefitPlan>().ToListAsync());Assert.Empty(await d.Set<AuditLog>().ToListAsync());Assert.False(d.ChangeTracker.HasChanges());}
}
