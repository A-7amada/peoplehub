namespace PeopleHub.Domain.Entities;
/// <summary>ENT-002: one staged employee CSV. Holds only the eight non-sensitive columns. A discarded batch is deleted; a committed batch is kept for reconciliation.</summary>
public class EmployeeImportBatch
{
 public int Id {get;set;}
 /// <summary>SHA-256 of the uploaded bytes. One batch per file content, so the same file cannot be staged or committed twice.</summary>
 public string FileHash {get;set;}="";
 public string Status {get;set;}="Staged";
 public int RowCount {get;set;}
 public string CreatedBy {get;set;}="";
 public DateTime CreatedAt {get;set;}
 public string? CommittedBy {get;set;}
 public DateTime? CommittedAt {get;set;}
}
public class StagedEmployeeRow
{
 public int Id {get;set;}
 public int BatchId {get;set;}
 public int RowNumber {get;set;}
 public string Code {get;set;}="";
 public string FullName {get;set;}="";
 public string FullNameAr {get;set;}="";
 public string Email {get;set;}="";
 public DateTime HireDate {get;set;}
 public int DepartmentId {get;set;}
 public int BranchId {get;set;}
 public int JobTitleId {get;set;}
 /// <summary>Set when the batch is committed.</summary>
 public int? EmployeeId {get;set;}
}
