using Microsoft.Extensions.Caching.Memory;
using PeopleHub.Application.Common.Tenancy;
using PeopleHub.Infrastructure.Caching;
using PeopleHub.Infrastructure.Files;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>Contract tests for the Phase 0 platform interfaces (ADR-0001, 0003, 0013, 0014).</summary>
public class PlatformContractTests
{
    [Fact] public void TenantKeyedCacheKeepsTenantsApart()
    {
        var shared = new InProcessCache(new MemoryCache(new MemoryCacheOptions()));
        var a = new TenantKeyedCache(shared, new FixedTenantContext(1));
        var b = new TenantKeyedCache(shared, new FixedTenantContext(2));
        a.Set("settings", "tenant-one", TimeSpan.FromMinutes(1));
        Assert.True(a.TryGet<string>("settings", out var va)); Assert.Equal("tenant-one", va);
        Assert.False(b.TryGet<string>("settings", out _));
        b.Set("settings", "tenant-two", TimeSpan.FromMinutes(1));
        a.Remove("settings");
        Assert.False(a.TryGet<string>("settings", out _)); Assert.True(b.TryGet<string>("settings", out var vb)); Assert.Equal("tenant-two", vb);
    }
    [Fact] public async Task CacheAsideRunsFactoryOnce()
    {
        var c = new InProcessCache(new MemoryCache(new MemoryCacheOptions())); var n = 0;
        Assert.Equal(7, await c.GetOrCreateAsync("k", () => { n++; return Task.FromResult(7); }, TimeSpan.FromMinutes(1)));
        Assert.Equal(7, await c.GetOrCreateAsync("k", () => { n++; return Task.FromResult(8); }, TimeSpan.FromMinutes(1)));
        Assert.Equal(1, n);
    }
    [Fact] public async Task FileStoreRoundTripsAndDeletes()
    {
        var f = new InMemoryFileStore(); var r = await f.SaveAsync("a.pdf", "application/pdf", [1, 2, 3]);
        Assert.Equal(3, r.Size); var got = await f.OpenAsync(r.Key); Assert.Equal([1, 2, 3], got!.Data);
        Assert.True(await f.DeleteAsync(r.Key)); Assert.Null(await f.OpenAsync(r.Key)); Assert.False(await f.DeleteAsync(r.Key));
    }
    [Fact] public void DefaultTenantIsStable() { Assert.Equal(1, TenantIds.Default); Assert.False(new FixedTenantContext().IsPlatform); }
}
