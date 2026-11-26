namespace PeopleHub.Domain.Entities;
/// <summary>EMP-004 (owner-approved default, 2026-11-25 22:23): reversing a final status (resigned, terminated, retired) needs a reason and a second HR Manager to approve. Old records stay; the status history gains one more row.</summary>
public class RehireRequest
{
 public int Id {get;set;}
 public int EmployeeId {get;set;}
 public EmployeeStatus FromStatus {get;set;}
 public string Reason {get;set;}="";
 public int RequestedByUserId {get;set;}
 public string RequestedBy {get;set;}="";
 public DateTime RequestedAt {get;set;}
 /// <summary>Pending, Approved or Rejected.</summary>
 public string Status {get;set;}="Pending";
 public int? DecidedByUserId {get;set;}
 public string DecidedBy {get;set;}="";
 public DateTime? DecidedAt {get;set;}
 public string DecisionNote {get;set;}="";
}
