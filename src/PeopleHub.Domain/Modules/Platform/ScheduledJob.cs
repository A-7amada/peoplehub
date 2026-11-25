namespace PeopleHub.Domain.Entities;
/// <summary>ARC-004: persisted run state of one in-process maintenance job. Survives restarts so a woken instance can see what ran, what failed and what a crashed run left leased. Holds no employee data and no exception messages.</summary>
public class ScheduledJobState
{
 public int Id {get;set;}
 public string Name {get;set;}="";
 public DateTime? LastStartedAt {get;set;}
 public DateTime? LastFinishedAt {get;set;}
 /// <summary>Running, Succeeded or Failed.</summary>
 public string LastStatus {get;set;}="";
 /// <summary>Exception type name only.</summary>
 public string LastError {get;set;}="";
 public int ConsecutiveFailures {get;set;}
 public long RunCount {get;set;}
 /// <summary>While in the future, another instance or loop must not start the same job. A crashed run frees itself when this passes.</summary>
 public DateTime? LeaseUntil {get;set;}
}
