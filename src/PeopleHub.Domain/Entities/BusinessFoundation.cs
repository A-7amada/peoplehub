namespace PeopleHub.Domain.Entities;

/// <summary>Append-only calculation evidence. Does not authorize payment.</summary>
public class PayrollCalculationSnapshot
{
 public int Id { get; set; }
 public int RunId { get; set; }
 public int Version { get; set; }
 public string Event { get; set; } = "";
 public string PolicyVersion { get; set; } = "EG-DEMO-2026-v1";
 public DateTime At { get; set; }
 public string Actor { get; set; } = "";
 public string Payload { get; set; } = "";
 public string Sha256 { get; set; } = "";
}
public class LeaveTransaction
{
 public int Id { get; set; }
 public int EmployeeId { get; set; }
 public int LeaveTypeId { get; set; }
 public int Year { get; set; }
 public int? RequestId { get; set; }
 public int Delta { get; set; }
 public string Kind { get; set; } = "";
 public string Reason { get; set; } = "";
 public string Actor { get; set; } = "";
 public DateTime At { get; set; }
}
/// <summary>Historical assignment evidence. Existing pre-upgrade history cannot be reconstructed.</summary>
public class EmployeeAssignment
{
 public int Id { get; set; }
 public int EmployeeId { get; set; }
 public DateTime EffectiveFrom { get; set; }
 public DateTime? EffectiveTo { get; set; }
 public int DepartmentId { get; set; }
 public int BranchId { get; set; }
 public int JobTitleId { get; set; }
 public int? ManagerId { get; set; }
 public int? ShiftId { get; set; }
 public decimal BasicSalary { get; set; }
 public string Reason { get; set; } = "";
 public string Actor { get; set; } = "";
 public static EmployeeAssignment Capture(Employee e, DateTime from, string actor, string reason) => new() {EmployeeId=e.Id,EffectiveFrom=from,DepartmentId=e.DepartmentId,BranchId=e.BranchId,JobTitleId=e.JobTitleId,ManagerId=e.ManagerId,ShiftId=e.ShiftId,BasicSalary=e.BasicSalary,Actor=actor,Reason=reason};
}

/// <summary>Append-only attendance observations and changes. Local punch times are retained verbatim.</summary>
public class AttendanceEvidence
{
 public int Id { get; set; }
 public int EmployeeId { get; set; }
 public DateTime Date { get; set; }
 public string Event { get; set; } = "";
 public DateTime At { get; set; }
 public string Actor { get; set; } = "";
 public string Reason { get; set; } = "";
 public DateTime? BeforeIn { get; set; }
 public DateTime? BeforeOut { get; set; }
 public DateTime? AfterIn { get; set; }
 public DateTime? AfterOut { get; set; }
 public int? CorrectionId { get; set; }
 public static AttendanceEvidence Capture(AttendanceRecord r,string action,string actor,DateTime at,string reason="",DateTime? beforeIn=null,DateTime? beforeOut=null,int? correction=null) => new() { EmployeeId=r.EmployeeId,Date=r.Date,Event=action,Actor=actor,At=at,Reason=reason,BeforeIn=beforeIn,BeforeOut=beforeOut,AfterIn=r.CheckIn,AfterOut=r.CheckOut,CorrectionId=correction };
}

/// <summary>Future organizational assignment or status change. Applied history is never rescheduled.</summary>
public class EmploymentChange
{
 public int Id {get;set;}
 public int EmployeeId {get;set;}
 public DateTime EffectiveOn {get;set;}
 public string Kind {get;set;}="Assignment";
 public int DepartmentId {get;set;}
 public int BranchId {get;set;}
 public int JobTitleId {get;set;}
 public int? ManagerId {get;set;}
 public int? ShiftId {get;set;}
 public decimal BasicSalary {get;set;}
 public EmployeeStatus? Status {get;set;}
 public string Reason {get;set;}="";
 public string Actor {get;set;}="";
 public DateTime CreatedAt {get;set;}
 public DateTime? AppliedAt {get;set;}
 public bool Cancelled {get;set;}
}

public class AttendancePeriodLock
{
 public int Id {get;set;}
 public int EmployeeId {get;set;}
 public int Year {get;set;}
 public int Month {get;set;}
 public string Actor {get;set;}="";
 public string Reason {get;set;}="";
 public DateTime At {get;set;}
}

public class EmployeeDependent
{
 public int Id {get;set;}
 public int EmployeeId {get;set;}
 public string Name {get;set;}="";
 public string Relationship {get;set;}="";
 public DateTime? BirthDate {get;set;}
 public string Actor {get;set;}="";
 public DateTime At {get;set;}
}
public class LifecycleTask
{
 public int Id {get;set;}
 public int EmployeeId {get;set;}
 public string Process {get;set;}="Onboarding";
 public string Title {get;set;}="";
 public int? AssignedEmployeeId {get;set;}
 public DateTime DueOn {get;set;}
 public string Status {get;set;}="Open";
 public string CompletionNote {get;set;}="";
 public DateTime? CompletedAt {get;set;}
 public string CompletedBy {get;set;}="";
}
public class EmployeeProbation
{
 public int Id {get;set;}
 public int EmployeeId {get;set;}
 public DateTime EndsOn {get;set;}
 public string Decision {get;set;}="Pending";
 public DateTime? DecisionAt {get;set;}
 public string Reason {get;set;}="";
 public string Actor {get;set;}="";
}
public class HrFieldDefinition
{
 public int Id {get;set;}
 public string Label {get;set;}="";
 public string LabelAr {get;set;}="";
 public string Kind {get;set;}="Text";
 public bool Active {get;set;}=true;
}
public class EmployeeFieldValue
{
 public int Id {get;set;}
 public int EmployeeId {get;set;}
 public int FieldId {get;set;}
 public string Value {get;set;}="";
}
