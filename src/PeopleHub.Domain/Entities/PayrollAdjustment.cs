namespace PeopleHub.Domain.Entities;
/// <summary>Append-only correction proposal referencing a locked source line, never changing that line.</summary>
public class PayrollAdjustment
{
 public int Id{get;set;} public int SourceRunId{get;set;} public int SourceLineId{get;set;} public int SourceVersion{get;set;} public int TargetRunId{get;set;} public int EmployeeId{get;set;}
 public string Kind{get;set;}=""; public decimal Amount{get;set;} public string Reason{get;set;}=""; public int ProposedByUserId{get;set;} public string Actor{get;set;}=""; public DateTime At{get;set;}
}
public class PayrollAdjustmentEvent
{
 public int Id{get;set;} public int AdjustmentId{get;set;} public int Sequence{get;set;} public string Action{get;set;}=""; public string Reason{get;set;}=""; public string Actor{get;set;}=""; public DateTime At{get;set;}
}
