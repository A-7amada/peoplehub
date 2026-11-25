namespace PeopleHub.Application.Common.Files;

/// <summary>Strategy pattern (ADR-0014): file bytes sit behind this contract. Today the demo keeps files in the
/// database (free tier); an object store can replace it without changing callers. Keys are opaque tokens.</summary>
public interface IFileStore
{
    Task<StoredFileRef> SaveAsync(string fileName, string contentType, byte[] data, CancellationToken ct = default);
    Task<StoredFile?> OpenAsync(string key, CancellationToken ct = default);
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
}

public sealed record StoredFileRef(string Key, string FileName, string ContentType, long Size);
public sealed record StoredFile(string Key, string FileName, string ContentType, byte[] Data);
