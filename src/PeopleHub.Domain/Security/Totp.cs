using System.Security.Cryptography;
namespace PeopleHub.Domain.Security;

/// <summary>RFC 6238 time-based one-time passwords (Google Authenticator compatible).</summary>
public static class Totp
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    public static string NewSecret() { var b = RandomNumberGenerator.GetBytes(20); return Base32(b); }
    public static string Base32(byte[] data)
    {
        var sb = new System.Text.StringBuilder(); int buf = 0, bits = 0;
        foreach (var x in data) { buf = (buf << 8) | x; bits += 8; while (bits >= 5) { sb.Append(Alphabet[(buf >> (bits - 5)) & 31]); bits -= 5; } }
        if (bits > 0) sb.Append(Alphabet[(buf << (5 - bits)) & 31]);
        return sb.ToString();
    }
    public static byte[] FromBase32(string s)
    {
        var o = new List<byte>(); int buf = 0, bits = 0;
        foreach (var c in s.Trim().Replace(" ", "").ToUpperInvariant()) { var v = Alphabet.IndexOf(c); if (v < 0) continue; buf = (buf << 5) | v; bits += 5; if (bits >= 8) { o.Add((byte)((buf >> (bits - 8)) & 255)); bits -= 8; } }
        return o.ToArray();
    }
    public static string Code(string secret, long unixSeconds, int digits = 6)
    {
        var counter = unixSeconds / 30; var msg = BitConverter.GetBytes(counter); if (BitConverter.IsLittleEndian) Array.Reverse(msg);
        var h = HMACSHA1.HashData(FromBase32(secret), msg); var off = h[^1] & 0xf;
        var bin = ((h[off] & 0x7f) << 24) | (h[off + 1] << 16) | (h[off + 2] << 8) | h[off + 3];
        return (bin % (int)Math.Pow(10, digits)).ToString().PadLeft(digits, '0');
    }
    /// <summary>The 30-second step that matches the code (current step, one before or one after), or null.</summary>
    public static long? MatchStep(string secret, string? code, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(code)) return null; code = code.Trim().Replace(" ", "");
        var t = new DateTimeOffset(utcNow).ToUnixTimeSeconds();
        for (var w = -1; w <= 1; w++) if (Code(secret, t + w * 30) == code) return (t + w * 30) / 30;
        return null;
    }
    public static bool Verify(string secret, string? code, DateTime utcNow) => MatchStep(secret, code, utcNow) != null;
}
