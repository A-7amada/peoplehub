using System.Globalization;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Leave;
/// <summary>US-PH2-004 leave encashment as a days-only approval record. Off per leave type until a maximum is set (setting Leave.Encash.MaxDays.CODE, days per year, default 0 = not allowed). Approval deducts the days from the balance with a ledger entry. It computes no money and changes no payroll: the payout rate and payroll linkage need the owner and a payroll specialist. Illustrative, not legally validated.</summary>
public class LeaveEncashments(IRepository repo, ICurrentUser me, AuditService audit, AppClock clock)
{
 private void Guard() { if (!me.Can(Perm.LeaveAdmin)) throw new DomainException("err.forbidden"); }
 private static string Key(string code) => "Leave.Encash.MaxDays." + code;
 public async Task<decimal> MaxDaysAsync(string code)
 {
  Guard();
  var v = (await repo.FirstOrDefaultAsync(repo.Query<AppSetting>().Where(x => x.Key == Key(code))))?.Value;
  return decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) && d is >= 0 and <= 366 ? d : 0m;
 }
 public Task SetMaxDaysAsync(int leaveTypeId, decimal days) => repo.AtomicAsync(async () =>
 {
  Guard();
  if (days is < 0 or > 366 || decimal.Round(days, 1) != days) throw new DomainException("err.required");
  var t = await repo.FirstOrDefaultAsync(repo.Query<LeaveType>().Where(x => x.Id == leaveTypeId)) ?? throw new DomainException("err.notFound");
  var key = Key(t.Code);
  var row = await repo.FirstOrDefaultAsync(repo.Query<AppSetting>().Where(x => x.Key == key));
  if (row == null) { row = new() { Key = key }; repo.Add(row); }
  row.Value = days.ToString(CultureInfo.InvariantCulture);
  audit.Log("LeaveEncashMax", "LeaveType", t.Code, $"{days} days per year ({(days == 0 ? "not allowed" : "allowed")}); days only, no payout amount; illustrative, not legally validated");
  await repo.SaveAsync();
 });
 public async Task<List<LeaveEncashment>> ListAsync() { Guard(); return await repo.ToListAsync(repo.Query<LeaveEncashment>().OrderByDescending(x => x.Id)); }
 public Task RequestAsync(int employeeId, int leaveTypeId, int year, decimal days) => repo.AtomicAsync(async () =>
 {
  Guard();
  if (days <= 0 || days > 366 || decimal.Round(days, 1) != days || year is < 2000 or > 2100) throw new DomainException("err.required");
  var t = await repo.FirstOrDefaultAsync(repo.Query<LeaveType>().Where(x => x.Id == leaveTypeId)) ?? throw new DomainException("err.notFound");
  if (await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x => x.Id == employeeId)) == null) throw new DomainException("err.notFound");
  var max = await MaxDaysAsync(t.Code);
  var open = (await repo.ToListAsync(repo.Query<LeaveEncashment>().Where(x => x.EmployeeId == employeeId && x.LeaveTypeId == leaveTypeId && x.Year == year))).Where(x => x.Status is "Requested" or "Approved").Sum(x => x.Days);
  if (max <= 0 || open + days > max) throw new DomainException("err.forbidden");
  var bal = await repo.FirstOrDefaultAsync(repo.Query<LeaveBalance>().Where(x => x.EmployeeId == employeeId && x.LeaveTypeId == leaveTypeId && x.Year == year)) ?? throw new DomainException("err.notFound");
  var pending = (await repo.ToListAsync(repo.Query<LeaveEncashment>().Where(x => x.EmployeeId == employeeId && x.LeaveTypeId == leaveTypeId && x.Year == year && x.Status == "Requested"))).Sum(x => x.Days);
  if (days + pending > bal.Remaining) throw new DomainException("err.forbidden");
  repo.Add(new LeaveEncashment { EmployeeId = employeeId, LeaveTypeId = leaveTypeId, Year = year, Days = days, RequestedBy = me.Email, RequestedAt = clock.UtcNow });
  audit.Log("LeaveEncashRequested", "Employee", employeeId, $"{days} days of {t.Code} {year}");
  await repo.SaveAsync();
 });
 public Task DecideAsync(int id, bool approve, string note) => repo.AtomicAsync(async () =>
 {
  Guard();
  if (string.IsNullOrWhiteSpace(note) || note.Trim().Length > 500) throw new DomainException("err.reasonRequired");
  var r = await repo.FirstOrDefaultAsync(repo.Query<LeaveEncashment>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
  if (r.Status != "Requested") throw new DomainException("err.notPending");
  if (string.Equals(r.RequestedBy, me.Email, StringComparison.OrdinalIgnoreCase)) throw new DomainException("err.forbidden");
  if (approve)
  {
   var bal = await repo.FirstOrDefaultAsync(repo.Query<LeaveBalance>().Where(x => x.EmployeeId == r.EmployeeId && x.LeaveTypeId == r.LeaveTypeId && x.Year == r.Year)) ?? throw new DomainException("err.notFound");
   if (r.Days > bal.Remaining) throw new DomainException("err.forbidden");
   bal.Used += r.Days;
   repo.Add(new LeaveTransaction { EmployeeId = r.EmployeeId, LeaveTypeId = r.LeaveTypeId, Year = r.Year, Delta = -r.Days, Kind = "Encashed", Reason = "Encashment approved (days only, no amount)", Actor = me.Email, At = clock.UtcNow });
  }
  r.Status = approve ? "Approved" : "Rejected"; r.DecidedBy = me.Email; r.DecidedAt = clock.UtcNow; r.DecisionNote = note.Trim();
  audit.Log(approve ? "LeaveEncashApproved" : "LeaveEncashRejected", "Employee", r.EmployeeId, $"{r.Days} days; no payout amount computed, payroll not changed");
  await repo.SaveAsync();
 });
 public Task CancelAsync(int id) => repo.AtomicAsync(async () =>
 {
  Guard();
  var r = await repo.FirstOrDefaultAsync(repo.Query<LeaveEncashment>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
  if (r.Status != "Requested") throw new DomainException("err.notPending");
  r.Status = "Cancelled"; r.DecidedBy = me.Email; r.DecidedAt = clock.UtcNow; r.DecisionNote = "Cancelled";
  audit.Log("LeaveEncashCancelled", "Employee", r.EmployeeId, $"{r.Days} days");
  await repo.SaveAsync();
 });
}
