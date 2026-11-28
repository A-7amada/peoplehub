using System.Text.RegularExpressions;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Operations;

/// <summary>US-PH3-001 integration catalog. Tenant-scoped. Administrators (AdmSettings) register, enable, disable and record credential rotation. Every change is audited. No secret is stored, shown or logged: only a reference label to the approved secrets store.</summary>
public class IntegrationRegistry(IRepository repo, ICurrentUser me, AuditService audit, AppClock clock)
{
 public static readonly string[] AuthMethods = ["None", "ApiKey", "OAuth2", "Basic", "Certificate"];
 private static readonly Regex RefOk = new(@"^[A-Za-z0-9._:/ -]{0,100}$", RegexOptions.Compiled);
 private void Guard() { if (!me.Can(Perm.AdmSettings)) throw new DomainException("err.forbidden"); }

 public Task<List<IntegrationEntry>> ListAsync() { Guard(); return repo.ToListAsync(repo.Query<IntegrationEntry>().OrderBy(x => x.Name)); }

 public Task RegisterAsync(string name, string purpose, string owner, string version, string authMethod, string? credentialRef) => repo.AtomicAsync(async () =>
 {
  Guard();
  name = (name ?? "").Trim(); purpose = (purpose ?? "").Trim(); owner = (owner ?? "").Trim(); version = (version ?? "").Trim(); credentialRef = (credentialRef ?? "").Trim();
  if (name.Length is < 2 or > 100 || purpose.Length is < 3 or > 300 || owner.Length is < 2 or > 100 || version.Length is < 1 or > 40 || !AuthMethods.Contains(authMethod) || !RefOk.IsMatch(credentialRef)) throw new DomainException("err.required");
  if (authMethod != "None" && credentialRef.Length == 0) throw new DomainException("err.required");
  if (await repo.AnyAsync(repo.Query<IntegrationEntry>().Where(x => x.Name == name))) throw new DomainException("err.required");
  repo.Add(new IntegrationEntry { Name = name, Purpose = purpose, OwnerName = owner, SupportedVersion = version, AuthMethod = authMethod, CredentialRef = credentialRef, Actor = me.Email, At = clock.UtcNow });
  audit.Log("Create", "Integration", name, $"Registered (disabled): owner {owner}, auth {authMethod}, version {version}");
  await repo.SaveAsync();
 });

 public Task SetStatusAsync(int id, bool enabled) => repo.AtomicAsync(async () =>
 {
  Guard();
  var e = await repo.FirstOrDefaultAsync(repo.Query<IntegrationEntry>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
  if (enabled && e.AuthMethod != "None" && e.CredentialRotatedAt == null) throw new DomainException("err.required");
  e.Status = enabled ? "Enabled" : "Disabled"; e.Actor = me.Email; e.At = clock.UtcNow;
  audit.Log("Update", "Integration", e.Name, enabled ? "Enabled" : "Disabled");
  await repo.SaveAsync();
 });

 /// <summary>Records that the credential was rotated in the secrets store and points to the new item label. The secret value is never handled here.</summary>
 public Task RecordRotationAsync(int id, string credentialRef) => repo.AtomicAsync(async () =>
 {
  Guard();
  credentialRef = (credentialRef ?? "").Trim();
  if (credentialRef.Length is < 1 or > 100 || !RefOk.IsMatch(credentialRef)) throw new DomainException("err.required");
  var e = await repo.FirstOrDefaultAsync(repo.Query<IntegrationEntry>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
  if (e.AuthMethod == "None") throw new DomainException("err.required");
  e.CredentialRef = credentialRef; e.CredentialRotatedAt = clock.UtcNow; e.Actor = me.Email; e.At = clock.UtcNow;
  audit.Log("Rotate", "Integration", e.Name, "Credential rotation recorded (reference label only, no secret stored)");
  await repo.SaveAsync();
 });
}
