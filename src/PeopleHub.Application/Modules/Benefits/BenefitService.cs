using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Benefits;
public record BenefitAllocationRow(string CostCenter,string Currency,int ActiveEnrollments,int Employees,decimal EmployerMonthly,decimal EmployeeMonthly);
public record BenefitAllocation(DateTime AsOf,List<BenefitAllocationRow> Rows);
public record BenefitView(List<BenefitPlan> Plans,List<BenefitEnrollment> Enrollments,List<BenefitCancellation> Cancellations,List<EmployeeDependent> Dependents);
public class BenefitService(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 private void Need(){if(!me.Can(Perm.BenefitsManage))throw new DomainException("err.forbidden");}
 public async Task<BenefitView> GetAsync(){bool hr=me.Can(Perm.BenefitsManage);var enroll=await repo.ToListAsync(repo.Query<BenefitEnrollment>().Where(x=>hr||x.EmployeeId==me.EmployeeId).OrderByDescending(x=>x.Id));var ids=enroll.Select(x=>x.Id).ToList();return new(await repo.ToListAsync(repo.Query<BenefitPlan>().OrderBy(x=>x.Name)),enroll,await repo.ToListAsync(repo.Query<BenefitCancellation>().Where(x=>ids.Contains(x.EnrollmentId))),hr?await repo.ToListAsync(repo.Query<EmployeeDependent>()):[]);}
 public async Task<BenefitAllocation> AllocationAsync(DateTime asOf)
 {
  Need();if(asOf.Year<2000||asOf.Year>9998)throw new DomainException("err.required");var day=asOf.Date;
  var entries=await repo.ToListAsync(repo.Query<BenefitEnrollment>().Where(x=>x.StartsOn<=day&&x.EndsOn>=day));
  var ids=entries.Select(x=>x.Id).ToList();var cancelled=await repo.ToListAsync(repo.Query<BenefitCancellation>().Where(x=>ids.Contains(x.EnrollmentId)&&x.EffectiveOn<=day).Select(x=>x.EnrollmentId));
  var plans=await repo.ToListAsync(repo.Query<BenefitPlan>());
  var active=entries.Where(x=>!cancelled.Contains(x.Id)).Select(x=>new {Entry=x,Plan=plans.First(p=>p.Id==x.PlanId)});
  var rows=active.GroupBy(x=>new {x.Entry.CostCenter,x.Plan.Currency}).OrderBy(g=>g.Key.CostCenter).ThenBy(g=>g.Key.Currency).Select(g=>new BenefitAllocationRow(g.Key.CostCenter,g.Key.Currency,g.Count(),g.Select(x=>x.Entry.EmployeeId).Distinct().Count(),g.Sum(x=>x.Plan.EmployerMonthly),g.Sum(x=>x.Plan.EmployeeMonthly))).ToList();
  return new(day,rows);
 }
 public Task AddPlanAsync(string name,string provider,string eligibility,string coverage,bool dependentsAllowed,decimal employerMonthly,decimal employeeMonthly,string currency)=>repo.AtomicAsync(async()=>
 {
  Need();if(string.IsNullOrWhiteSpace(name)||name.Length>150||string.IsNullOrWhiteSpace(provider)||provider.Length>200||string.IsNullOrWhiteSpace(eligibility)||eligibility.Length>2000||string.IsNullOrWhiteSpace(coverage)||coverage.Length>4000||employerMonthly<0||employeeMonthly<0||employerMonthly>1000000000||employeeMonthly>1000000000||currency==null||currency.Length!=3||!currency.All(c=>c>='A'&&c<='Z'))throw new DomainException("err.required");
  repo.Add(new BenefitPlan{Name=name.Trim(),Provider=provider.Trim(),Eligibility=eligibility.Trim(),Coverage=coverage.Trim(),DependentsAllowed=dependentsAllowed,EmployerMonthly=employerMonthly,EmployeeMonthly=employeeMonthly,Currency=currency,Actor=me.Email,At=clock.UtcNow});audit.Log("Create","BenefitPlan",name.Trim(),"Recorded plan only; no purchase, insurer notification or payroll effect");await repo.SaveAsync();
 });
 public Task EnrollAsync(int planId,int employeeId,int? dependentId,DateTime startsOn,DateTime endsOn,string eligibilityEvidence,string costCenter)=>repo.AtomicAsync(async()=>
 {
  Need();if(startsOn.Date<clock.Today||endsOn.Date<startsOn.Date||endsOn.Year>9998||string.IsNullOrWhiteSpace(eligibilityEvidence)||eligibilityEvidence.Length>2000||string.IsNullOrWhiteSpace(costCenter)||costCenter.Length>100)throw new DomainException("err.required");
  var plan=await repo.FirstOrDefaultAsync(repo.Query<BenefitPlan>().Where(x=>x.Id==planId))??throw new DomainException("err.notFound");var emp=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==employeeId))??throw new DomainException("err.notFound");if(!emp.IsWorking||emp.HireDate.Date>startsOn.Date)throw new DomainException("err.forbidden");
  EmployeeDependent? dependent=null;if(dependentId!=null){if(!plan.DependentsAllowed)throw new DomainException("err.forbidden");dependent=await repo.FirstOrDefaultAsync(repo.Query<EmployeeDependent>().Where(x=>x.Id==dependentId&&x.EmployeeId==employeeId))??throw new DomainException("err.notFound");}
  var rows=await repo.ToListAsync(repo.Query<BenefitEnrollment>().Where(x=>x.PlanId==planId&&x.EmployeeId==employeeId&&x.DependentId==dependentId&&x.StartsOn<=endsOn.Date&&x.EndsOn>=startsOn.Date));var ids=rows.Select(x=>x.Id).ToList();var cancelled=await repo.ToListAsync(repo.Query<BenefitCancellation>().Where(x=>ids.Contains(x.EnrollmentId)));
  if(rows.Any(x=>{var cancel=cancelled.FirstOrDefault(y=>y.EnrollmentId==x.Id);return cancel==null||cancel.EffectiveOn>startsOn.Date;}))throw new DomainException("err.overlap");
  emp.LeaveRequestStamp=Guid.NewGuid().ToString("N");repo.Add(new BenefitEnrollment{PlanId=planId,EmployeeId=employeeId,DependentId=dependentId,CoveredName=dependent?.Name??emp.FullName,CoveredRelationship=dependent?.Relationship??"Employee",StartsOn=startsOn.Date,EndsOn=endsOn.Date,EligibilityEvidence=eligibilityEvidence.Trim(),CostCenter=costCenter.Trim(),Actor=me.Email,At=clock.UtcNow});audit.Log("Enroll","Benefit",employeeId,"Internal eligibility evidence and allocation recorded, no payroll or insurer effect");await repo.SaveAsync();
 });
 public Task CancelAsync(int enrollmentId,DateTime effectiveOn,string reason)=>repo.AtomicAsync(async()=>
 {
  Need();if(string.IsNullOrWhiteSpace(reason)||reason.Length>2000||effectiveOn.Date<clock.Today)throw new DomainException("err.required");var e=await repo.FirstOrDefaultAsync(repo.Query<BenefitEnrollment>().Where(x=>x.Id==enrollmentId))??throw new DomainException("err.notFound");if(effectiveOn.Date<e.StartsOn||effectiveOn.Date>e.EndsOn||await repo.AnyAsync(repo.Query<BenefitCancellation>().Where(x=>x.EnrollmentId==e.Id)))throw new DomainException("err.forbidden");
  var emp=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==e.EmployeeId))??throw new DomainException("err.notFound");emp.LeaveRequestStamp=Guid.NewGuid().ToString("N");repo.Add(new BenefitCancellation{EnrollmentId=e.Id,EffectiveOn=effectiveOn.Date,Reason=reason.Trim(),Actor=me.Email,At=clock.UtcNow});audit.Log("Cancel","Benefit",e.Id,"Internal cancellation recorded, no insurer cancellation or refund");await repo.SaveAsync();
 });
}
