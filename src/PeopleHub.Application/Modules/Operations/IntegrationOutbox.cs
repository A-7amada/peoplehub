using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Operations;
/// <summary>Handler for one event type and schema version. No concrete adapters ship yet.</summary>
public interface IIntegrationHandler
{
 string EventType {get;}
 int SchemaVersion {get;}
 Task HandleAsync(IntegrationEvent e);
}
public record OutboxReconciliation(int Pending,int Done,int DeadLetter,DateTime? OldestPendingAt);
/// <summary>ARC-007: transactional outbox with idempotent enqueue, bounded retry with backoff, dead-lettering and reconciliation counts.</summary>
public class IntegrationOutbox(IRepository repo,AppClock clock,IEnumerable<IIntegrationHandler> handlers)
{
 public const int MaxAttempts=5;
 public const int MaxKeyLength=200;
 public const int MaxPayloadLength=16000;
 /// <summary>Adds the event to the caller's unit of work. It does not save, so it commits or rolls back with the business change that produced it. Returns false when the key already exists.</summary>
 public async Task<bool> EnqueueAsync(string eventType,int schemaVersion,string idempotencyKey,string payloadJson)
 {
  if(string.IsNullOrWhiteSpace(eventType)||eventType.Length>100)throw new DomainException("err.generic");
  if(schemaVersion<1)throw new DomainException("err.generic");
  if(string.IsNullOrWhiteSpace(idempotencyKey)||idempotencyKey.Length>MaxKeyLength)throw new DomainException("err.generic");
  if(payloadJson==null||payloadJson.Length>MaxPayloadLength)throw new DomainException("err.generic");
  try{using var _=System.Text.Json.JsonDocument.Parse(payloadJson);}catch(System.Text.Json.JsonException){throw new DomainException("err.generic");}
  if(await repo.AnyAsync(repo.Query<IntegrationEvent>().Where(x=>x.IdempotencyKey==idempotencyKey)))return false;
  repo.Add(new IntegrationEvent{EventType=eventType,SchemaVersion=schemaVersion,IdempotencyKey=idempotencyKey,Payload=payloadJson,CreatedAt=clock.UtcNow});
  return true;
 }
 /// <summary>Delivers due events one at a time, each in its own transaction. Returns the number delivered.</summary>
 public async Task<int> ProcessDueAsync(int batch=20)
 {
  var now=clock.UtcNow;int delivered=0;
  var ids=(await repo.ToListAsync(repo.Query<IntegrationEvent>().Where(x=>x.Status=="Pending"&&(x.NextAttemptAt==null||x.NextAttemptAt<=now)).OrderBy(x=>x.Id).Take(Math.Clamp(batch,1,100)).Select(x=>x.Id))).ToList();
  foreach(var id in ids)if(await DeliverAsync(id))delivered++;
  return delivered;
 }
 async Task<bool> DeliverAsync(int id)
 {
  var ok=false;
  try
  {
   // The handler's own writes and the Done mark commit together, or both roll back.
   await repo.AtomicAsync(async()=>
   {
    var e=await repo.FirstOrDefaultAsync(repo.Query<IntegrationEvent>().Where(x=>x.Id==id&&x.Status=="Pending"));if(e==null)return;
    var h=handlers.FirstOrDefault(x=>x.EventType==e.EventType&&x.SchemaVersion==e.SchemaVersion);
    if(h==null)throw new MissingHandlerException();
    await h.HandleAsync(e);
    e.Attempts++;e.Status="Done";e.ProcessedAt=clock.UtcNow;e.LastError=null;e.NextAttemptAt=null;
    await repo.SaveAsync();ok=true;
   });
  }
  catch(Exception ex)
  {
   // Only the exception type is stored: messages can echo payload data.
   await repo.AtomicAsync(async()=>
   {
    var e=await repo.FirstOrDefaultAsync(repo.Query<IntegrationEvent>().Where(x=>x.Id==id&&x.Status=="Pending"));if(e==null)return;
    e.Attempts++;e.LastError=ex is MissingHandlerException?"No handler for this event type and version":ex.GetType().Name;
    if(ex is MissingHandlerException||e.Attempts>=MaxAttempts){e.Status="DeadLetter";e.NextAttemptAt=null;}
    else e.NextAttemptAt=clock.UtcNow.AddMinutes(Math.Pow(2,e.Attempts));
    await repo.SaveAsync();
   });
  }
  return ok;
 }
 sealed class MissingHandlerException:Exception{}
 public async Task<OutboxReconciliation> ReconcileAsync()
 {
  var rows=await repo.ToListAsync(repo.Query<IntegrationEvent>().Select(x=>new{x.Status,x.CreatedAt}));
  return new(rows.Count(x=>x.Status=="Pending"),rows.Count(x=>x.Status=="Done"),rows.Count(x=>x.Status=="DeadLetter"),rows.Where(x=>x.Status=="Pending").Select(x=>(DateTime?)x.CreatedAt).Min());
 }
}
