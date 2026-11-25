using System.Collections.Concurrent;
using PeopleHub.Application.Common.Files;
namespace PeopleHub.Infrastructure.Files;

/// <summary>Strategy implementation of IFileStore for tests and local tooling. The database-backed store
/// arrives with the tenancy migration (it needs a tenant-stamped table, ADR-0002 and ADR-0014).</summary>
public sealed class InMemoryFileStore : IFileStore
{
    readonly ConcurrentDictionary<string, StoredFile> files = new();
    public Task<StoredFileRef> SaveAsync(string fileName, string contentType, byte[] data, CancellationToken ct = default)
    {
        var key = Guid.NewGuid().ToString("N");
        files[key] = new StoredFile(key, fileName, contentType, data);
        return Task.FromResult(new StoredFileRef(key, fileName, contentType, data.LongLength));
    }
    public Task<StoredFile?> OpenAsync(string key, CancellationToken ct = default) => Task.FromResult(files.TryGetValue(key, out var f) ? f : null);
    public Task<bool> DeleteAsync(string key, CancellationToken ct = default) => Task.FromResult(files.TryRemove(key, out _));
}
