using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Domain.Entities;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Security;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Tests;

public class HardeningTests
{
    static AppDbContext Db(string file) => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={file}").Options);
    static string FileName() => Path.Combine(Path.GetTempPath(), $"ph-test-{Guid.NewGuid():N}.db");
    [Fact] public async Task MigrationsAndRestartPreserveRecords()
    {
        var f=FileName();
        await using(var db=Db(f)) { await db.Database.MigrateAsync(); db.Add(new AppSetting {Key="persistence-proof",Value="kept"});await db.SaveAsync(); }
        await using(var db=Db(f)) {await db.Database.MigrateAsync();Assert.Equal("kept",(await db.Set<AppSetting>().SingleAsync()).Value);}
    }
    [Fact] public async Task StaleWritesFailInsteadOfOverwriting()
    {
        var f=FileName();await using(var db=Db(f)){await db.Database.MigrateAsync();db.Add(new PerfBonus {AppraisalId=991,Amount=100});await db.SaveAsync();}
        await using var a=Db(f);await using var b=Db(f);var x=await a.Set<PerfBonus>().SingleAsync();var y=await b.Set<PerfBonus>().SingleAsync();
        x.Amount=200;await a.SaveAsync();y.Amount=300;await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>b.SaveAsync());
    }
    [Fact] public async Task DuplicateAwardsRejectedAtDatabaseLevel()
    {
        await using var db=Db(FileName());await db.Database.MigrateAsync();db.Add(new PerfBonus {AppraisalId=991});await db.SaveAsync();
        db.Add(new PerfBonus {AppraisalId=991});await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveAsync());
    }
    [Fact] public async Task TransactionRollsBackEarlySavedChanges()
    {
        await using var db=Db(FileName());await db.Database.MigrateAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>db.AtomicAsync(async()=>{db.Add(new AppSetting{Key="rollback",Value="bad"});await db.SaveAsync();throw new InvalidOperationException();}));
        Assert.False(await db.Set<AppSetting>().AnyAsync());
    }
    [Fact] public async Task ResetIsHashedSingleUseAndInvalidatesPriorToken()
    {
        await using var db=Db(FileName());await db.Database.MigrateAsync();
        db.Add(new AppUser{Email="test@example.local",PasswordHash=PasswordHasher.Hash("Before123!")});await db.SaveAsync();
        var clock=new AppClock(TimeProvider.System);var me=new TestMe();var mail=new EmailService(db,clock);var account=new AccountService(db,me,clock,mail,new AuditService(db,me,clock));
        var first=await account.RequestResetAsync("test@example.local","https://demo.example");Assert.NotNull(first);Assert.DoesNotContain(first!,await db.Set<PasswordReset>().Select(r=>r.Token).ToListAsync());
        var second=await account.RequestResetAsync("test@example.local","https://demo.example");Assert.False(await account.TokenValidAsync(first!));Assert.True(await account.TokenValidAsync(second!));
        var stamp=(await db.Set<AppUser>().SingleAsync()).SecurityStamp;Assert.True(await account.ResetAsync(second!,"After123!"));Assert.NotEqual(stamp,(await db.Set<AppUser>().SingleAsync()).SecurityStamp);Assert.False(await account.ResetAsync(second!,"Again123!"));
    }
    [Fact] public async Task PostgresMigrationSmoke()
    {
        var connection=Environment.GetEnvironmentVariable("TEST_POSTGRES");if(connection==null)return;
        await using var db=new PgDbContext(new DbContextOptionsBuilder<PgDbContext>().UseNpgsql(connection).Options);
        await db.Database.MigrateAsync();await SeedData.InitializeAsync(db,"Test123!","Demo123!",new DateTime(2026,11,24));Assert.True(await db.Set<Employee>().AnyAsync());Assert.True(await db.Set<AppUser>().AnyAsync());
        var archive=await PeopleHub.Infrastructure.Operations.PostgresArchive.ExportAsync(db);Assert.True(archive.Length>1000);
        var cs=new Npgsql.NpgsqlConnectionStringBuilder(connection);var restoreName="ph_restore_"+Guid.NewGuid().ToString("N");
        await using(var admin=new Npgsql.NpgsqlConnection(connection)) {await admin.OpenAsync();await using var create=new Npgsql.NpgsqlCommand($"CREATE DATABASE {restoreName}",admin);await create.ExecuteNonQueryAsync();}
        try {
            cs.Database=restoreName;await using var target=new PgDbContext(new DbContextOptionsBuilder<PgDbContext>().UseNpgsql(cs.ConnectionString).Options);
            await PeopleHub.Infrastructure.Operations.PostgresArchive.RestoreEmptyAsync(target,archive);
            Assert.Equal(await db.Set<Employee>().CountAsync(),await target.Set<Employee>().CountAsync());
            Assert.Equal(await db.Set<AppUser>().CountAsync(),await target.Set<AppUser>().CountAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(()=>PeopleHub.Infrastructure.Operations.PostgresArchive.RestoreEmptyAsync(target,archive));
        } finally {
            Npgsql.NpgsqlConnection.ClearAllPools();await using var admin=new Npgsql.NpgsqlConnection(connection);await admin.OpenAsync();await using var drop=new Npgsql.NpgsqlCommand($"DROP DATABASE {restoreName} WITH (FORCE)",admin);await drop.ExecuteNonQueryAsync();
        }
    }
    class TestMe:ICurrentUser {public bool IsAuthenticated=>true;public string Email=>"test@example.local";public int? EmployeeId=>null;public int UserId=>1;public bool IsInRole(string r)=>true;public bool Can(string p)=>true;}
}
