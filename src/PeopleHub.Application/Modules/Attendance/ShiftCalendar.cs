using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Attendance;
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
 public Task RotateAsync(int employeeId,DateTime from,DateTime to,string pattern,string reason)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.AttManage))throw new DomainException("err.forbidden");
  from=from.Date;to=to.Date;
  if(from<clock.Today||to<from||to.Year>9998||(to-from).TotalDays>92||string.IsNullOrWhiteSpace(reason))throw new DomainException("err.required");
  var tokens=(pattern??"").Split(',',StringSplitOptions.TrimEntries);var sequence=new List<int>();
  foreach(var token in tokens){if(!int.TryParse(token,out var id)||id<=0)throw new DomainException("err.required");sequence.Add(id);}
  if(sequence.Count<1||sequence.Count>31)throw new DomainException("err.required");
  var valid=await repo.ToListAsync(repo.Query<Shift>());if(sequence.Any(id=>valid.All(s=>s.Id!=id)))throw new DomainException("err.notFound");
  var employee=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e=>e.Id==employeeId))??throw new DomainException("err.notFound");
  if(await repo.AnyAsync(repo.Query<EmployeeShiftDay>().Where(x=>x.EmployeeId==employeeId&&x.Date>=from&&x.Date<=to)))throw new DomainException("err.overlap");
  for(var day=from;day<=to;day=day.AddDays(1))repo.Add(new EmployeeShiftDay{EmployeeId=employeeId,Date=day,ShiftId=sequence[(day-from).Days%sequence.Count],Reason=reason.Trim(),Actor=me.Email,At=clock.UtcNow});
  employee.LeaveRequestStamp=Guid.NewGuid().ToString("N");audit.Log("RotateShifts","Employee",employeeId,$"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}, calendar-day pattern {string.Join(',',sequence)}: {reason.Trim()}");await repo.SaveAsync();
 });
 public Task AddShiftAsync(string name,string? nameAr,TimeSpan start,TimeSpan end,int grace,int maxLate,bool flexible=false,TimeSpan? latestStart=null,int? requiredMinutes=null)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.OrgManage))throw new DomainException("err.forbidden");if(string.IsNullOrWhiteSpace(name)||name.Length>100||start<TimeSpan.Zero||start>=TimeSpan.FromDays(1)||end<TimeSpan.Zero||end>=TimeSpan.FromDays(1)||start==end||grace<0||maxLate<grace||maxLate>1440)throw new DomainException("err.required");if(flexible&&(latestStart==null||requiredMinutes is not >0||requiredMinutes>1440||latestStart<start||latestStart>=TimeSpan.FromDays(1)||latestStart.Value.TotalMinutes+requiredMinutes>1440||end.TotalMinutes!=latestStart.Value.TotalMinutes+requiredMinutes||end<=start))throw new DomainException("err.required");repo.Add(new Shift{Flexible=flexible,LatestStart=flexible?latestStart:null,RequiredMinutes=flexible?requiredMinutes:null,Name=name.Trim(),NameAr=nameAr?.Trim()??"",Start=start,End=end,GraceMinutes=grace,MaxLateMinutes=maxLate});audit.Log("Create","Shift",name.Trim(),$"{start}-{end}, grace {grace}");await repo.SaveAsync();
 });
}
