using System.Globalization;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Operations;
public record RetentionRule(string Category, int Months);
public record RetentionCandidate(string Category, int Id, string Label, DateTime Since);
/// <summary>US-PH1-005 retention schedule and legal hold. Configuration plus a read-only list of records that passed their retention period. Nothing is deleted or anonymised by this feature: any removal stays a separate, reviewed decision. A period of 0 months means keep. Defaults are illustrative, not legally validated, and need review by a labour lawyer.</summary>
public class RetentionReview(IRepository repo, ICurrentUser me, AuditService audit, AppClock clock)
{
 public static readonly string[] Categories = ["Employees", "Candidates"];
 private void Guard() { if (!me.Can(Perm.AdmSettings)) throw new DomainException("err.forbidden"); }
 private static string Key(string c) => "Retention." + c + ".Months";
 public async Task<List<RetentionRule>> RulesAsync()
 {
  Guard();
  var rows = await repo.ToListAsync(repo.Query<AppSetting>().Where(x => x.Key.StartsWith("Retention.")));
  return Categories.Select(c => new RetentionRule(c, int.TryParse(rows.FirstOrDefault(x => x.Key == Key(c))?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var m) && m is >= 0 and <= 1200 ? m : 0)).ToList();
 }
 public Task SetRuleAsync(string category, int months) => repo.AtomicAsync(async () =>
 {
  Guard();
  if (!Categories.Contains(category) || months is < 0 or > 1200) throw new DomainException("err.required");
  var key = Key(category);
  var row = await repo.FirstOrDefaultAsync(repo.Query<AppSetting>().Where(x => x.Key == key));
  if (row == null) { row = new() { Key = key }; repo.Add(row); }
  row.Value = months.ToString(CultureInfo.InvariantCulture);
  audit.Log("RetentionRule", "Setting", key, $"{category}: {(months == 0 ? "keep (no review)" : months + " months")}; review list only, nothing is deleted; illustrative, not legally validated");
  await repo.SaveAsync();
 });
 public async Task<List<LegalHold>> HoldsAsync() { Guard(); return await repo.ToListAsync(repo.Query<LegalHold>().OrderByDescending(x => x.Id)); }
 public Task PlaceHoldAsync(int employeeId, string reason) => repo.AtomicAsync(async () =>
 {
  Guard();
  if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) throw new DomainException("err.reasonRequired");
  if (await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x => x.Id == employeeId)) == null) throw new DomainException("err.notFound");
  if ((await repo.ToListAsync(repo.Query<LegalHold>().Where(x => x.EmployeeId == employeeId))).Any(x => x.IsActive)) throw new DomainException("err.forbidden");
  repo.Add(new LegalHold { EmployeeId = employeeId, Reason = reason.Trim(), PlacedBy = me.Email, PlacedAt = clock.UtcNow });
  audit.Log("LegalHoldPlaced", "Employee", employeeId, "Excluded from the retention review until released");
  await repo.SaveAsync();
 });
 public Task ReleaseHoldAsync(int id, string reason) => repo.AtomicAsync(async () =>
 {
  Guard();
  if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) throw new DomainException("err.reasonRequired");
  var h = await repo.FirstOrDefaultAsync(repo.Query<LegalHold>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
  if (!h.IsActive) throw new DomainException("err.forbidden");
  h.ReleasedBy = me.Email; h.ReleasedAt = clock.UtcNow; h.ReleaseReason = reason.Trim();
  audit.Log("LegalHoldReleased", "Employee", h.EmployeeId, "Back in the retention review");
  await repo.SaveAsync();
 });
 /// <summary>Records past their period, minus held employees. Read-only.</summary>
 public async Task<List<RetentionCandidate>> ReviewListAsync()
 {
  var rules = await RulesAsync(); var now = clock.UtcNow; var found = new List<RetentionCandidate>();
  var held = (await repo.ToListAsync(repo.Query<LegalHold>())).Where(x => x.IsActive).Select(x => x.EmployeeId).ToHashSet();
  var emp = rules.First(r => r.Category == "Employees").Months;
  if (emp > 0)
  {
   var ended = new[] { EmployeeStatus.Resigned, EmployeeStatus.Terminated, EmployeeStatus.Retired };
   var employees = (await repo.ToListAsync(repo.Query<Employee>())).Where(e => ended.Contains(e.Status) && !held.Contains(e.Id)).ToList();
   var history = await repo.ToListAsync(repo.Query<StatusHistory>());
   foreach (var e in employees)
   {
    var at = history.Where(h => h.EmployeeId == e.Id && ended.Contains(h.To)).Select(h => (DateTime?)h.At).OrderByDescending(x => x).FirstOrDefault();
    if (at != null && at.Value.AddMonths(emp) <= now) found.Add(new("Employees", e.Id, string.IsNullOrWhiteSpace(e.Code) ? "#" + e.Id : e.Code, at.Value));
   }
  }
  var cand = rules.First(r => r.Category == "Candidates").Months;
  if (cand > 0)
   foreach (var c in (await repo.ToListAsync(repo.Query<Candidate>().Where(x => x.Stage == CandidateStage.Rejected))).Where(c => c.CreatedAt.AddMonths(cand) <= now))
    found.Add(new("Candidates", c.Id, "#" + c.Id, c.CreatedAt));
  return found.OrderBy(x => x.Since).ToList();
 }
}
