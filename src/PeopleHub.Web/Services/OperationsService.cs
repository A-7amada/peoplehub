using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using PeopleHub.Infrastructure.Persistence;
namespace PeopleHub.Web.Services;

public class OperationsService(AppDbContext db, IConfiguration config)
{
    public string Provider => db.Database.IsNpgsql() ? "PostgreSQL" : "SQLite";
    public bool PersistenceConfigured => db.Database.IsNpgsql() || config["PERSISTENT_STORAGE"] == "true";
    public async Task<byte[]> ExportAsync()
    {
        if (db.Database.IsNpgsql()) return await PeopleHub.Infrastructure.Operations.PostgresArchive.ExportAsync(db);
        var temp = Path.Combine(Path.GetTempPath(), $"peoplehub-{Guid.NewGuid():N}.db");
        try {
            await db.Database.OpenConnectionAsync();
            using var target = new SqliteConnection($"Data Source={temp}"); await target.OpenAsync();
            ((SqliteConnection)db.Database.GetDbConnection()).BackupDatabase(target);
            await target.CloseAsync();
            return await File.ReadAllBytesAsync(temp);
        } finally { await db.Database.CloseConnectionAsync(); SqliteConnection.ClearAllPools(); if(File.Exists(temp)) File.Delete(temp); }
    }
    public async Task BackupAsync()
    {
        var dir = config["BACKUP_PATH"]; if (string.IsNullOrWhiteSpace(dir) || db.Database.IsNpgsql()) return;
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"peoplehub-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db");
        await File.WriteAllBytesAsync(path, await ExportAsync());
        foreach (var old in Directory.GetFiles(dir,"peoplehub-*.db").OrderDescending().Skip(7)) File.Delete(old);
    }
}
public class BackupWorker(IServiceScopeFactory scopes, ILogger<BackupWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested) {
            try { using var scope=scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<OperationsService>().BackupAsync(); }
            catch(Exception ex) { log.LogError(ex,"Database backup failed"); }
            await Task.Delay(TimeSpan.FromHours(24),ct);
        }
    }
}
public class MailWorker(IServiceScopeFactory scopes, ILogger<MailWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested) {
            try { using var scope=scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<PeopleHub.Application.Services.EmailService>().FlushAsync(); }
            catch(Exception ex) { log.LogWarning(ex,"Mail delivery deferred"); }
            await Task.Delay(TimeSpan.FromMinutes(1),ct);
        }
    }
}
