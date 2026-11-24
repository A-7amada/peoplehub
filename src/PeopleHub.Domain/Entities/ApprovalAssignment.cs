namespace PeopleHub.Domain.Entities;
/// <summary>Append-only reassignment of one pending approval stage; routing snapshots stay unchanged.</summary>
public class ApprovalAssignment
{
 public int Id {get;set;}
 public string Kind {get;set;}="";
 public int RequestId {get;set;}
 public int StageIndex {get;set;}
 public int ToUserId {get;set;}
 public string ToEmail {get;set;}="";
 public string Actor {get;set;}="";
 public string Reason {get;set;}="";
 public DateTime At {get;set;}
}
