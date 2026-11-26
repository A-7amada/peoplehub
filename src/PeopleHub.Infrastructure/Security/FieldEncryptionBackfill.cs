using System.Data;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Infrastructure.Security;

/// <summary>ADR-0021: encrypts legacy plaintext in the protected columns, and re-encrypts values written under a previous key.
/// Idempotent. Uses raw SQL on purpose so tenant filters and the append-only guards do not apply to a storage-only change.</summary>
public static class FieldEncryptionBackfill
{
    public sealed record Result(bool Enabled, int Encrypted, int Remaining, string? KeyId);
    public static Result Last { get; private set; } = new(false, 0, 0, null);

    static readonly (Type Entity, string Property)[] Columns =
    {
        (typeof(AppUser), nameof(AppUser.TotpSecret)),
        (typeof(Employee), nameof(Employee.NationalId)),
        (typeof(Employee), nameof(Employee.Iban)),
        (typeof(PayrollLine), nameof(PayrollLine.IbanSnapshot)),
    };

    public static async Task<Result> RunAsync(DbContext db, FieldCipher cipher, CancellationToken ct = default)
    {
        if (!cipher.Enabled) return Last = new(false, 0, await CountPlainAsync(db, ct), null);
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync(ct);
        var done = 0;
        try
        {
            foreach (var (entity, prop) in Columns)
            {
                var et = db.Model.FindEntityType(entity)!;
                var table = et.GetTableName()!; var col = et.FindProperty(prop)!.GetColumnName();
                var aad = $"{entity.Name}.{prop}";
                var rows = new List<(long Id, string Value)>();
                await using (var q = conn.CreateCommand())
                {
                    q.CommandText = $"SELECT \"Id\", \"{col}\" FROM \"{table}\" WHERE \"{col}\" IS NOT NULL AND \"{col}\" <> ''";
                    await using var r = await q.ExecuteReaderAsync(ct);
                    while (await r.ReadAsync(ct)) rows.Add((Convert.ToInt64(r.GetValue(0)), r.GetString(1)));
                }
                foreach (var (id, value) in rows)
                {
                    if (FieldCipher.IsEncrypted(value) && cipher.IsCurrentKey(value)) continue;
                    var plain = cipher.Decrypt(value, aad);
                    var enc = cipher.Encrypt(plain, aad);
                    await using var u = conn.CreateCommand();
                    u.CommandText = $"UPDATE \"{table}\" SET \"{col}\" = @v WHERE \"Id\" = @id";
                    var pv = u.CreateParameter(); pv.ParameterName = "@v"; pv.Value = enc; u.Parameters.Add(pv);
                    var pi = u.CreateParameter(); pi.ParameterName = "@id"; pi.Value = id; u.Parameters.Add(pi);
                    await u.ExecuteNonQueryAsync(ct);
                    done++;
                }
            }
        }
        finally { if (wasClosed) await conn.CloseAsync(); }
        return Last = new(true, done, await CountPlainAsync(db, ct), cipher.CurrentKeyId);
    }

    static async Task<int> CountPlainAsync(DbContext db, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync(ct);
        try
        {
            var n = 0;
            foreach (var (entity, prop) in Columns)
            {
                var et = db.Model.FindEntityType(entity)!;
                await using var q = conn.CreateCommand();
                var col = et.FindProperty(prop)!.GetColumnName();
                q.CommandText = $"SELECT COUNT(*) FROM \"{et.GetTableName()}\" WHERE \"{col}\" IS NOT NULL AND \"{col}\" <> '' AND \"{col}\" NOT LIKE 'enc:v1:%'";
                n += Convert.ToInt32(await q.ExecuteScalarAsync(ct));
            }
            return n;
        }
        finally { if (wasClosed) await conn.CloseAsync(); }
    }
}
