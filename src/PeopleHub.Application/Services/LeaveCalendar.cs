using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record LeaveCalendarDay(DateTime Date,List<Employee> Away);
public record LeaveCalendarView(int Year,int Month,int? DepartmentId,List<Department> Departments,List<LeaveCalendarDay> Days);
public class LeaveCalendar(IRepository repo,AccessScope scope)
{
 public async Task<LeaveCalendarView> GetAsync(int year,int month,int? departmentId)
 {
  if(year<2000||year>2100||month<1||month>12)throw new DomainException("err.dates");
  var start=new DateTime(year,month,1);var end=start.AddMonths(1);var ids=await scope.VisibleEmployeeIdsAsync();
  var employees=await repo.ToListAsync(repo.Query<Employee>().Where(e=>ids==null||ids.Contains(e.Id)));var visible=employees.Select(e=>e.Id).ToList();
  var assignments=await repo.ToListAsync(repo.Query<EmployeeAssignment>().Where(a=>visible.Contains(a.EmployeeId)&&a.EffectiveFrom<end&&(a.EffectiveTo==null||a.EffectiveTo>start)));
  var deptIds=employees.Select(e=>e.DepartmentId).Concat(assignments.Select(a=>a.DepartmentId)).Distinct().ToList();
  var depts=await repo.ToListAsync(repo.Query<Department>().Where(d=>deptIds.Contains(d.Id)).OrderBy(d=>d.Name));
  if(departmentId!=null&&!deptIds.Contains(departmentId.Value))throw new DomainException("err.forbidden");
  var approved=await repo.ToListAsync(repo.Query<LeaveRequest>().Where(l=>visible.Contains(l.EmployeeId)&&l.Status==RequestStatus.Approved&&l.StartDate<end&&l.EndDate>=start));
  var days=new List<LeaveCalendarDay>();
  for(var date=start;date<end;date=date.AddDays(1)){
   var day=date;
   var away=employees.Where(e=>approved.Any(l=>l.EmployeeId==e.Id&&l.StartDate<=day&&l.EndDate>=day)&& (departmentId==null||(assignments.Where(a=>a.EmployeeId==e.Id&&a.EffectiveFrom<=day&&(a.EffectiveTo==null||a.EffectiveTo>day)).OrderByDescending(a=>a.EffectiveFrom).ThenByDescending(a=>a.Id).FirstOrDefault()?.DepartmentId??e.DepartmentId)==departmentId)).OrderBy(e=>e.FullName).ToList();
   days.Add(new(day,away));
  }
  return new(year,month,departmentId,depts,days);
 }
}
