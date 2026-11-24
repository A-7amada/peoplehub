using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace PeopleHub.Infrastructure.Persistence;
public class PgDbContext(DbContextOptions<PgDbContext> options) : AppDbContext(options);
public class SqliteFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=design.db").Options);
}
public class PgFactory : IDesignTimeDbContextFactory<PgDbContext>
{
    public PgDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<PgDbContext>().UseNpgsql("Host=localhost;Database=design;Username=design;Password=unused").Options);
}
