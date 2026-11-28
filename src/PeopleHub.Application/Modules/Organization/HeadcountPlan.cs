using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Organization;
public record PlanRow(Department Dept, int Active, int Target, int Gap, decimal AvgBasic, decimal GapMonthlyCost);
/// <summary>US-PH3-011 read-only workforce cost per target headcount. Active and on-leave employees per department against the department target. The cost of filling a gap is gap times the department's average basic salary per month. Basic only: no allowances, insurance, tax or hiring cost. It forecasts nothing. Illustrative.</summary>
public class HeadcountPlan(IRepository repo, ICurrentUser me)
{
 public async Task<List<PlanRow>> BuildAsync()
 {
  if (!me.Can(Perm.OrgManage)) throw new DomainException("err.forbidden");
  var deps = await repo.ToListAsync(repo.Query<Department>().OrderBy(x => x.Name));
  var emps = (await repo.ToListAsync(repo.Query<Employee>())).Where(e => e.Status is EmployeeStatus.Active or EmployeeStatus.OnLeave).ToList();
  return deps.Select(d =>
  {
   var mine = emps.Where(e => e.DepartmentId == d.Id).ToList();
   var avg = mine.Count == 0 ? 0m : Math.Round(mine.Average(e => e.BasicSalary), 2);
   var gap = Math.Max(0, d.TargetHeadcount - mine.Count);
   return new PlanRow(d, mine.Count, d.TargetHeadcount, d.TargetHeadcount - mine.Count, avg, gap * avg);
  }).ToList();
 }
}
