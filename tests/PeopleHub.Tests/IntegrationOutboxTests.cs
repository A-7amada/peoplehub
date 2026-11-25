using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Operations;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
namespace PeopleHub.Tests;
/// <summary>ARC-007. SQLite only: these prove the outbox logic, not PostgreSQL concurrency.</summary>
public class FakeTimeProvider(DateTimeOffset start):TimeProvider{DateTimeOffset now=start;public override DateTimeOffset GetUtcNow()=>now;public void Advance(TimeSpan s)=>now+=s;}
public class IntegrationOutboxTests
{
 class H(string type,int ver,Func<IntegrationEvent,Task> act):IIntegrationHandler{public string EventType=>type;public int SchemaVersion=>ver;public Task HandleAsync(IntegrationEvent e)=>act(e);}
 static async Task<AppDbContext> Open(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.EnsureCreatedAsync();return d;}
 static (IntegrationOutbox o,FakeTimeProvider t) Make(AppDbContext d,params IIntegrationHandler[] h){var t=new FakeTimeProvider(new DateTimeOffset(2026,11,25,10,0,0,TimeSpan.Zero));return (new IntegrationOutbox(d,new AppClock(t),h),t);}
 const string P="{\"a\":1}";
 [Fact]public async Task EnqueueIsIdempotentByKey(){await using var d=await Open();var (o,_)=Make(d);Assert.True(await o.EnqueueAsync("t",1,"k1",P));await d.SaveChangesAsync();Assert.False(await o.EnqueueAsync("t",1,"k1",P));Assert.True(await o.EnqueueAsync("t",1,"k2",P));await d.SaveChangesAsync();Assert.Equal(2,await d.Set<IntegrationEvent>().CountAsync());}
 [Fact]public async Task EnqueueRollsBackWithCallersTransaction(){await using var d=await Open();var (o,_)=Make(d);await Assert.ThrowsAsync<DomainException>(()=>d.AtomicAsync(async()=>{await o.EnqueueAsync("t",1,"k1",P);await d.SaveChangesAsync();throw new DomainException("err.generic");}));Assert.Equal(0,await d.Set<IntegrationEvent>().CountAsync());}
 [Theory][InlineData("",1,"k","{}")][InlineData("t",0,"k","{}")][InlineData("t",1,"","{}")][InlineData("t",1,"k","not json")]
 public async Task EnqueueRejectsInvalidInput(string type,int ver,string key,string payload){await using var d=await Open();var (o,_)=Make(d);await Assert.ThrowsAsync<DomainException>(()=>o.EnqueueAsync(type,ver,key,payload));}
 [Fact]public async Task DeliversAndMarksDone(){await using var d=await Open();var n=0;var (o,_)=Make(d,new H("t",1,_=>{n++;return Task.CompletedTask;}));await o.EnqueueAsync("t",1,"k",P);await d.SaveChangesAsync();Assert.Equal(1,await o.ProcessDueAsync());Assert.Equal(0,await o.ProcessDueAsync());Assert.Equal(1,n);var e=await d.Set<IntegrationEvent>().SingleAsync();Assert.Equal("Done",e.Status);Assert.NotNull(e.ProcessedAt);}
 [Fact]public async Task FailureRetriesWithBackoffThenDeadLetters(){await using var d=await Open();var (o,t)=Make(d,new H("t",1,_=>throw new InvalidOperationException("secret-payload-value")));await o.EnqueueAsync("t",1,"k",P);await d.SaveChangesAsync();
  var expected=new[]{2,4,8,16};
  for(int i=0;i<4;i++){Assert.Equal(0,await o.ProcessDueAsync());var e=await d.Set<IntegrationEvent>().AsNoTracking().SingleAsync();Assert.Equal("Pending",e.Status);Assert.Equal(i+1,e.Attempts);Assert.Equal(t.GetUtcNow().UtcDateTime.AddMinutes(expected[i]),e.NextAttemptAt);
   Assert.Equal(0,await o.ProcessDueAsync());Assert.Equal(i+1,(await d.Set<IntegrationEvent>().AsNoTracking().SingleAsync()).Attempts);t.Advance(TimeSpan.FromMinutes(expected[i]));}
  Assert.Equal(0,await o.ProcessDueAsync());var f=await d.Set<IntegrationEvent>().AsNoTracking().SingleAsync();Assert.Equal("DeadLetter",f.Status);Assert.Equal(5,f.Attempts);Assert.Null(f.NextAttemptAt);Assert.Equal("InvalidOperationException",f.LastError);Assert.DoesNotContain("secret",f.LastError);
  t.Advance(TimeSpan.FromDays(1));Assert.Equal(0,await o.ProcessDueAsync());Assert.Equal(5,(await d.Set<IntegrationEvent>().AsNoTracking().SingleAsync()).Attempts);}
 [Fact]public async Task UnknownTypeOrVersionMismatchDeadLetters(){await using var d=await Open();var (o,_)=Make(d,new H("t",2,_=>Task.CompletedTask));await o.EnqueueAsync("t",1,"k1",P);await o.EnqueueAsync("zzz",1,"k2",P);await d.SaveChangesAsync();Assert.Equal(0,await o.ProcessDueAsync());var all=await d.Set<IntegrationEvent>().AsNoTracking().ToListAsync();Assert.All(all,e=>Assert.Equal("DeadLetter",e.Status));}
 [Fact]public async Task HandlerWritesRollBackWhenHandlerFails(){await using var d=await Open();var (o,_)=Make(d,new H("t",1,async _=>{d.Add(new AuditLog{At=DateTime.UtcNow,User="x",Action="a",Entity="e",EntityId="1",Details="d"});await d.SaveChangesAsync();throw new InvalidOperationException();}));await o.EnqueueAsync("t",1,"k",P);await d.SaveChangesAsync();await o.ProcessDueAsync();Assert.Equal(0,await d.Set<AuditLog>().CountAsync());}
 [Fact]public async Task ReconcileCountsByStatus(){await using var d=await Open();var (o,t)=Make(d,new H("ok",1,_=>Task.CompletedTask));await o.EnqueueAsync("ok",1,"k1",P);await o.EnqueueAsync("bad",1,"k2",P);await d.SaveChangesAsync();await o.ProcessDueAsync();t.Advance(TimeSpan.FromMinutes(1));await o.EnqueueAsync("ok",1,"k3",P);await d.SaveChangesAsync();var r=await o.ReconcileAsync();Assert.Equal(1,r.Done);Assert.Equal(1,r.DeadLetter);Assert.Equal(1,r.Pending);Assert.Equal(t.GetUtcNow().UtcDateTime,r.OldestPendingAt);}
}
public class OperationsProjectionTests
{
 [Fact]public async Task CountsPendingByKindAndOutboxWithoutIdentifiers()
 {
  await using var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.EnsureCreatedAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));
  var t=new FakeTimeProvider(new DateTimeOffset(2026,11,25,10,0,0,TimeSpan.Zero));var c=new AppClock(t);
  var o0=new IntegrationOutbox(d,c,[]);var b=(await new OperationsProjection(d,c,o0).SnapshotAsync()).Approvals.Single(x=>x.Kind=="Overtime");
  d.Add(new OvertimeRequest{EmployeeId=6,Route="HRManager",Hours=1,Date=new(2026,11,20),CreatedAt=c.UtcNow.AddDays(-3)});d.Add(new OvertimeRequest{EmployeeId=6,Route="HRManager",Hours=1,Date=new(2026,11,21),CreatedAt=c.UtcNow.AddDays(-1),Status=RequestStatus.Approved});
  var o=new IntegrationOutbox(d,c,[]);await o.EnqueueAsync("x",1,"k",P);await d.SaveChangesAsync();
  var s=await new OperationsProjection(d,c,o).SnapshotAsync();
  var ot=s.Approvals.Single(x=>x.Kind=="Overtime");Assert.Equal(b.Pending+1,ot.Pending);Assert.True(ot.OldestAgeDays>=3);Assert.Equal(1,s.Outbox.Pending);
  Assert.DoesNotContain(typeof(OperationsSnapshot).GetProperties().Concat(typeof(PendingKind).GetProperties()),p=>p.Name.Contains("Employee")||p.Name.Contains("Name"));
 }
 const string P="{}";
}
