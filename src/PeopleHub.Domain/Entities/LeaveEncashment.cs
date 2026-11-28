namespace PeopleHub.Domain.Entities;

/// <summary>US-PH2-004 request to pay out unused leave days. Records days only: the payout amount, rate and payroll linkage are owner and specialist decisions that are not built. Illustrative, not legally validated.</summary>
public class LeaveEncashment
{
 public int Id { get; set; }
 public int EmployeeId { get; set; }
 public int LeaveTypeId { get; set; }
 public int Year { get; set; }
 public decimal Days { get; set; }
 /// <summary>Requested, Approved, Rejected or Cancelled.</summary>
 public string Status { get; set; } = "Requested";
 public string RequestedBy { get; set; } = "";
 public DateTime RequestedAt { get; set; }
 public string? DecidedBy { get; set; }
 public DateTime? DecidedAt { get; set; }
 public string? DecisionNote { get; set; }
}
