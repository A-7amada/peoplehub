using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Application.Modules.Talent;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH3-012 skills inventory and gap view. Information only; assessment data restricted.</summary>
public class SkillsInventoryTests
{
    class V(int emp, bool talent, bool team = false, bool company = false) : ICurrentUser { public string Email => $"u{emp}@example.local"; public int? EmployeeId => emp; public int UserId => emp; public bool IsAuthenticated => true; public bool IsInRole(string r) => false; public bool Can(string p) => p == Perm.TalentManage ? talent : p == Perm.ScopeTeam ? team : p == Perm.ScopeCompany && company; }
    static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
    static SkillsInventory S(AppDbContext d, V u) { var c = new AppClock(TimeProvider.System); return new(d, u, new AccessScope(d, u), new AuditService(d, u, c), c); }

    [Fact] public async Task TaxonomyNeedsTalentManageAndRejectsDuplicates()
    {
        await using var d = await Db();
        await Assert.ThrowsAsync<DomainException>(() => S(d, new(1, false)).AddSkillAsync("Fictional SQL", null, "Data"));
        await S(d, new(1, true)).AddSkillAsync("Fictional SQL", null, "Data");
        await Assert.ThrowsAsync<DomainException>(() => S(d, new(1, true)).AddSkillAsync("Fictional SQL", null, "Data"));
        Assert.Single(await S(d, new(1, true)).SkillsAsync());
    }
    [Fact] public async Task ProfileSkillStaysUnconfirmedUntilAnotherPersonApprovesAndCountsInGaps()
    {
        await using var d = await Db();
        var hr = new V(1, true, company: true);await S(d, hr).AddSkillAsync("Fictional SQL", null, "Data");
        var skill = (await S(d, hr).SkillsAsync()).Single();var dept = (await d.Set<Employee>().FindAsync(6))!.DepartmentId;
        await S(d, hr).AddRequirementAsync(skill.Id, dept, 3, 1);
        var me6 = new V(6, false);await S(d, me6).RecordAsync(6, skill.Id, 4, "Profile", "self");
        var row = await d.Set<EmployeeSkill>().SingleAsync();Assert.False(row.Confirmed);
        Assert.Equal(1, (await S(d, hr).GapsAsync()).Single().Missing);
        await Assert.ThrowsAsync<DomainException>(() => S(d, me6).ConfirmAsync(row.Id));
        await S(d, hr).ConfirmAsync(row.Id);
        var gap = (await S(d, hr).GapsAsync()).Single();Assert.Equal(1, gap.Have);Assert.Equal(0, gap.Missing);
    }
    [Fact] public async Task EmployeeCannotRecordAssessmentOrOthersAndAssessmentDetailIsRestricted()
    {
        await using var d = await Db();
        var hr = new V(1, true, company: true);await S(d, hr).AddSkillAsync("Fictional SQL", null, "Data");var skill = (await S(d, hr).SkillsAsync()).Single();
        var me6 = new V(6, false);
        await Assert.ThrowsAsync<DomainException>(() => S(d, me6).RecordAsync(6, skill.Id, 5, "Assessment", "x"));
        await Assert.ThrowsAsync<DomainException>(() => S(d, me6).RecordAsync(7, skill.Id, 5, "Profile", "x"));
        await Assert.ThrowsAsync<DomainException>(() => S(d, hr).RecordAsync(6, skill.Id, 6, "Assessment", "x"));
        await S(d, hr).RecordAsync(6, skill.Id, 5, "Assessment", "Fictional panel note");
        var own = (await S(d, me6).ForEmployeeAsync(6)).Single();Assert.True(own.Restricted);Assert.Equal(0, own.Row.Level);Assert.Equal("", own.Row.Evidence);
        var full = (await S(d, hr).ForEmployeeAsync(6)).Single();Assert.False(full.Restricted);Assert.Equal(5, full.Row.Level);Assert.True(full.Row.Confirmed);
    }
    [Fact] public async Task GapViewNeedsTeamOrCompanyScope()
    {
        await using var d = await Db();
        await Assert.ThrowsAsync<DomainException>(() => S(d, new(6, false)).GapsAsync());
        Assert.NotNull(await S(d, new(1, false, company: true)).GapsAsync());
    }
}
