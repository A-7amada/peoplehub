using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common.Tenancy;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Security;
namespace PeopleHub.Infrastructure.Persistence;

/// <summary>Creates a customer tenant (ADR-0023 stage 3). No demo people are seeded: only the reference data a company needs to start, and one administrator.
/// One context and one transaction: the ambient tenant is switched to the new tenant after its row exists, so filters and stamping apply to it. All or nothing.</summary>
public sealed class TenantProvisioner(AppDbContext db, ITenantOverride tenantSwitch) : ITenantProvisioner
{
    public async Task<int> ProvisionAsync(string name, string slug, string adminEmail, string adminPassword, CancellationToken ct = default)
    {
        var before = tenantSwitch.Override;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var tenant = new Tenant { Name = name, Slug = slug, Status = "Active", CreatedAt = DateTime.UtcNow };
            db.Add(tenant); await db.SaveChangesAsync(ct);
            tenantSwitch.Override = tenant.Id;
            await SeedData.SeedAccessAndWorkflowsAsync(db);
            db.AddRange(
                new LeaveType { Code = "ANN", Name = "Annual leave", NameAr = "إجازة سنوية", DefaultDays = 21, MaxCarryOver = 7 },
                new LeaveType { Code = "SICK", Name = "Sick leave", NameAr = "إجازة مرضية", DefaultDays = 15, RequiresDocument = true, AllowBackdated = true },
                new LeaveType { Code = "CAS", Name = "Casual leave", NameAr = "إجازة عارضة", DefaultDays = 6, AllowBackdated = true },
                new LeaveType { Code = "UNP", Name = "Unpaid leave", NameAr = "إجازة بدون أجر", DefaultDays = 0, IsPaid = false, DeductsBalance = false });
            db.Add(new AppUser { Email = adminEmail, RoleList = $"{Roles.SystemAdmin},{Roles.HRManager}", PasswordHash = PasswordHasher.Hash(adminPassword), IsActive = true });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return tenant.Id;
        }
        finally { tenantSwitch.Override = before; }
    }
}
