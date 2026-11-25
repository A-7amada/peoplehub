namespace PeopleHub.Application.Common.Tenancy;

/// <summary>Ambient Context pattern (ADR-0001, ADR-0003): who the current work belongs to.
/// Resolved per web request from the session and per background job from the job's tenant.
/// Application code never reads a tenant from a controller argument.</summary>
public interface ITenantContext
{
    /// <summary>Customer tenant the current request or job runs for.</summary>
    int TenantId { get; }
    /// <summary>True only for platform staff acting on the platform tenant (ADR-0004).</summary>
    bool IsPlatform { get; }
}

public static class TenantIds
{
    /// <summary>Tenant that owns all data created before multi-tenancy (backfill target, ADR-0002).</summary>
    public const int Default = 1;
}

/// <summary>Null Object / fixed context for jobs, seeding and tests, where the tenant is decided by the caller.</summary>
public sealed class FixedTenantContext(int tenantId = TenantIds.Default, bool isPlatform = false) : ITenantContext
{
    public int TenantId { get; } = tenantId;
    public bool IsPlatform { get; } = isPlatform;
}
