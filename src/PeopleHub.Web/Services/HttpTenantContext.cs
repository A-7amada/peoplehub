using PeopleHub.Application.Common.Tenancy;
namespace PeopleHub.Web.Services;

/// <summary>Ambient Context for web requests (ADR-0003): reads the tenant from the signed-in user's claim.
/// Until tenant claims are issued every session resolves to the default tenant.</summary>
public sealed class HttpTenantContext(IHttpContextAccessor http) : ITenantContext
{
    public int TenantId => int.TryParse(http.HttpContext?.User.FindFirst("tenant_id")?.Value, out var t) ? t : TenantIds.Default;
    public bool IsPlatform => http.HttpContext?.User.FindFirst("platform")?.Value == "1";
}
