using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace PeopleHub.Infrastructure.Persistence;
/// <summary>PostgreSQL context. Strategy pattern (ADR-0015): money is exact numeric(18,2); quantities and
/// factors (hours, multiplier, score) are numeric(18,4) so no precision is lost to rounding.</summary>
public class PgDbContext(DbContextOptions<PgDbContext> options, PeopleHub.Application.Common.Tenancy.ITenantContext? tenant = null) : AppDbContext(options, tenant)
{
    static readonly HashSet<string> Quantities = ["Multiplier", "OvertimeHours", "Hours", "FinalScore"];
    protected override void ConfigureDecimalStorage(ModelConfigurationBuilder c)
    {
        c.Properties<decimal>().HavePrecision(18, 2);
        c.Properties<decimal?>().HavePrecision(18, 2);
    }
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        foreach (var p in b.Model.GetEntityTypes().SelectMany(e => e.GetProperties())
                     .Where(p => (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)) && Quantities.Contains(p.Name)))
            { p.SetPrecision(18); p.SetScale(4); }
    }
}
public class SqliteFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=design.db").Options);
}
public class PgFactory : IDesignTimeDbContextFactory<PgDbContext>
{
    public PgDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<PgDbContext>().UseNpgsql("Host=localhost;Database=design;Username=design;Password=unused").Options);
}
