using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class CaseFileAtomicTests
{
 class User:ICurrentUser{public string Email=>"caseatomic@example.local";public int? EmployeeId=>8;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>p==Perm.RelationsManage;}
 class Db(DbContextOptions options):AppDbContext(options)
 {
  public bool FailAfterSave;
  public override async Task<int> SaveChangesAsync(CancellationToken ct=default){var n=await base.SaveChangesAsync(ct);if(FailAfterSave)throw new InvalidOperationException("Fictional post-write failure");return n;}
 }
 static EmployeeRelations S(AppDbContext d){var u=new User();var c=new AppClock(TimeProvider.System);return new(d,u,c,new AuditService(d,u,c));}
 [Fact]public async Task FailureAfterSaveRollsBackPrivateBytesRevisionAndAuditThenRetriesOnce()
 {
  await using var d=new Db(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.EnsureCreatedAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));await S(d).CreateAsync(6,"Grievance","Fictional case","Fictional source");int id=await d.Set<EmployeeCase>().Select(x=>x.Id).SingleAsync();var stamp=(await d.Set<EmployeeCase>().FindAsync(id))!.ChangeStamp;int audit=await d.Set<AuditLog>().CountAsync();
  d.FailAfterSave=true;await Assert.ThrowsAsync<InvalidOperationException>(()=>S(d).AddFileAsync(id,"Fictional source","case.pdf","%PDF-1.4 Fictional test"u8.ToArray()));d.FailAfterSave=false;
  Assert.Empty(await d.Set<EmployeeCaseFile>().ToListAsync());Assert.Equal(stamp,(await d.Set<EmployeeCase>().FindAsync(id))!.ChangeStamp);Assert.Equal(audit,await d.Set<AuditLog>().CountAsync());Assert.False(d.ChangeTracker.HasChanges());
  await S(d).AddFileAsync(id,"Retry","case.pdf","%PDF-1.4 Fictional test"u8.ToArray());Assert.Single(await d.Set<EmployeeCaseFile>().ToListAsync());Assert.NotEqual(stamp,(await d.Set<EmployeeCase>().FindAsync(id))!.ChangeStamp);
 }
 [Fact]public async Task ConcurrentStaleEvidenceCannotCommitAcrossAccessRevisionChange()
 {
  var path=Path.Combine(Path.GetTempPath(),"case-file-"+Guid.NewGuid()+".db");
  try{
   var options=new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source="+path).Options;await using var a=new AppDbContext(options);await a.Database.MigrateAsync();await SeedData.InitializeAsync(a,"Admin123!","Demo123!",new DateTime(2026,11,24));await S(a).CreateAsync(6,"Grievance","Fictional case","Fictional source");int id=await a.Set<EmployeeCase>().Select(x=>x.Id).SingleAsync();
   await using var b=new AppDbContext(options);await b.Set<EmployeeCase>().SingleAsync();await S(a).SetAccessAsync(id,3,true,"Appointment changes case revision");
   await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>S(b).AddFileAsync(id,"Stale evidence","case.pdf","%PDF-1.4 Fictional test"u8.ToArray()));Assert.Empty(await b.Set<EmployeeCaseFile>().ToListAsync());
   await S(b).AddFileAsync(id,"Reloaded evidence","case.pdf","%PDF-1.4 Fictional test"u8.ToArray());Assert.Single(await b.Set<EmployeeCaseFile>().ToListAsync());
  }finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path);File.Delete(path+"-wal");File.Delete(path+"-shm");}
 }
}
