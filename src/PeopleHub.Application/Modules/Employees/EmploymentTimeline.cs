using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Employees;
public class EmploymentTimeline(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 public async Task<List<EmploymentChange>> ListAsync(int employeeId,AccessScope scope){if(!await scope.CanSeeAsync(employeeId))throw new DomainException("err.forbidden");return (await repo.ToListAsync(repo.Query<EmploymentChange>().Where(x=>x.EmployeeId==employeeId).OrderBy(x=>x.EffectiveOn))).Select(x=>{var copy=System.Text.Json.JsonSerializer.Deserialize<EmploymentChange>(System.Text.Json.JsonSerializer.Serialize(x))!;if(me.EmployeeId!=employeeId&&!me.Can(Perm.EmpIdentity))copy.Reason="";if(!scope.CanSeeSalary(employeeId))copy.BasicSalary=0;return copy;}).ToList();}
 public Task ScheduleAsync(EmploymentChange change)=>repo.AtomicAsync(()=>ScheduleCoreAsync(change));
 private async Task ScheduleCoreAsync(EmploymentChange change)
 {
  if(!me.Can(Perm.EmpManage))throw new DomainException("err.forbidden");
  if(change.EffectiveOn.Date<=clock.Today || change.EffectiveOn.Year>9998 || string.IsNullOrWhiteSpace(change.Reason))throw new DomainException("err.required");
  var e=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e=>e.Id==change.EmployeeId))??throw new DomainException("err.notFound");
  if(e.Status is EmployeeStatus.Terminated or EmployeeStatus.Resigned or EmployeeStatus.Retired)throw new DomainException("err.finalStatus");
  if(change.Kind=="Status" && change.ReturnOn!=null)throw new DomainException("err.required");
  if(change.ReturnOn is {} returnOn && (returnOn.Date<=change.EffectiveOn.Date || returnOn.Year>9998))throw new DomainException("err.required");
  if(change.Kind=="Status") {if(change.Status==null || !Enum.IsDefined(change.Status.Value) || change.Status==e.Status)throw new DomainException("err.required");}
  else if(change.Kind=="Assignment")
  {
   if(!me.Can(Perm.EmpPay)) change.BasicSalary=e.BasicSalary;
   if(change.BasicSalary<0 || !await repo.AnyAsync(repo.Query<Department>().Where(x=>x.Id==change.DepartmentId)) || !await repo.AnyAsync(repo.Query<Branch>().Where(x=>x.Id==change.BranchId)) || !await repo.AnyAsync(repo.Query<JobTitle>().Where(x=>x.Id==change.JobTitleId)) || (change.ManagerId!=null && (change.ManagerId==e.Id || !await repo.AnyAsync(repo.Query<Employee>().Where(x=>x.Id==change.ManagerId)))) || (change.ShiftId!=null && !await repo.AnyAsync(repo.Query<Shift>().Where(x=>x.Id==change.ShiftId))))throw new DomainException("err.required");
  }
  else throw new DomainException("err.required");
  if(await repo.AnyAsync(repo.Query<EmploymentChange>().Where(x=>x.EmployeeId==e.Id && !x.Cancelled && x.AppliedAt==null)))throw new DomainException("err.required");
  e.LeaveRequestStamp=Guid.NewGuid().ToString("N");
  change.Id=0;change.EffectiveOn=change.EffectiveOn.Date;change.CreatedAt=clock.UtcNow;change.AppliedAt=null;change.Cancelled=false;change.ParentChangeId=null;change.ReturnOn=change.ReturnOn?.Date;change.Actor=me.Email;change.Reason=change.Reason.Trim();repo.Add(change);audit.Log("Schedule","Employee",e.Id,$"{change.Kind} on {change.EffectiveOn:yyyy-MM-dd}: {change.Reason}");await repo.SaveAsync();
  if(change.ReturnOn is {} back){repo.Add(new EmploymentChange{EmployeeId=e.Id,Kind="Assignment",EffectiveOn=back,ParentChangeId=change.Id,DepartmentId=e.DepartmentId,BranchId=e.BranchId,JobTitleId=e.JobTitleId,ManagerId=e.ManagerId,ShiftId=e.ShiftId,BasicSalary=e.BasicSalary,Reason=$"Return from temporary assignment #{change.Id}: {change.Reason}",Actor=me.Email,CreatedAt=clock.UtcNow});await repo.SaveAsync();}
 }
 public Task CancelAsync(int id,string reason)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.EmpManage))throw new DomainException("err.forbidden");if(string.IsNullOrWhiteSpace(reason))throw new DomainException("err.reasonRequired");
  var x=await repo.FirstOrDefaultAsync(repo.Query<EmploymentChange>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");if(x.ParentChangeId!=null || x.AppliedAt!=null || x.Cancelled || x.EffectiveOn<=clock.Today)throw new DomainException("err.forbidden");x.Cancelled=true;
  foreach(var child in await repo.ToListAsync(repo.Query<EmploymentChange>().Where(c=>c.ParentChangeId==x.Id && c.AppliedAt==null)))child.Cancelled=true;
  audit.Log("CancelSchedule","Employee",x.EmployeeId,$"schedule #{id}: {reason.Trim()}");await repo.SaveAsync();
 });
 public async Task ApplyDueAsync()
 {
  var due=await repo.ToListAsync(repo.Query<EmploymentChange>().Where(x=>!x.Cancelled && x.AppliedAt==null && x.EffectiveOn<=clock.Today).OrderBy(x=>x.EffectiveOn).ThenBy(x=>x.Id).Select(x=>x.EmployeeId));
  var failed=new List<Exception>();
  // Keep a temporary assignment and its return in the same employee transaction.
  foreach(var employeeId in due.Distinct())
  {
   try { await repo.AtomicAsync(async()=>
   {
    var pending=await repo.ToListAsync(repo.Query<EmploymentChange>().Where(x=>x.EmployeeId==employeeId && !x.Cancelled && x.AppliedAt==null && x.EffectiveOn<=clock.Today).OrderBy(x=>x.EffectiveOn).ThenBy(x=>x.Id));
    foreach(var x in pending)
    {
   var e=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e=>e.Id==x.EmployeeId))??throw new DomainException("err.notFound");
   if(x.Kind=="Status")repo.Add(e.ChangeStatus(x.Status!.Value,x.Actor,x.Reason,x.EffectiveOn));
   else
   {
    var prior=await repo.ToListAsync(repo.Query<EmployeeAssignment>().Where(a=>a.EmployeeId==e.Id).OrderBy(a=>a.EffectiveFrom).ThenBy(a=>a.Id));
    var previous=prior.LastOrDefault(a=>a.EffectiveFrom<=x.EffectiveOn && a.EffectiveTo==null);
    if(previous!=null)previous.EffectiveTo=x.EffectiveOn;
    else if(prior.Count==0){var baseline=EmployeeAssignment.Capture(e,x.CreatedAt.Date,x.Actor,"Observed baseline at scheduling; earlier effective dates unknown");baseline.EffectiveTo=x.EffectiveOn;repo.Add(baseline);}
    e.DepartmentId=x.DepartmentId;e.BranchId=x.BranchId;e.JobTitleId=x.JobTitleId;e.ManagerId=x.ManagerId;e.ShiftId=x.ShiftId;e.BasicSalary=x.BasicSalary;
    repo.Add(EmployeeAssignment.Capture(e,x.EffectiveOn,x.Actor,x.Reason));
   }
   x.AppliedAt=clock.UtcNow;audit.Log("ApplySchedule","Employee",e.Id,$"schedule #{x.Id}, effective {x.EffectiveOn:yyyy-MM-dd}");await repo.SaveAsync();
    }
   }); }
   catch(Exception ex) when(ex is not OperationCanceledException){failed.Add(new InvalidOperationException($"Employee schedule deferred: {employeeId}",ex));}
  }
  if(failed.Count>0)throw new AggregateException("Some employee schedules were deferred; unrelated employee batches were processed.",failed);
 }
}
