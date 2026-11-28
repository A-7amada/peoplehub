using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Benefits;

/// <summary>US-PH2-015 claims record. Internal record only: no insurer submission, no payment, no payroll deduction. Illustrative workflow, not legally validated.</summary>
public class BenefitClaims(IRepository repo, ICurrentUser me, AppClock clock, AuditService audit)
{
 public static readonly string[] Flow = ["Recorded", "Reviewed", "Closed"];
 private void Need() { if (!me.Can(Perm.BenefitsManage)) throw new DomainException("err.forbidden"); }

 /// <summary>Benefits managers see every claim; an employee sees only their own.</summary>
 public async Task<List<BenefitClaim>> ListAsync()
 {
  var hr = me.Can(Perm.BenefitsManage);
  return await repo.ToListAsync(repo.Query<BenefitClaim>().Where(x => hr || x.EmployeeId == me.EmployeeId).OrderByDescending(x => x.Id));
 }

 public Task RecordAsync(int enrollmentId, DateTime serviceDate, decimal amount, string description) => repo.AtomicAsync(async () =>
 {
  Need();
  if (amount <= 0 || amount > 100000000 || string.IsNullOrWhiteSpace(description) || description.Length > 2000 || serviceDate.Year < 2000 || serviceDate.Date > clock.Today) throw new DomainException("err.required");
  var e = await repo.FirstOrDefaultAsync(repo.Query<BenefitEnrollment>().Where(x => x.Id == enrollmentId)) ?? throw new DomainException("err.notFound");
  if (serviceDate.Date < e.StartsOn.Date || serviceDate.Date > e.EndsOn.Date) throw new DomainException("err.required");
  var cancel = await repo.FirstOrDefaultAsync(repo.Query<BenefitCancellation>().Where(x => x.EnrollmentId == e.Id));
  if (cancel != null && serviceDate.Date >= cancel.EffectiveOn.Date) throw new DomainException("err.required");
  var plan = await repo.FirstOrDefaultAsync(repo.Query<BenefitPlan>().Where(x => x.Id == e.PlanId)) ?? throw new DomainException("err.notFound");
  repo.Add(new BenefitClaim { EnrollmentId = e.Id, EmployeeId = e.EmployeeId, ServiceDate = serviceDate.Date, Amount = Math.Round(amount, 2), Currency = plan.Currency, Description = description.Trim(), Actor = me.Email, At = clock.UtcNow });
  audit.Log("Record", "BenefitClaim", e.Id, "Internal claim record; nothing is sent to an insurer, paid or deducted");
  await repo.SaveAsync();
 });

 public Task AdvanceAsync(int claimId, string? outcome) => repo.AtomicAsync(async () =>
 {
  Need();
  var c = await repo.FirstOrDefaultAsync(repo.Query<BenefitClaim>().Where(x => x.Id == claimId)) ?? throw new DomainException("err.notFound");
  var next = Array.IndexOf(Flow, c.Status) + 1;
  if (next >= Flow.Length || (outcome?.Length ?? 0) > 2000) throw new DomainException("err.required");
  if (Flow[next] == "Closed" && string.IsNullOrWhiteSpace(outcome)) throw new DomainException("err.reasonRequired");
  var before = c.Status; c.Status = Flow[next]; if (!string.IsNullOrWhiteSpace(outcome)) c.Outcome = outcome.Trim();
  audit.Log("Advance", "BenefitClaim", c.Id, $"{before} to {c.Status}; internal record only");
  await repo.SaveAsync();
 });
}
