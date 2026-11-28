using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Operations;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>US-PH3-001 integration registry. Metadata only: no secret is stored, enabling is intent only.</summary>
public class IntegrationRegistryTests
{
 class User(int emp = 1, bool grant = true) : ICurrentUser { public string Email => "integ@example.local"; public int? EmployeeId => emp; public int UserId => emp; public bool IsAuthenticated => true; public bool IsInRole(string r) => false; public bool Can(string p) => p == Perm.AdmSettings && grant; }
 class Time : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero); }
 static async Task<AppDbContext> Db() { var d = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options); await d.Database.OpenConnectionAsync(); await d.Database.MigrateAsync(); await SeedData.InitializeAsync(d, "Admin123!", "Demo123!", new DateTime(2026, 11, 24)); return d; }
 static IntegrationRegistry R(AppDbContext d, User? u = null) { u ??= new(); var c = new AppClock(new Time()); return new(d, u, new AuditService(d, u, c), c); }

 [Fact] public async Task RegisterNeedsAdminAndCompleteMetadataAndStartsDisabled()
 {
  await using var d = await Db();
  await Assert.ThrowsAsync<DomainException>(() => R(d, new(1, false)).RegisterAsync("Fictional HRIS sync", "Sync", "IT", "v1", "ApiKey", "vault/item"));
  await Assert.ThrowsAsync<DomainException>(() => R(d).RegisterAsync("Fictional HRIS sync", "Sync", "IT", "v1", "Magic", ""));
  await Assert.ThrowsAsync<DomainException>(() => R(d).RegisterAsync("Fictional HRIS sync", "Sync", "IT", "v1", "ApiKey", ""));
  await Assert.ThrowsAsync<DomainException>(() => R(d).RegisterAsync("Fictional HRIS sync", "Sync", "IT", "v1", "ApiKey", "sk-live;DROP<>"));
  await R(d).RegisterAsync("Fictional HRIS sync", "Sync", "IT", "v1", "ApiKey", "vault/hris-key");
  await Assert.ThrowsAsync<DomainException>(() => R(d).RegisterAsync("Fictional HRIS sync", "Sync", "IT", "v1", "None", ""));
  var e = await d.Set<IntegrationEntry>().SingleAsync(); Assert.Equal("Disabled", e.Status); Assert.Equal("vault/hris-key", e.CredentialRef);
 }
 [Fact] public async Task EnableNeedsARecordedRotationForCredentialedEntriesAndIsAudited()
 {
  await using var d = await Db();
  await R(d).RegisterAsync("Fictional payroll export", "Export", "Finance", "2", "OAuth2", "vault/pay");
  await R(d).RegisterAsync("Fictional open feed", "Public feed", "HR", "1", "None", "");
  var ids = await d.Set<IntegrationEntry>().OrderBy(x => x.Name).Select(x => x.Id).ToListAsync(); var open = ids[0]; var pay = ids[1];
  await R(d).SetStatusAsync(open, true); Assert.Equal("Enabled", (await d.Set<IntegrationEntry>().FindAsync(open))!.Status);
  await Assert.ThrowsAsync<DomainException>(() => R(d).SetStatusAsync(pay, true));
  await Assert.ThrowsAsync<DomainException>(() => R(d, new(1, false)).RecordRotationAsync(pay, "vault/pay2"));
  await Assert.ThrowsAsync<DomainException>(() => R(d).RecordRotationAsync(open, "vault/x"));
  await R(d).RecordRotationAsync(pay, "vault/pay2");
  await R(d).SetStatusAsync(pay, true); await R(d).SetStatusAsync(pay, false);
  var p = (await d.Set<IntegrationEntry>().FindAsync(pay))!; Assert.Equal("Disabled", p.Status); Assert.Equal("vault/pay2", p.CredentialRef); Assert.NotNull(p.CredentialRotatedAt);
  Assert.True(await d.Set<AuditLog>().CountAsync(a => a.Entity == "Integration") >= 6);
 }
 [Fact] public async Task ListIsAdminOnly()
 {
  await using var d = await Db();
  await Assert.ThrowsAsync<DomainException>(() => R(d, new(1, false)).ListAsync());
  Assert.Empty(await R(d).ListAsync());
 }
}
