using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common.Tenancy;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>ADR-0023 stage 3a: creating and managing customer companies.</summary>
public class TenantProvisioningTests
{
    sealed class Rig : IAsyncDisposable
    {
        public SqliteConnection C = new("Data Source=:memory:");
        public MutableTenantContext Ctx = new();
        public AppDbContext Db = null!;
        public async Task<Rig> Init()
        {
            await C.OpenAsync();
            Db = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(C).Options, Ctx);
            await Db.Database.EnsureCreatedAsync();
            await SeedData.SeedAccessAndWorkflowsAsync(Db);
            return this;
        }
        public TenantAdminService Admin(bool platform) => new(Db, new FixedTenantContext(TenantIds.Default, platform), new TenantProvisioner(Db, Ctx));
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await C.DisposeAsync(); }
    }

    [Fact] public async Task ProvisionedTenantHasItsOwnReferenceDataAndAdmin_AndTenantOneIsUntouched()
    {
        await using var r = await new Rig().Init();
        var before = await r.Db.Set<Role>().CountAsync();
        var id = await r.Admin(true).CreateAsync("Acme Co", "acme", "Boss@Acme.test", "Passw0rdX", "staff@platform.test", "Customer signed contract");
        Assert.True(id > 1);
        r.Ctx.Override = id;
        Assert.Equal(before, await r.Db.Set<Role>().CountAsync());
        Assert.Equal(4, await r.Db.Set<LeaveType>().CountAsync());
        Assert.True(await r.Db.Set<WorkflowDefinition>().AnyAsync());
        var admin = await r.Db.Set<AppUser>().SingleAsync();
        Assert.Equal("boss@acme.test", admin.Email); Assert.Contains("SystemAdmin", admin.RoleList); Assert.Null(admin.EmployeeId);
        Assert.Empty(await r.Db.Set<Employee>().ToListAsync());
        r.Ctx.Override = null;
        Assert.Empty(await r.Db.Set<AppUser>().ToListAsync());
        Assert.Empty(await r.Db.Set<LeaveType>().ToListAsync());
    }

    [Theory] [InlineData("X", "a", "a@b.co", "Passw0rdX")] [InlineData("X", "Bad Slug", "a@b.co", "Passw0rdX")] [InlineData("X", "platform", "a@b.co", "Passw0rdX")]
    [InlineData("X", "okslug", "nope", "Passw0rdX")] [InlineData("X", "okslug", "a@b.co", "short1")] [InlineData("X", "okslug", "a@b.co", "onlyletters")] [InlineData("", "okslug", "a@b.co", "Passw0rdX")]
    public async Task InvalidInputIsRefused_AndNothingIsCreated(string name, string slug, string email, string pw)
    {
        await using var r = await new Rig().Init();
        await Assert.ThrowsAsync<DomainException>(() => r.Admin(true).CreateAsync(name, slug, email, pw, "staff@platform.test", "Reason"));
        Assert.Single(await r.Db.Set<Tenant>().ToListAsync());
    }

    [Fact] public async Task DuplicateSlugRefused_AndFailedProvisioningRollsBack()
    {
        await using var r = await new Rig().Init();
        await r.Admin(true).CreateAsync("A", "acme", "a@a.test", "Passw0rdX", "staff@platform.test", "Customer signed contract");
        var ex = await Assert.ThrowsAsync<DomainException>(() => r.Admin(true).CreateAsync("B", "ACME", "b@b.test", "Passw0rdX", "staff@platform.test", "Customer signed contract"));
        Assert.Equal("err.tenantSlugTaken", ex.Message);
        Assert.Equal(2, await r.Db.Set<Tenant>().CountAsync());
    }

    [Fact] public async Task OnlyPlatformStaffMayManage_AndDefaultTenantIsProtected()
    {
        await using var r = await new Rig().Init();
        await Assert.ThrowsAsync<DomainException>(() => r.Admin(false).CreateAsync("A", "acme", "a@a.test", "Passw0rdX", "staff@platform.test", "Customer signed contract"));
        await Assert.ThrowsAsync<DomainException>(() => r.Admin(false).ListAsync());
        await Assert.ThrowsAsync<DomainException>(() => r.Admin(true).SuspendAsync(1, "staff@platform.test", "Test reason"));
        await Assert.ThrowsAsync<DomainException>(() => r.Admin(true).ArchiveAsync(999, "staff@platform.test", "Test reason"));
    }

    [Fact] public async Task SuspendReactivateArchive()
    {
        await using var r = await new Rig().Init();
        var a = r.Admin(true); var id = await a.CreateAsync("A", "acme", "a@a.test", "Passw0rdX", "staff@platform.test", "Customer signed contract");
        await a.SuspendAsync(id, "staff@platform.test", "Test reason"); Assert.Equal("Suspended", (await a.ListAsync()).Single(t => t.Id == id).Status);
        await a.ReactivateAsync(id, "staff@platform.test", "Test reason"); Assert.Equal("Active", (await a.ListAsync()).Single(t => t.Id == id).Status);
        await a.ArchiveAsync(id, "staff@platform.test", "Test reason"); Assert.Equal("Archived", (await a.ListAsync()).Single(t => t.Id == id).Status);
    }

    [Fact] public async Task EveryLifecycleChangeIsAuditedWithActorTimeAndReason()
    {
        await using var r = await new Rig().Init();
        var a = r.Admin(true); var id = await a.CreateAsync("A", "acme", "a@a.test", "Passw0rdX", "staff@platform.test", "Signed order 123");
        await a.SuspendAsync(id, "staff@platform.test", "Unpaid invoice");
        await a.ReactivateAsync(id, "ops@platform.test", "Paid");
        await a.ArchiveAsync(id, "staff@platform.test", "Contract ended");
        var log = await a.AuditAsync();
        Assert.Equal(new[] { "Archived", "Reactivated", "Suspended", "Created" }, log.Select(x => x.Action).ToArray());
        Assert.All(log, x => { Assert.Equal("acme", x.TenantSlug); Assert.Equal(id, x.CompanyId); Assert.True(x.At > DateTime.UtcNow.AddMinutes(-5)); });
        Assert.Equal("ops@platform.test", log[1].Actor); Assert.Equal("Contract ended", log[0].Reason);
    }

    [Theory] [InlineData("")] [InlineData("  ")] [InlineData("ab")]
    public async Task ChangeWithoutAReasonIsRefused_AndNothingChangesOrIsLogged(string reason)
    {
        await using var r = await new Rig().Init();
        var a = r.Admin(true); var id = await a.CreateAsync("A", "acme", "a@a.test", "Passw0rdX", "staff@platform.test", "Signed order 123");
        await Assert.ThrowsAsync<DomainException>(() => a.SuspendAsync(id, "staff@platform.test", reason));
        await Assert.ThrowsAsync<DomainException>(() => a.CreateAsync("B", "beta", "b@b.test", "Passw0rdX", "staff@platform.test", reason));
        Assert.Equal("Active", (await a.ListAsync()).Single(t => t.Id == id).Status);
        Assert.Single(await a.AuditAsync());
        Assert.Equal(2, (await a.ListAsync()).Count);
    }

    [Fact] public async Task OnlyPlatformStaffCanReadTheAuditTrail()
    {
        await using var r = await new Rig().Init();
        await Assert.ThrowsAsync<DomainException>(() => r.Admin(false).AuditAsync());
    }
}
