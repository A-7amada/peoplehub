namespace PeopleHub.Domain.Entities;

/// <summary>US-PH2-014 candidate CV or document, stored in the database (free-tier deviation, files in DB). Format and size are checked. No malware scan and no authenticity check.</summary>
public class CandidateFile
{
 public int Id {get;set;}
 public int CandidateId {get;set;}
 public string Token {get;set;}=Guid.NewGuid().ToString("N");
 /// <summary>CV or Document.</summary>
 public string Kind {get;set;}="CV";
 public string FileName {get;set;}="";
 public string ContentType {get;set;}="";
 public byte[] Data {get;set;}=[];
 public string Sha256 {get;set;}="";
 public string Actor {get;set;}="";
 public DateTime At {get;set;}
}
