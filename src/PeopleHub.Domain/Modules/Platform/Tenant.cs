namespace PeopleHub.Domain.Entities;
/// <summary>A customer company on the platform (ADR-0001). Id 1 is the default tenant that owns all data
/// created before multi-tenancy. Customer rows carry a TenantId column (shadow property) pointing here.</summary>
public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc);
}
