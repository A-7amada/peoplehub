namespace PeopleHub.Domain.Entities;
/// <summary>Immutable vote for a captured participant seat. A human can fill only one seat in a stage.</summary>
public class ApprovalVote
{
 public int Id {get;set;}
 public string Kind {get;set;}="";
 public int RequestId {get;set;}
 public int StageIndex {get;set;}
 public int ParticipantEmployeeId {get;set;}
 public int ActorEmployeeId {get;set;}
 public int ActorUserId {get;set;}
 public DateTime At {get;set;}
}
