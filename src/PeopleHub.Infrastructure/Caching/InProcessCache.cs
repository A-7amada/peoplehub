using Microsoft.Extensions.Caching.Memory;
using PeopleHub.Application.Common.Caching;
using PeopleHub.Application.Common.Tenancy;
namespace PeopleHub.Infrastructure.Caching;

/// <summary>Strategy implementation of ICache for a single instance (ADR-0013).</summary>
public sealed class InProcessCache(IMemoryCache memory) : ICache
{
    public bool TryGet<T>(string key, out T? value) { if (memory.TryGetValue(key, out var o) && o is T t) { value = t; return true; } value = default; return false; }
    public void Set<T>(string key, T value, TimeSpan ttl) => memory.Set(key, value, ttl);
    public void Remove(string key) => memory.Remove(key);
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        if (TryGet<T>(key, out var hit) && hit is not null) return hit;
        var made = await factory(); Set(key, made, ttl); return made;
    }
}

/// <summary>Decorator pattern (ADR-0003): wraps any ICache and prefixes every key with the current tenant,
/// so two tenants can never read each other's cached values.</summary>
public sealed class TenantKeyedCache(ICache inner, ITenantContext tenant) : ICache
{
    string K(string key) => $"t{tenant.TenantId}:{key}";
    public bool TryGet<T>(string key, out T? value) => inner.TryGet(K(key), out value);
    public void Set<T>(string key, T value, TimeSpan ttl) => inner.Set(K(key), value, ttl);
    public void Remove(string key) => inner.Remove(K(key));
    public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) => inner.GetOrCreateAsync(K(key), factory, ttl);
}
