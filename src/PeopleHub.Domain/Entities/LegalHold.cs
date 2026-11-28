namespace PeopleHub.Domain.Entities;

/// <summary>US-PH1-005 legal hold on one employee's records. A held employee is left out of the retention review. Illustrative, not legally validated.</summary>
public class LegalHold
{
 public int Id { get; set; }
 public int EmployeeId { get; set; }
 public string Reason { get; set; } = "";
 public string PlacedBy { get; set; } = "";
 public DateTime PlacedAt { get; set; }
 public string? ReleasedBy { get; set; }
 public DateTime? ReleasedAt { get; set; }
 public string? ReleaseReason { get; set; }
 public bool IsActive => ReleasedAt == null;
}
