using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common.Tenancy;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>Tenant isolation (ADR-0001 to ADR-0003): two tenants share one database; each context sees,
/// stamps and writes only its own rows.</summary>
public class TenantIsolationTests
{
    static AppDbContext Ctx(SqliteConnection c, int tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options, new FixedTenantContext(tenant));
    static async Task<SqliteConnection> Shared()
    {
        var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
        await using var d = Ctx(c, 1); await d.Database.EnsureCreatedAsync(); return c;
    }

    [Fact] public async Task EachTenantSeesOnlyItsOwnRows()
    {
        await using var c = await Shared();
        await using (var a = Ctx(c, 1)) { a.Add(new Branch { Name = "Cairo HQ" }); await a.SaveChangesAsync(); }
        await using (var b = Ctx(c, 2)) { b.Add(new Branch { Name = "Alex office" }); b.Add(new Branch { Name = "Giza" }); await b.SaveChangesAsync(); }
        await using var a1 = Ctx(c, 1); await using var b1 = Ctx(c, 2);
        Assert.Equal(["Cairo HQ"], await a1.Set<Branch>().Select(x => x.Name).ToListAsync());
        Assert.Equal(["Alex office", "Giza"], await b1.Set<Branch>().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync());
        Assert.Equal(3, await a1.Set<Branch>().IgnoreQueryFilters().CountAsync());
    }
    [Fact] public async Task NewRowsAreStampedWithTheCurrentTenant()
    {
        await using var c = await Shared();
        await using var b = Ctx(c, 2); var br = new Branch { Name = "Stamped" }; b.Add(br); await b.SaveChangesAsync();
        Assert.Equal(2, b.Entry(br).Property<int>("TenantId").CurrentValue);
    }
    [Fact] public async Task ATenantCannotReadOrChangeAnotherTenantsRow()
    {
        await using var c = await Shared();
        int id; await using (var a = Ctx(c, 1)) { var br = new Branch { Name = "Private" }; a.Add(br); await a.SaveChangesAsync(); id = br.Id; }
        await using var b = Ctx(c, 2);
        Assert.Null(await b.Set<Branch>().FirstOrDefaultAsync(x => x.Id == id));
        var foreign = new Branch { Id = id, Name = "Hijack" }; b.Attach(foreign).State = EntityState.Modified;
        await Assert.ThrowsAsync<InvalidOperationException>(() => b.SaveChangesAsync());
    }
    [Fact] public async Task TenantIdCannotBeEdited()
    {
        await using var c = await Shared();
        await using var a = Ctx(c, 1); var br = new Branch { Name = "X" }; a.Add(br); await a.SaveChangesAsync();
        a.Entry(br).Property<int>("TenantId").CurrentValue = 2;
        await Assert.ThrowsAsync<InvalidOperationException>(() => a.SaveChangesAsync());
    }
    [Fact] public async Task DefaultTenantRowExistsAndEveryCustomerTableIsTenantOwned()
    {
        await using var c = await Shared(); await using var a = Ctx(c, 1);
        Assert.Equal("demo", (await a.Set<Tenant>().SingleAsync()).Slug);
        var missing = a.Model.GetEntityTypes().Where(t => t.BaseType == null && t.ClrType.Name is not (nameof(Tenant) or nameof(ScheduledJobState)) && t.FindProperty("TenantId") == null).Select(t => t.ClrType.Name).ToList();
        Assert.Empty(missing);
    }
}
