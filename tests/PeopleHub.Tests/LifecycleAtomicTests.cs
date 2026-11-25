using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class LifecycleAtomicTests
{
 class User:ICurrentUser{public string Email=>"lifecycleatomic@example.local";public int? EmployeeId=>8;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>true;}
 class Db(DbContextOptions options):AppDbContext(options){public bool Fail;public override async Task<int> SaveChangesAsync(CancellationToken ct=default){var n=await base.SaveChangesAsync(ct);if(Fail)throw new InvalidOperationException("Fictional post-write failure");return n;}}
 static EmployeeLifecycle S(AppDbContext d){var u=new User();var c=new AppClock(TimeProvider.System);var a=new AuditService(d,u,c);return new(d,u,new(d,u),c,a,new(d,u,c));}
 static async Task<Db> Open(){var d=new Db(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.EnsureCreatedAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static async Task<Func<Task>> Operation(Db d,string kind)
 {
  var s=S(d);
  switch(kind){
   case "AddDependent":return ()=>s.AddDependentAsync(6,"Fictional child","Child",null);
   case "RemoveDependent":await s.AddDependentAsync(6,"Fictional child","Child",null);int dep=await d.Set<EmployeeDependent>().Select(x=>x.Id).SingleAsync();return ()=>s.RemoveDependentAsync(dep,"Fictional correction");
   case "CompleteTask":await s.AddTaskAsync(6,"Clearance","Fictional asset receipt",8,new(2026,12,1));int task=await d.Set<LifecycleTask>().Select(x=>x.Id).SingleAsync();return ()=>s.CompleteTaskAsync(task,"Receipt checked");
   case "SetProbation":return ()=>s.SetProbationAsync(6,new(2026,12,1),"Fictional offer term");
   case "DecideProbation":await s.SetProbationAsync(6,new(2026,12,1),"Fictional offer term");return ()=>s.DecideProbationAsync(6,"Confirmed","Fictional review");
   case "AddField":return ()=>s.AddFieldAsync("Fictional field",null,"Number");
   case "SetField":await s.AddFieldAsync("Fictional field",null,"Number");int field=await d.Set<HrFieldDefinition>().Select(x=>x.Id).SingleAsync();return ()=>s.SetFieldAsync(6,field,"12.5");
   default:throw new InvalidOperationException();
  }
 }
 static async Task<string> Snapshot(Db d)=>System.Text.Json.JsonSerializer.Serialize(new{Dep=await d.Set<EmployeeDependent>().OrderBy(x=>x.Id).ToListAsync(),Tasks=await d.Set<LifecycleTask>().OrderBy(x=>x.Id).ToListAsync(),Probation=await d.Set<EmployeeProbation>().OrderBy(x=>x.Id).ToListAsync(),Fields=await d.Set<HrFieldDefinition>().OrderBy(x=>x.Id).ToListAsync(),Values=await d.Set<EmployeeFieldValue>().OrderBy(x=>x.Id).ToListAsync(),Audit=await d.Set<AuditLog>().CountAsync(),Notifications=await d.Set<Notification>().CountAsync()});
 [Theory][InlineData("AddDependent")][InlineData("RemoveDependent")][InlineData("CompleteTask")][InlineData("SetProbation")][InlineData("DecideProbation")][InlineData("AddField")][InlineData("SetField")]
 public async Task PostSaveFailureRollsBackAndAllowsCleanRetry(string kind){await using var d=await Open();var work=await Operation(d,kind);d.ChangeTracker.Clear();var before=await Snapshot(d);d.Fail=true;await Assert.ThrowsAsync<InvalidOperationException>(work);d.Fail=false;Assert.Equal(before,await Snapshot(d));Assert.False(d.ChangeTracker.HasChanges());await work();Assert.NotEqual(before,await Snapshot(d));}
 [Theory][InlineData("AddDependent")][InlineData("RemoveDependent")][InlineData("CompleteTask")][InlineData("SetProbation")][InlineData("DecideProbation")][InlineData("AddField")][InlineData("SetField")]
 public async Task OuterFailureRollsBackLifecycleMutationAndAudit(string kind){await using var d=await Open();var work=await Operation(d,kind);d.ChangeTracker.Clear();var before=await Snapshot(d);await Assert.ThrowsAsync<DomainException>(()=>d.AtomicAsync(async()=>{await work();throw new DomainException("Fictional outer failure");}));Assert.Equal(before,await Snapshot(d));Assert.False(d.ChangeTracker.HasChanges());}
}
