namespace PeopleHub.Application.Common.Tenancy;

/// <summary>Lets code that works across tenants (sign-in, provisioning) point the ambient tenant somewhere else for a while.
/// The web tenant context implements it; tests use <see cref="MutableTenantContext"/>.</summary>
public interface ITenantOverride { int? Override { get; set; } }

public sealed class MutableTenantContext(int tenantId = TenantIds.Default) : ITenantContext, ITenantOverride
{
    public int? Override { get; set; }
    public int TenantId => Override ?? tenantId;
    public bool IsPlatform => false;
}
