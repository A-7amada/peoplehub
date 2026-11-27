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
