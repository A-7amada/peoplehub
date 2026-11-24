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
