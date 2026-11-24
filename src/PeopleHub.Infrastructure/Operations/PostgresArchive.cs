using System.IO.Compression;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PeopleHub.Infrastructure.Persistence;
namespace PeopleHub.Infrastructure.Operations;

/// <summary>Data archive for this exact EF schema. Not a replacement for pg_dump or point-in-time recovery.</summary>
public static class PostgresArchive
{
    static string Q(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";
    public static async Task<byte[]> ExportAsync(AppDbContext db)
    {
        var conn = (NpgsqlConnection)db.Database.GetDbConnection(); await db.Database.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output,ZipArchiveMode.Create,true))
        {
            var manifest=zip.CreateEntry("manifest.txt"); await using(var w=new StreamWriter(manifest.Open())) await w.WriteAsync("PeopleHub data archive v1. Restore only into an EMPTY database with the same migrations. Contains private authentication data. Encrypt at rest.");
            foreach(var table in db.Model.GetEntityTypes().Select(e=>e.GetTableName()!).Distinct().Order())
            {
                var entry=zip.CreateEntry(table+".copy",CompressionLevel.Optimal);
                await using var w=new StreamWriter(entry.Open(),new UTF8Encoding(false));
                using var reader=await conn.BeginTextExportAsync($"COPY {Q(table)} TO STDOUT");
                await w.WriteAsync(await reader.ReadToEndAsync());
            }
        }
        await tx.CommitAsync();await db.Database.CloseConnectionAsync();return output.ToArray();
    }
    public static async Task RestoreEmptyAsync(AppDbContext db, byte[] bytes)
    {
        await db.Database.MigrateAsync();await db.Database.OpenConnectionAsync();var conn=(NpgsqlConnection)db.Database.GetDbConnection();
        await using var tx=await conn.BeginTransactionAsync();
        var tables=db.Model.GetEntityTypes().Select(e=>e.GetTableName()!).Distinct().Order().ToArray();
        foreach(var table in tables) { using var c=new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {Q(table)})",conn,tx);if((bool)(await c.ExecuteScalarAsync())!)throw new InvalidOperationException("Restore refused: target is not empty"); }
        // EF migrations define FKs; archive table ordering must follow dependencies.
        using(var c=new NpgsqlCommand("SET CONSTRAINTS ALL DEFERRED",conn,tx))await c.ExecuteNonQueryAsync();
        var remaining=db.Model.GetEntityTypes().ToList();var ordered=new List<string>();
        while(remaining.Count>0) {
            var ready=remaining.Where(e=>e.GetForeignKeys().All(f=>f.PrincipalEntityType==e || ordered.Contains(f.PrincipalEntityType.GetTableName()!))).ToList();
            if(ready.Count==0)throw new InvalidOperationException("Cyclic foreign keys require pg_restore");
            foreach(var e in ready){ordered.Add(e.GetTableName()!);remaining.Remove(e);}
        }
        using var input=new MemoryStream(bytes);using var zip=new ZipArchive(input);
        foreach(var table in ordered.Distinct()) {
            var entry=zip.GetEntry(table+".copy")??throw new InvalidOperationException("Incomplete archive");
            using var r=new StreamReader(entry.Open());using(var writer=await conn.BeginTextImportAsync($"COPY {Q(table)} FROM STDIN"))await writer.WriteAsync(await r.ReadToEndAsync());
            // Every mapped entity has an integer identity primary key.
            using var seq=new NpgsqlCommand($"SELECT setval(pg_get_serial_sequence('{Q(table)}','Id'),COALESCE(MAX(\"Id\"),1),MAX(\"Id\") IS NOT NULL) FROM {Q(table)}",conn,tx);await seq.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();await db.Database.CloseConnectionAsync();
    }
}
