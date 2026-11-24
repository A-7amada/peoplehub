using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record PunchException(int EmployeeId,string EmployeeName,string EmployeeNameAr,DateTime Date,string Kind,DateTime? CheckIn,DateTime? CheckOut,bool Locked);
public record PunchExceptionView(List<PunchException> Rows,List<DevicePunch> Unmatched);
public class AttendanceExceptions(IRepository repo,ICurrentUser me,AccessScope scope,AppClock clock)
{
 public async Task<PunchExceptionView> GetAsync(DateTime from,DateTime to)
 {
  if(from.Year<2000||to<from||to>clock.Today|| (to-from).TotalDays>93)throw new DomainException("err.required");from=from.Date;to=to.Date;var ids=await scope.VisibleEmployeeIdsAsync();
  var employees=await repo.ToListAsync(repo.Query<Employee>().Where(e=>ids==null||ids.Contains(e.Id)));var employeeIds=employees.Select(e=>e.Id).ToList();
  var recs=await repo.ToListAsync(repo.Query<AttendanceRecord>().Where(x=>employeeIds.Contains(x.EmployeeId)&&x.Date>=from.Date&&x.Date<=to.Date&&(x.CheckIn==null||x.CheckOut==null)&&!x.IsExcused));
  var assignments=await repo.ToListAsync(repo.Query<EmployeeAssignment>().Where(x=>employeeIds.Contains(x.EmployeeId)));var overrides=await repo.ToListAsync(repo.Query<EmployeeShiftDay>().Where(x=>employeeIds.Contains(x.EmployeeId)&&x.Date>=from.Date&&x.Date<=to.Date));var shifts=await repo.ToListAsync(repo.Query<Shift>());var locks=await repo.ToListAsync(repo.Query<AttendancePeriodLock>());var rows=new List<PunchException>();
  foreach(var rec in recs){var e=employees.First(e=>e.Id==rec.EmployeeId);var shift=shifts.FirstOrDefault(x=>x.Id==ShiftCalendar.ShiftId(e,rec.Date,assignments,overrides));if(shift==null)continue;var end=rec.Date.Date+shift.End;if(shift.IsNight)end=end.AddDays(1);if(end>=clock.Now)continue;rows.Add(new(e.Id,e.FullName,e.FullNameAr,rec.Date,rec.CheckIn==null&&rec.CheckOut==null?"NoPunches":rec.CheckIn==null?"MissingIn":"MissingOut",rec.CheckIn,rec.CheckOut,locks.Any(x=>x.EmployeeId==e.Id&&x.Year==rec.Date.Year&&x.Month==rec.Date.Month)));}
  var unmatched=me.Can(Perm.AttManage)?await repo.ToListAsync(repo.Query<DevicePunch>().Where(x=>x.EmployeeId==null&&x.Time>=from.Date&&x.Time<to.AddDays(1)).OrderByDescending(x=>x.Time).Take(100)):[];
  return new(rows.OrderByDescending(x=>x.Date).ThenBy(x=>x.EmployeeId).ToList(),unmatched);
 }
}
