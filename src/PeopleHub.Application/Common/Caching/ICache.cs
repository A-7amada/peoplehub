namespace PeopleHub.Application.Common.Caching;

/// <summary>Strategy pattern (ADR-0013): application code depends on this contract, the host picks the
/// implementation (in-process today, a shared cache when ARC-005 is approved). Keys are plain names; the
/// tenant prefix is added by the TenantKeyedCache decorator so a feature cannot forget it.</summary>
public interface ICache
{
    bool TryGet<T>(string key, out T? value);
    void Set<T>(string key, T value, TimeSpan ttl);
    void Remove(string key);
    /// <summary>Cache-aside helper: returns the cached value or runs the factory once and stores the result.</summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl);
}
