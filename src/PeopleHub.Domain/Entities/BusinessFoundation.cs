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
 public decimal Delta { get; set; }
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
 public DateTime? ReturnOn {get;set;}
 public int? ParentChangeId {get;set;}
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

public class EmployeeDocument
{
 public int Id {get;set;}
 public int EmployeeId {get;set;}
 public string Title {get;set;}="";
 public string AttachmentToken {get;set;}="";
 public DateTime? ExpiresOn {get;set;}
 public string UploadedBy {get;set;}="";
 public DateTime At {get;set;}
 public DateTime? LastAlertDate {get;set;}
}

public class EmployeeShiftDay
{
 public int Id {get;set;}
 public int EmployeeId {get;set;}
 public DateTime Date {get;set;}
 public int ShiftId {get;set;}
 public string Reason {get;set;}="";
 public string Actor {get;set;}="";
 public DateTime At {get;set;}
}

public class CompanyAsset
{
 public int Id {get;set;}
 public string Tag {get;set;}="";
 public string Name {get;set;}="";
 public string Serial {get;set;}="";
 public string Status {get;set;}="Available";
 public int? AssignedEmployeeId {get;set;}
 public DateTime? DueOn {get;set;}
 public string Condition {get;set;}="";
}
public class AssetMovement
{
 public int Id {get;set;}
 public int AssetId {get;set;}
 public int? EmployeeId {get;set;}
 public string Kind {get;set;}="";
 public string Condition {get;set;}="";
 public string Evidence {get;set;}="";
 public string Actor {get;set;}="";
 public DateTime At {get;set;}
}

public class PerformanceResponse
{
 public int Id {get;set;}
 public int AppraisalId {get;set;}
 public string Kind {get;set;}="";
 public int? AppealId {get;set;}
 public string Note {get;set;}="";
 public string Actor {get;set;}="";
 public DateTime At {get;set;}
}

public class LoginSession
{
 public DateTime? ElevatedUntil {get;set;}
 public int Id {get;set;}
 public int UserId {get;set;}
 public string TokenHash {get;set;}="";
 public DateTime StartedAt {get;set;}
 public DateTime ExpiresAt {get;set;}
 public string Agent {get;set;}="";
 public string Ip {get;set;}="";
 public DateTime? RevokedAt {get;set;}
}

public class PipMilestone
{
 public int Id {get;set;}
 public int AppraisalId {get;set;}
 public string Title {get;set;}="";
 public string SuccessMeasure {get;set;}="";
 public DateTime DueOn {get;set;}
 public string Status {get;set;}="Open";
 public string CreatedBy {get;set;}="";
 public DateTime CreatedAt {get;set;}
}
public class PipProgress
{
 public int Id {get;set;}
 public int MilestoneId {get;set;}
 public string Kind {get;set;}="Update";
 public int? Progress {get;set;}
 public string Note {get;set;}="";
 public string Actor {get;set;}="";
 public DateTime At {get;set;}
}

/// <summary>Append-only certificate evidence. Does not certify issuer authenticity.</summary>
public class TrainingCertificate
{
 public int Id {get;set;} public int EnrollmentId {get;set;} public int EmployeeId {get;set;}
 public string Issuer {get;set;}="";public string Reference {get;set;}="";public DateTime IssuedOn {get;set;} public DateTime? ExpiresOn {get;set;}
 public string AttachmentToken {get;set;}="";public string Actor {get;set;}="";public DateTime At {get;set;}
}
public class TrainingOutcome
{
 public int Id {get;set;}public int EnrollmentId {get;set;}public int EmployeeId {get;set;}public string Kind {get;set;}="Participant";
 public string Note {get;set;}="";public string Actor {get;set;}="";public DateTime At {get;set;}
}

/// <summary>Internal interview plan, not a calendar invitation.</summary>
public class CandidateInterview
{
 public int Id {get;set;}public int CandidateId {get;set;}public int InterviewerEmployeeId {get;set;}public DateTime StartsAt {get;set;}public int Minutes {get;set;}public string Place {get;set;}="";public string Note {get;set;}="";public string Status {get;set;}="Scheduled";public string Actor {get;set;}="";public DateTime CreatedAt {get;set;}
}
public class RecruitmentEvent
{
 public int Id {get;set;}public int CandidateId {get;set;}public int? InterviewId {get;set;}public string Kind {get;set;}="";public string Note {get;set;}="";public string Actor {get;set;}="";public DateTime At {get;set;}
}

