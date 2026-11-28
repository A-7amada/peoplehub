namespace PeopleHub.Domain.Entities;

/// <summary>US-PH2-015 internal claim record against a recorded enrolment. Record only: nothing is submitted to an insurer, paid or deducted.</summary>
public class BenefitClaim
{
 public int Id { get; set; }
 public int EnrollmentId { get; set; }
 public int EmployeeId { get; set; }
 public DateTime ServiceDate { get; set; }
 public decimal Amount { get; set; }
 public string Currency { get; set; } = "";
 public string Description { get; set; } = "";
 /// <summary>Recorded, Reviewed or Closed. Moves forward only.</summary>
 public string Status { get; set; } = "Recorded";
 public string Outcome { get; set; } = "";
 public string Actor { get; set; } = "";
 public DateTime At { get; set; }
}
