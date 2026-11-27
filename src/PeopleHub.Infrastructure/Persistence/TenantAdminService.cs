using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common.Tenancy;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Infrastructure.Persistence;

/// <summary>Platform-staff operations on customer companies (ADR-0023 stage 3): list, create, suspend, reactivate, archive.
/// Every call is guarded on <see cref="ITenantContext.IsPlatform"/>. Suspended and archived companies cannot sign in and lose live sessions.</summary>
public sealed class TenantAdminService(AppDbContext db, ITenantContext tenant, ITenantProvisioner provisioner)
{
    public static readonly string[] Reserved = ["platform", "admin", "api", "www", "static", "health", "account", "login"];
    static readonly Regex SlugRx = new("^[a-z0-9][a-z0-9-]{1,30}$", RegexOptions.Compiled);
    static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    void Guard() { if (!tenant.IsPlatform) throw new DomainException("err.platformOnly"); }

    public async Task<List<Tenant>> ListAsync() { Guard(); return await db.Set<Tenant>().AsNoTracking().OrderBy(t => t.Id).ToListAsync(); }

    public async Task<int> CreateAsync(string name, string slug, string adminEmail, string adminPassword)
    {
        Guard();
        name = (name ?? "").Trim(); slug = (slug ?? "").Trim().ToLowerInvariant(); adminEmail = (adminEmail ?? "").Trim().ToLowerInvariant();
        if (name.Length is 0 or > 120) throw new DomainException("err.tenantName");
        if (!SlugRx.IsMatch(slug)) throw new DomainException("err.tenantSlug");
        if (Reserved.Contains(slug) || await db.Set<Tenant>().AnyAsync(t => t.Slug == slug)) throw new DomainException("err.tenantSlugTaken");
        if (!EmailRx.IsMatch(adminEmail) || adminEmail.Length > 200) throw new DomainException("err.tenantEmail");
        if (adminPassword is null || adminPassword.Length < 8 || !adminPassword.Any(char.IsLetter) || !adminPassword.Any(char.IsDigit)) throw new DomainException("err.tenantPassword");
        return await provisioner.ProvisionAsync(name, slug, adminEmail, adminPassword);
    }

    public Task SuspendAsync(int id) => SetAsync(id, "Suspended");
    public Task ReactivateAsync(int id) => SetAsync(id, "Active");
    public Task ArchiveAsync(int id) => SetAsync(id, "Archived");
    async Task SetAsync(int id, string status)
    {
        Guard();
        if (id == TenantIds.Default && status != "Active") throw new DomainException("err.tenantProtected");
        var t = await db.Set<Tenant>().FirstOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("err.tenantNotFound");
        t.Status = status; await db.SaveChangesAsync();
    }
}
