namespace PeopleHub.Domain.Entities;

/// <summary>US-PH3-008 draft of a workflow cycle. One open draft per request kind. Live routing is unchanged until the draft is published. StepsJson is a list of draft steps.</summary>
public class WorkflowDraft
{
 public int Id {get;set;}
 public string Kind {get;set;}="";
 public string StepsJson {get;set;}="[]";
 public string CreatedBy {get;set;}="";
 public DateTime CreatedAt {get;set;}
 public string UpdatedBy {get;set;}="";
 public DateTime UpdatedAt {get;set;}
}
