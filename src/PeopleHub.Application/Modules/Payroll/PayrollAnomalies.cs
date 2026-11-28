using System.Globalization;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Payroll;
/// <summary>US-PH3-015 anomaly thresholds. A value of 0 switches that rule off. Defaults are illustrative, not legally validated.</summary>
public record AnomalyThresholds(decimal NetChangePercent = 30m, int LateMinutes = 240, int AbsentDays = 3, decimal OvertimeHours = 40m);
public record AnomalyFinding(string Rule, int EmployeeId, string Code, string Name, string Detail);
/// <summary>Rule-based review flags for one payroll run. Read-only: it never changes a run, a line or a payment. A flag is a prompt to look, not a finding of error.</summary>
public class PayrollAnomalies(IRepository repo, ICurrentUser me, AuditService audit)
{
 static readonly string[] Keys = ["Anomaly.NetChangePercent", "Anomaly.LateMinutes", "Anomaly.AbsentDays", "Anomaly.OvertimeHours"];
 private void NeedView() { if (!me.Can(Perm.PayView)) throw new DomainException("err.forbidden"); }
 public async Task<AnomalyThresholds> ThresholdsAsync()
 {
  NeedView();
  var rows = await repo.ToListAsync(repo.Query<AppSetting>().Where(x => x.Key.StartsWith("Anomaly.")));
  decimal? V(string k) => decimal.TryParse(rows.FirstOrDefault(x => x.Key == k)?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) && v >= 0 && v <= 100000 ? v : null;
  var d = new AnomalyThresholds();
  return new(V(Keys[0]) ?? d.NetChangePercent, (int?)V(Keys[1]) ?? d.LateMinutes, (int?)V(Keys[2]) ?? d.AbsentDays, V(Keys[3]) ?? d.OvertimeHours);
 }
 public Task SetThresholdsAsync(decimal netChangePercent, int lateMinutes, int absentDays, decimal overtimeHours) => repo.AtomicAsync(async () =>
 {
  if (!me.Can(Perm.AdmSettings)) throw new DomainException("err.forbidden");
  if (netChangePercent is < 0 or > 1000 || lateMinutes is < 0 or > 44640 || absentDays is < 0 or > 31 || overtimeHours is < 0 or > 744) throw new DomainException("err.required");
  var values = new[] { netChangePercent.ToString(CultureInfo.InvariantCulture), lateMinutes.ToString(CultureInfo.InvariantCulture), absentDays.ToString(CultureInfo.InvariantCulture), overtimeHours.ToString(CultureInfo.InvariantCulture) };
  for (var i = 0; i < Keys.Length; i++)
  {
   var key = Keys[i];
   var row = await repo.FirstOrDefaultAsync(repo.Query<AppSetting>().Where(x => x.Key == key));
   if (row == null) { row = new() { Key = key }; repo.Add(row); }
   row.Value = values[i];
  }
  audit.Log("AnomalyThresholds", "Setting", "Anomaly", $"net change {netChangePercent}%, late {lateMinutes} min, absent {absentDays} d, overtime {overtimeHours} h (0 = rule off); illustrative defaults, not legally validated");
  await repo.SaveAsync();
 });
 public async Task<List<AnomalyFinding>> ScanAsync(int runId)
 {
  NeedView();
  var t = await ThresholdsAsync();
  var run = await repo.FirstOrDefaultAsync(repo.Query<PayrollRun>().Where(x => x.Id == runId)) ?? throw new DomainException("err.notFound");
  var lines = await repo.ToListAsync(repo.Query<PayrollLine>().Where(x => x.RunId == runId));
  var prev = (await repo.ToListAsync(repo.Query<PayrollRun>())).Where(x => x.Id != runId && x.Year * 12 + x.Month < run.Year * 12 + run.Month).OrderByDescending(x => x.Year * 12 + x.Month).ThenByDescending(x => x.Id).FirstOrDefault();
  var prevLines = prev == null ? [] : await repo.ToListAsync(repo.Query<PayrollLine>().Where(x => x.RunId == prev.Id));
  var found = new List<AnomalyFinding>();
  string N(PayrollLine l) => string.IsNullOrWhiteSpace(l.EmployeeNameSnapshot) ? "#" + l.EmployeeId : l.EmployeeNameSnapshot;
  foreach (var l in lines)
  {
   if (l.Net <= 0) found.Add(new("NetNotPositive", l.EmployeeId, l.EmployeeCodeSnapshot, N(l), $"Net pay is {l.Net:0.00}."));
   if (l.TotalDeductions > l.Gross && l.Gross > 0) found.Add(new("DeductionsAboveGross", l.EmployeeId, l.EmployeeCodeSnapshot, N(l), $"Deductions {l.TotalDeductions:0.00} exceed gross {l.Gross:0.00}."));
   var p = prevLines.FirstOrDefault(x => x.EmployeeId == l.EmployeeId);
   if (t.NetChangePercent > 0 && p != null && p.Net > 0)
   {
    var change = Math.Abs(l.Net - p.Net) / p.Net * 100m;
    if (change > t.NetChangePercent) found.Add(new("NetChange", l.EmployeeId, l.EmployeeCodeSnapshot, N(l), $"Net moved {change:0.#}% from the previous run ({p.Net:0.00} to {l.Net:0.00}); threshold {t.NetChangePercent:0.#}%."));
   }
   if (t.LateMinutes > 0 && l.LateMinutes > t.LateMinutes) found.Add(new("LateMinutes", l.EmployeeId, l.EmployeeCodeSnapshot, N(l), $"{l.LateMinutes} late minutes; threshold {t.LateMinutes}."));
   if (t.AbsentDays > 0 && l.AbsentDays > t.AbsentDays) found.Add(new("AbsentDays", l.EmployeeId, l.EmployeeCodeSnapshot, N(l), $"{l.AbsentDays} absent days; threshold {t.AbsentDays}."));
   if (t.OvertimeHours > 0 && l.OvertimeHours > t.OvertimeHours) found.Add(new("OvertimeHours", l.EmployeeId, l.EmployeeCodeSnapshot, N(l), $"{l.OvertimeHours:0.#} overtime hours; threshold {t.OvertimeHours:0.#}."));
  }
  return found;
 }
}
