using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public static class ShiftCalendar
{
 public static int? ShiftId(Employee e,DateTime day,IEnumerable<EmployeeAssignment> history,IEnumerable<EmployeeShiftDay> overrides)
 {
  var one=overrides.FirstOrDefault(x=>x.EmployeeId==e.Id&&x.Date.Date==day.Date);if(one!=null)return one.ShiftId;
  var a=history.Where(x=>x.EmployeeId==e.Id&&x.EffectiveFrom<=day.Date&&(x.EffectiveTo==null||day.Date<x.EffectiveTo)).OrderByDescending(x=>x.EffectiveFrom).ThenByDescending(x=>x.Id).FirstOrDefault();return a?.ShiftId??e.ShiftId;
 }
 public static DateTime WorkDate(DateTime time,Shift shift)=>shift.IsNight && time.TimeOfDay<=shift.End?time.Date.AddDays(-1):time.Date;
 public static DateTime? OvernightOut(DateTime date,DateTime? checkIn,DateTime? checkOut,Shift shift)=>shift.IsNight&&checkIn!=null&&checkOut!=null&&checkOut<=checkIn?checkOut.Value.AddDays(1):checkOut;
}
public class ShiftSchedule(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 public async Task<List<EmployeeShiftDay>> ListAsync(int employeeId,AccessScope scope){if(!await scope.CanSeeAsync(employeeId))throw new DomainException("err.forbidden");return await repo.ToListAsync(repo.Query<EmployeeShiftDay>().Where(x=>x.EmployeeId==employeeId).OrderByDescending(x=>x.Date).Take(90));}
 public Task SetAsync(int employeeId,DateTime date,int shiftId,string reason)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.AttManage))throw new DomainException("err.forbidden");if(date<clock.Today||date.Year>9998||string.IsNullOrWhiteSpace(reason))throw new DomainException("err.required");var e=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e=>e.Id==employeeId))??throw new DomainException("err.notFound");if(!await repo.AnyAsync(repo.Query<Shift>().Where(s=>s.Id==shiftId)))throw new DomainException("err.notFound");
  var row=await repo.FirstOrDefaultAsync(repo.Query<EmployeeShiftDay>().Where(x=>x.EmployeeId==employeeId&&x.Date==date.Date));if(row==null){row=new(){EmployeeId=employeeId,Date=date.Date};repo.Add(row);}row.ShiftId=shiftId;row.Actor=me.Email;row.At=clock.UtcNow;row.Reason=reason.Trim();e.LeaveRequestStamp=Guid.NewGuid().ToString("N");audit.Log("ScheduleShift","Employee",employeeId,$"{date:yyyy-MM-dd}, shift #{shiftId}: {reason.Trim()}");await repo.SaveAsync();
 });
 public async Task AddShiftAsync(string name,string? nameAr,TimeSpan start,TimeSpan end,int grace,int maxLate)
 {
  if(!me.Can(Perm.OrgManage))throw new DomainException("err.forbidden");if(string.IsNullOrWhiteSpace(name)||name.Length>100||start<TimeSpan.Zero||start>=TimeSpan.FromDays(1)||end<TimeSpan.Zero||end>=TimeSpan.FromDays(1)||start==end||grace<0||maxLate<grace||maxLate>1440)throw new DomainException("err.required");repo.Add(new Shift{Name=name.Trim(),NameAr=nameAr?.Trim()??"",Start=start,End=end,GraceMinutes=grace,MaxLateMinutes=maxLate});audit.Log("Create","Shift",name.Trim(),$"{start}-{end}, grace {grace}");await repo.SaveAsync();
 }
}
