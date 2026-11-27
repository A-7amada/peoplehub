using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common.Tenancy;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>ADR-0023 stage 1: the isolation audit. Model-wide checks over every tenant-owned table, so a new table that forgets tenancy fails here.</summary>
public class TenantAuditTests
{
    static readonly string[] NotOwned = [nameof(Tenant), nameof(ScheduledJobState), nameof(PlatformUser), nameof(PlatformAuditLog)];
    static AppDbContext Ctx(SqliteConnection c, int tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options, new FixedTenantContext(tenant));
    static async Task<SqliteConnection> Shared()
    {
        var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
        await using var d = Ctx(c, 1); await d.Database.EnsureCreatedAsync(); return c;
    }

    [Fact] public async Task EveryTenantOwnedEntityHasAQueryFilterOnTenantId()
    {
        await using var c = await Shared(); await using var a = Ctx(c, 1);
        var bad = a.Model.GetEntityTypes().Where(t => t.BaseType == null && !NotOwned.Contains(t.ClrType.Name) && t.GetQueryFilter() == null).Select(t => t.ClrType.Name).ToList();
        Assert.Empty(bad);
    }
    /// <summary>Unique indexes without TenantId that were reviewed. Parent-scoped ones are made of ids of tenant-owned rows (ids are database-wide unique, so they cannot collide across tenants); token ones are random secrets.</summary>
    static readonly string[] Reviewed = [
        "ApprovalReminder(", "ApprovalVote(", "Attachment(Token)", "AttendancePeriodLock(", "AttendanceRecord(", "BenefitCancellation(", "CompetencyApplication(",
        "EmployeeCaseFile(Token)", "EmployeeDocument(AttachmentToken)", "EmployeeFieldValue(", "EmployeeGrievanceAppeal(", "EmployeeGrievanceAppealDecision(",
        "EmployeeProbation(", "EmployeeShiftDay(", "EmploymentChange(", "Feedback360(", "LoginSession(TokenHash)", "PasswordReset(Token)", "PayrollAdjustmentEvent(",
        "PayrollBankResult(", "PayrollCalculationSnapshot(", "PayrollLine(", "PerfAppraisal(LegacyReviewId)", "PerfBonus(", "StagedEmployeeRow(", "TrainingCertificate(AttachmentToken)", "TrainingCertificateAlert("];
    [Fact] public async Task EveryUniqueIndexOnATenantOwnedTableIncludesTenantId()
    {
        await using var c = await Shared(); await using var a = Ctx(c, 1);
        var bad = a.Model.GetEntityTypes().Where(t => t.BaseType == null && !NotOwned.Contains(t.ClrType.Name))
            .SelectMany(t => t.GetIndexes().Where(i => i.IsUnique && !i.Properties.Any(p => p.Name == "TenantId")).Select(i => t.ClrType.Name + "(" + string.Join(",", i.Properties.Select(p => p.Name)) + ")")).Where(x => !Reviewed.Any(r => x.StartsWith(r))).ToList();
        Assert.True(bad.Count == 0, string.Join(" | ", bad));
    }
    [Fact] public async Task RelationshipsNeverReachAcrossTenantsInTheModel()
    {
        // Every foreign key between two tenant-owned tables is covered by the same filter on both ends, so a join cannot surface another tenant's row.
        await using var c = await Shared(); await using var a = Ctx(c, 1);
        var bad = a.Model.GetEntityTypes().Where(t => t.BaseType == null && !NotOwned.Contains(t.ClrType.Name))
            .SelectMany(t => t.GetForeignKeys().Where(f => !f.PrincipalEntityType.IsOwned() && NotOwned.Contains(f.PrincipalEntityType.ClrType.Name) && f.PrincipalEntityType.ClrType.Name != nameof(Tenant)).Select(f => t.ClrType.Name + "->" + f.PrincipalEntityType.ClrType.Name)).ToList();
        Assert.Empty(bad);
    }
    [Fact] public async Task TheSameSettingKeyMayExistInTwoTenantsButNotTwiceInOne()
    {
        await using var c = await Shared();
        await using (var a = Ctx(c, 1)) { a.Add(new AppSetting { Key = "k", Value = "1" }); await a.SaveChangesAsync(); }
        await using (var b = Ctx(c, 2)) { b.Add(new AppSetting { Key = "k", Value = "2" }); await b.SaveChangesAsync(); }
        await using (var a = Ctx(c, 1)) { a.Add(new AppSetting { Key = "k", Value = "3" }); await Assert.ThrowsAnyAsync<DbUpdateException>(() => a.SaveChangesAsync()); }
        await using var b2 = Ctx(c, 2);
        Assert.Equal("2", (await b2.Set<AppSetting>().SingleAsync(x => x.Key == "k")).Value);
    }
    [Fact] public async Task ADeleteInOneTenantNeverTouchesAnotherTenantsRows()
    {
        await using var c = await Shared();
        await using (var a = Ctx(c, 1)) { a.Add(new Branch { Name = "A" }); await a.SaveChangesAsync(); }
        await using (var b = Ctx(c, 2)) { b.Add(new Branch { Name = "B" }); await b.SaveChangesAsync(); }
        await using (var b = Ctx(c, 2)) { b.RemoveRange(await b.Set<Branch>().ToListAsync()); await b.SaveChangesAsync(); }
        await using var a2 = Ctx(c, 1);
        Assert.Equal(["A"], await a2.Set<Branch>().Select(x => x.Name).ToListAsync());
    }
    [Fact] public async Task TheSameLoginEmailSignsIntoItsOwnTenantOnly()
    {
        await using var c = await Shared();
        await using (var d = Ctx(c, 1)) { d.Add(new Tenant { Id = 2, Name = "Second", Slug = "second" }); await d.SaveChangesAsync(); }
        await using (var a = Ctx(c, 1)) { a.Add(new AppUser { Email = "same@peoplehub.local", PasswordHash = PeopleHub.Domain.Security.PasswordHasher.Hash("One-pass1!"), IsActive = true }); await a.SaveChangesAsync(); }
        await using (var b = Ctx(c, 2)) { b.Add(new AppUser { Email = "same@peoplehub.local", PasswordHash = PeopleHub.Domain.Security.PasswordHasher.Hash("Two-pass2!"), IsActive = true }); await b.SaveChangesAsync(); }
        var clock = new PeopleHub.Application.Common.AppClock(TimeProvider.System);
        await using var a2 = Ctx(c, 1); await using var b2 = Ctx(c, 2);
        var s1 = new PeopleHub.Application.Modules.Identity.AuthService(a2, clock);
        var s2 = new PeopleHub.Application.Modules.Identity.AuthService(b2, clock);
        Assert.NotNull(await s1.LoginAsync("same@peoplehub.local", "One-pass1!"));
        Assert.Null(await s1.LoginAsync("same@peoplehub.local", "Two-pass2!"));
        Assert.NotNull(await s2.LoginAsync("same@peoplehub.local", "Two-pass2!"));
        Assert.Null(await s2.LoginAsync("same@peoplehub.local", "One-pass1!"));
        await using var a3 = Ctx(c, 1);
        Assert.Equal(1, await a3.Set<AppUser>().CountAsync(u => u.Email == "same@peoplehub.local"));
    }
}
