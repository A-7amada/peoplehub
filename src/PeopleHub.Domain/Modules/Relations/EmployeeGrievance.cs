namespace PeopleHub.Domain.Entities;
/// <summary>Employee's immutable internal intake. It does not grant investigation-case access.</summary>
public class EmployeeGrievance
{
 public int Id{get;set;} public int EmployeeId{get;set;} public int SubmittedByUserId{get;set;} public string Title{get;set;}=""; public string Statement{get;set;}=""; public string Actor{get;set;}=""; public DateTime At{get;set;} public string ChangeStamp{get;set;}="";
}
/// <summary>Explicit response visible to the submitter, never private investigation notes.</summary>
public class EmployeeGrievanceResponse
{
 public int Id{get;set;} public int GrievanceId{get;set;} public int ActorUserId{get;set;} public string Actor{get;set;}=""; public string Note{get;set;}=""; public DateTime At{get;set;}
}
/// <summary>Immutable appeal of an employee-visible response, not an investigation or legal appeal.</summary>
public class EmployeeGrievanceAppeal
{
 public int Id{get;set;} public int GrievanceId{get;set;} public int ResponseId{get;set;} public string Note{get;set;}=""; public int ActorUserId{get;set;} public string Actor{get;set;}=""; public DateTime At{get;set;}
}
/// <summary>Independent internal disposition. Does not overwrite the original response.</summary>
public class EmployeeGrievanceAppealDecision
{
 public int Id{get;set;} public int AppealId{get;set;} public int GrievanceId{get;set;} public string Decision{get;set;}=""; public string Note{get;set;}=""; public int ActorUserId{get;set;} public string Actor{get;set;}=""; public DateTime At{get;set;}
}
