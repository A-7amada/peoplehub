using System.Security.Cryptography;
using System.Text;
namespace PeopleHub.Infrastructure.Security;

/// <summary>Application-level authenticated encryption for sensitive columns (ADR-0021). AES-256-GCM, random nonce per value,
/// column name bound as associated data. Values without the "enc:" prefix are legacy plaintext and are read as they are.</summary>
public sealed class FieldCipher
{
    const string Prefix = "enc:v1:";
    readonly (string Id, byte[] Key) current;
    readonly Dictionary<string, byte[]> keys = new();
    public bool Enabled { get; }
    public string? CurrentKeyId => Enabled ? current.Id : null;

    /// <summary>The cipher the running app uses. Set once at startup from the environment.</summary>
    public static FieldCipher Default { get; private set; } = new(null, null);
    public static void ConfigureDefault(string? key, string? previous) => Default = new(key, previous);

    public FieldCipher(string? key, string? previousKeys)
    {
        if (string.IsNullOrWhiteSpace(key)) { current = ("", []); return; }
        current = Derive(key.Trim()); keys[current.Id] = current.Key; Enabled = true;
        foreach (var k in (previousKeys ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) { var d = Derive(k); keys[d.Id] = d.Key; }
    }

    static (string Id, byte[] Key) Derive(string secret)
    {
        var key = HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(secret), 32, null, Encoding.UTF8.GetBytes("PeopleHub field encryption v1"));
        return (Convert.ToHexString(SHA256.HashData(key))[..8].ToLowerInvariant(), key);
    }

    public static bool IsEncrypted(string? stored) => stored != null && stored.StartsWith(Prefix, StringComparison.Ordinal);

    public bool IsCurrentKey(string? stored) => Enabled && IsEncrypted(stored) && stored!.Substring(Prefix.Length).StartsWith(current.Id + ":", StringComparison.Ordinal);

    public string? Encrypt(string? plain, string column)
    {
        if (!Enabled || string.IsNullOrEmpty(plain)) return plain;
        var nonce = RandomNumberGenerator.GetBytes(12); var data = Encoding.UTF8.GetBytes(plain);
        var ct = new byte[data.Length]; var tag = new byte[16];
        using (var g = new AesGcm(current.Key, 16)) g.Encrypt(nonce, data, ct, tag, Encoding.UTF8.GetBytes(column));
        return Prefix + current.Id + ":" + Convert.ToBase64String([.. nonce, .. ct, .. tag]);
    }

    public string? Decrypt(string? stored, string column)
    {
        if (!IsEncrypted(stored)) return stored;
        var parts = stored!.Substring(Prefix.Length).Split(':', 2);
        if (parts.Length != 2 || !keys.TryGetValue(parts[0], out var key)) throw new CryptographicException("Encrypted field cannot be read: the key it was written with is not configured.");
        var raw = Convert.FromBase64String(parts[1]);
        if (raw.Length < 28) throw new CryptographicException("Encrypted field is malformed.");
        var nonce = raw[..12]; var tag = raw[^16..]; var ct = raw[12..^16]; var data = new byte[ct.Length];
        using (var g = new AesGcm(key, 16)) g.Decrypt(nonce, ct, tag, data, Encoding.UTF8.GetBytes(column));
        return Encoding.UTF8.GetString(data);
    }
}