/// <summary>Immutable internal offer proposal. Not a sent letter or contract.</summary>
public class CandidateOffer
{
 public int Id {get;set;}public int CandidateId {get;set;}public string RoleTitle {get;set;}="";public decimal MonthlyBase {get;set;}public string Currency {get;set;}="";public DateTime StartOn {get;set;}public DateTime ValidUntil {get;set;}public string Terms {get;set;}="";public int CreatedByUserId {get;set;}public string Actor {get;set;}="";public DateTime At {get;set;}
}
public class CandidateOfferEvent
{
 public int Id {get;set;}public int OfferId {get;set;}public string Kind {get;set;}="";public string Note {get;set;}="";public int ActorUserId {get;set;}public string Actor {get;set;}="";public DateTime At {get;set;}
}

/// <summary>Daily in-app certificate expiry alert ledger, no email delivery.</summary>
public class TrainingCertificateAlert
{
 public int Id {get;set;}public int CertificateId {get;set;}public DateTime OnDate {get;set;}public DateTime At {get;set;}
}

/// <summary>Immutable observed competency framework copied into an appraisal.</summary>
public class CompetencyApplication
{
 public int Id {get;set;}public int AppraisalId {get;set;}public int TemplateId {get;set;}public int? JobGrade {get;set;}public string TemplateName {get;set;}="";public string Content {get;set;}="";public string Sha256 {get;set;}="";public string Actor {get;set;}="";public DateTime At {get;set;}
}

/// <summary>Recorded benefits plan, no insurer purchase or payroll instruction.</summary>
public class BenefitPlan
{
 public int Id {get;set;}public string Name {get;set;}="";public string Provider {get;set;}="";public string Eligibility {get;set;}="";public string Coverage {get;set;}="";public bool DependentsAllowed {get;set;}public decimal EmployerMonthly {get;set;}public decimal EmployeeMonthly {get;set;}public string Currency {get;set;}="";public string Actor {get;set;}="";public DateTime At {get;set;}
}
public class BenefitEnrollment
{
 public int Id {get;set;}public int PlanId {get;set;}public int EmployeeId {get;set;}public int? DependentId {get;set;}public string CoveredName {get;set;}="";public string CoveredRelationship {get;set;}="";public DateTime StartsOn {get;set;}public DateTime EndsOn {get;set;}public string EligibilityEvidence {get;set;}="";public string CostCenter {get;set;}="";public string Actor {get;set;}="";public DateTime At {get;set;}
}
public class BenefitCancellation
{
 public int Id {get;set;}public int EnrollmentId {get;set;}public DateTime EffectiveOn {get;set;}public string Reason {get;set;}="";public string Actor {get;set;}="";public DateTime At {get;set;}
}

/// <summary>Confidential internal case record, never a delivered disciplinary notice.</summary>
public class EmployeeCase
{
 public int Id {get;set;}public int EmployeeId {get;set;}public string Kind {get;set;}="Grievance";public string Title {get;set;}="";public string Statement {get;set;}="";public int CreatedByUserId {get;set;}public string Actor {get;set;}="";public DateTime At {get;set;}public string ChangeStamp {get;set;}="";
}
public class EmployeeCaseEvent
{
 public int Id {get;set;}public int CaseId {get;set;}public string Kind {get;set;}="Investigation";public string Note {get;set;}="";public int ActorUserId {get;set;}public string Actor {get;set;}="";public DateTime At {get;set;}
}

/// <summary>Append-only manually recorded bank result, never initiates or proves a transfer.</summary>
public class PayrollBankResult
{
 public int Id {get;set;}public int LineId {get;set;}public int RunId {get;set;}public int Version {get;set;}public string Outcome {get;set;}="";public decimal Amount {get;set;}public string Reference {get;set;}="";public string Note {get;set;}="";public string Actor {get;set;}="";public DateTime At {get;set;}
}

/// <summary>Daily recipient/stage reminder ledger. No approval or route change.</summary>
public class ApprovalReminder
{
 public int Id {get;set;}public string Kind {get;set;}="";public int RequestId {get;set;}public int StageIndex {get;set;}public int UserId {get;set;}public DateTime OnDate {get;set;}public DateTime At {get;set;}
}

/// <summary>Immutable explicit grade compensation template. No automatic salary rule.</summary>
public class SalaryStructure
{
 public int Id {get;set;}public string Name {get;set;}="";public int JobGrade {get;set;}public EmploymentType? EmploymentCategory {get;set;}public decimal Basic {get;set;}public string ComponentsJson {get;set;}="{}";public string Reason {get;set;}="";public string Actor {get;set;}="";public DateTime At {get;set;}
}
public class SalaryStructureApplication
{
 public int Id {get;set;}public int EmployeeId {get;set;}public int StructureId {get;set;}public string BeforeJson {get;set;}="";public string AfterJson {get;set;}="";public string Sha256 {get;set;}="";public string Reason {get;set;}="";public string Actor {get;set;}="";public DateTime At {get;set;}
}
