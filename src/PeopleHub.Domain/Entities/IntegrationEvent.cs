namespace PeopleHub.Domain.Entities;
/// <summary>Transactional outbox row for an internal domain or integration event. Payload is a versioned JSON document that must not carry secrets.</summary>
public class IntegrationEvent
{
 public int Id {get;set;}
 public string EventType {get;set;}="";
 public int SchemaVersion {get;set;}=1;
 /// <summary>Unique per logical event so a retried or replayed producer cannot create a duplicate.</summary>
 public string IdempotencyKey {get;set;}="";
 public string Payload {get;set;}="{}";
 /// <summary>Pending, Done or DeadLetter.</summary>
 public string Status {get;set;}="Pending";
 public int Attempts {get;set;}
 public DateTime? NextAttemptAt {get;set;}
 public string? LastError {get;set;}
 public DateTime CreatedAt {get;set;}
 public DateTime? ProcessedAt {get;set;}
}
