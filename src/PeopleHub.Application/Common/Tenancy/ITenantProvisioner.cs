namespace PeopleHub.Application.Common.Tenancy;

/// <summary>Port (ADR-0023 stage 3): creates a customer tenant with its reference data and first administrator.
/// Implemented in Infrastructure because it needs a context bound to the new tenant.</summary>
public interface ITenantProvisioner
{
    /// <summary>Creates the tenant row, seeds roles, permissions, approval cycles and leave types, and adds the first admin. Returns the new tenant id. Everything or nothing.</summary>
    Task<int> ProvisionAsync(string name, string slug, string adminEmail, string adminPassword, CancellationToken ct = default);
}
