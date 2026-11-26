namespace PeopleHub.Domain.Entities;
/// <summary>US-PH2-016: an employee asks for an employment confirmation letter, HR issues or declines it. The text is a snapshot taken at issue
/// time so later profile edits never change an issued letter. The wording is neutral and factual (no policy statements).</summary>
public class EmployeeLetter
{
 public int Id {get;set;}
 public int EmployeeId {get;set;}
 public string AddressedTo {get;set;}="";
 public bool IncludeSalary {get;set;}
 /// <summary>Requested, Issued or Declined.</summary>
 public string Status {get;set;}="Requested";
 public int RequestedByUserId {get;set;}
 public string RequestedBy {get;set;}="";
 public DateTime RequestedAt {get;set;}
 public int? DecidedByUserId {get;set;}
 public string DecidedBy {get;set;}="";
 public DateTime? DecidedAt {get;set;}
 public string DecisionNote {get;set;}="";
 public string Reference {get;set;}="";
 public string BodyEn {get;set;}="";
 public string BodyAr {get;set;}="";
}
