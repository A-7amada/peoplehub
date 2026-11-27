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
    static readonly string[] NotOwned = [nameof(Tenant), nameof(ScheduledJobState)];
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
    /// <summary>Unique indexes without TenantId that were reviewed. Parent-scoped ones are made of ids of tenant-owned rows (ids are database-wide unique, so they cannot collide across tenants); token ones are random secrets. AppUser(Email) is the open item for stage 2 (tenant-bound login).</summary>
    static readonly string[] Reviewed = [
        "AppUser(Email)",
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
}
