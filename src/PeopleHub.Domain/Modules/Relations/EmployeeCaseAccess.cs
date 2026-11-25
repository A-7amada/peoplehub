namespace PeopleHub.Domain.Entities;
/// <summary>Append-only explicit per-case investigator grant or revocation, not a general HR permission.</summary>
public class EmployeeCaseAccess
{
 public int Id {get;set;}
 public int CaseId {get;set;}
 public int UserId {get;set;}
 public bool Granted {get;set;}
 public string Reason {get;set;}="";
 public int ActorUserId {get;set;}
 public string Actor {get;set;}="";
 public DateTime At {get;set;}
}
