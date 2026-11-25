namespace PeopleHub.Domain.Entities;
/// <summary>Immutable private case evidence. Never an employee-owned general attachment.</summary>
public class EmployeeCaseFile
{
 public int Id {get;set;}
 public int CaseId {get;set;}
 public string Token {get;set;}=Guid.NewGuid().ToString("N");
 public string FileName {get;set;}="";
 public string ContentType {get;set;}="";
 public byte[] Data {get;set;}=[];
 public string Sha256 {get;set;}="";
 public string SourceReference {get;set;}="";
 public int ActorUserId {get;set;}
 public string Actor {get;set;}="";
 public DateTime At {get;set;}
}
