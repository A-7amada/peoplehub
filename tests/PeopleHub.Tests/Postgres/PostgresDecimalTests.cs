using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests.Postgres;

/// <summary>Runs only when TEST_POSTGRES points to a disposable database (CI service container).
/// Guards ADR-0015: money is exact numeric on PostgreSQL, never double precision.</summary>
public class PostgresDecimalTests
{
    [PostgresFact]
    public async Task MoneyColumnsAreExactNumeric()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_POSTGRES");
        if (connection == null) return;
        var cs = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        var name = "ph_dec_" + Guid.NewGuid().ToString("N"); 
        await using (var admin = new Npgsql.NpgsqlConnection(connection)) { await admin.OpenAsync(); await using var c = new Npgsql.NpgsqlCommand($"CREATE DATABASE {name}", admin); await c.ExecuteNonQueryAsync(); }
        try
        {
            cs.Database = name;
            await using var db = new PgDbContext(new DbContextOptionsBuilder<PgDbContext>().UseNpgsql(cs.ConnectionString).Options);
            await db.Database.MigrateAsync();
            await using var con = new Npgsql.NpgsqlConnection(cs.ConnectionString); await con.OpenAsync();
            async Task<long> Scalar(string sql) { await using var cmd = new Npgsql.NpgsqlCommand(sql, con); return Convert.ToInt64(await cmd.ExecuteScalarAsync()); }
            Assert.Equal(0, await Scalar("select count(*) from information_schema.columns where table_schema='public' and data_type='double precision'"));
            Assert.Equal(1, await Scalar("select count(*) from information_schema.columns where table_schema='public' and table_name='Employee' and column_name='BasicSalary' and numeric_precision=18 and numeric_scale=2"));
            Assert.Equal(1, await Scalar("select count(*) from information_schema.columns where table_schema='public' and table_name='OvertimeRequest' and column_name='Multiplier' and numeric_precision=18 and numeric_scale=4"));
            // exact arithmetic: 0.1 + 0.2 must equal 0.3 in a money column
            await using var q = new Npgsql.NpgsqlCommand("select (0.10::numeric(18,2) + 0.20::numeric(18,2)) = 0.30::numeric(18,2)", con);
            Assert.True((bool)(await q.ExecuteScalarAsync())!);
        }
        finally
        {
            Npgsql.NpgsqlConnection.ClearAllPools();
            await using var admin = new Npgsql.NpgsqlConnection(connection); await admin.OpenAsync();
            await using var d = new Npgsql.NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", admin); await d.ExecuteNonQueryAsync();
        }
    }
}
