namespace PeopleHub.Domain.Entities;
/// <summary>Platform staff (ADR-0004): signs in separately from customer users and belongs to no company. Cannot read customer data; it only manages company lifecycle.</summary>
public class PlatformUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; } = new DateTime(2026, 11, 27, 0, 0, 0, DateTimeKind.Utc);
}

/// <summary>Who changed a customer company's lifecycle, when and why (US-PH1-001). Belongs to no company; only platform staff can read it.</summary>
public class PlatformAuditLog
{
    public int Id { get; set; }
    public DateTime At { get; set; }
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public int CompanyId { get; set; }
    public string TenantSlug { get; set; } = "";
    public string Reason { get; set; } = "";
}
