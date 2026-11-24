using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
/// <summary>In-app reminders only, service-awake and single-instance.</summary>
public class ApprovalReminders(IRepository repo,AppClock clock)
{
 public Task RunAsync()=>repo.AtomicAsync(async()=>
 {
  var settings=await repo.ToListAsync(repo.Query<AppSetting>().Where(x=>x.Key.StartsWith("Workflow.ReminderHours.")));
  var requests=new List<(string Kind,Approvable Request)>();
  requests.AddRange((await repo.ToListAsync(repo.Query<LeaveRequest>().Where(x=>x.Status==RequestStatus.Pending))).Select(x=>("Leave",(Approvable)x)));
  requests.AddRange((await repo.ToListAsync(repo.Query<OvertimeRequest>().Where(x=>x.Status==RequestStatus.Pending))).Select(x=>("Overtime",(Approvable)x)));
  requests.AddRange((await repo.ToListAsync(repo.Query<AttendanceCorrection>().Where(x=>x.Status==RequestStatus.Pending))).Select(x=>("Correction",(Approvable)x)));
  requests.AddRange((await repo.ToListAsync(repo.Query<AdvanceRequest>().Where(x=>x.Status==RequestStatus.Pending))).Select(x=>("Advance",(Approvable)x)));
  var delegations=await repo.ToListAsync(repo.Query<Delegation>().Where(x=>!x.Revoked&&x.StartDate<=clock.Today&&x.EndDate>=clock.Today));
  var users=await repo.ToListAsync(repo.Query<AppUser>().Where(x=>x.IsActive));var employees=await repo.ToListAsync(repo.Query<Employee>());var assignments=await repo.ToListAsync(repo.Query<ApprovalAssignment>());var actions=await repo.ToListAsync(repo.Query<ApprovalAction>());var done=await repo.ToListAsync(repo.Query<ApprovalReminder>().Where(x=>x.OnDate==clock.Today));
  foreach(var(kind,r)in requests)
  {
   if(!int.TryParse(settings.FirstOrDefault(x=>x.Key=="Workflow.ReminderHours."+kind)?.Value,out var hours)||hours<1||hours>8760||r.CurrentStage==null)continue;
   var since=actions.Where(x=>x.Kind==kind&&x.RequestId==r.Id&&(x.Action.StartsWith("Approved:")||x.Action.StartsWith("Reassigned:"))).Select(x=>x.At).Append(r.CreatedAt).Max();if(since.AddHours(hours)>clock.UtcNow)continue;
   var assignment=assignments.Where(x=>x.Kind==kind&&x.RequestId==r.Id&&x.StageIndex==r.StageIndex).OrderByDescending(x=>x.Id).FirstOrDefault();
   foreach(var u in users.Where(x=>x.EmployeeId!=r.EmployeeId&&(x.EmployeeId==null||employees.Any(e=>e.Id==x.EmployeeId&&e.IsWorking))))
   {
    bool eligible=u.RoleArray.Contains(Roles.SystemAdmin)||(assignment!=null?assignment.ToUserId==u.Id:r.CurrentStage==ApprovalStage.Manager?r.Employee?.ManagerId!=null&&r.Employee.ManagerId==u.EmployeeId:r.CurrentStage.Split('|').Intersect(u.RoleArray).Any());
    if(!eligible&&assignment==null&&u.EmployeeId is {} employeeId){foreach(var d in delegations.Where(x=>x.ToEmployeeId==employeeId)){var sourceRoles=users.Where(x=>x.EmployeeId==d.FromEmployeeId).SelectMany(x=>x.RoleArray);if(r.CurrentStage==ApprovalStage.Manager?r.Employee?.ManagerId==d.FromEmployeeId:r.CurrentStage.Split('|').Intersect(sourceRoles).Any()){eligible=true;break;}}}
    if(!eligible||done.Any(x=>x.Kind==kind&&x.RequestId==r.Id&&x.StageIndex==r.StageIndex&&x.UserId==u.Id))continue;
    repo.Add(new Notification{UserId=u.Id,TitleKey="notif.approvalReminder",Details=$"{kind} #{r.Id}, stage {r.StageIndex+1}",Link="/Requests?scope=inbox",CreatedAt=clock.UtcNow});repo.Add(new ApprovalReminder{Kind=kind,RequestId=r.Id,StageIndex=r.StageIndex,UserId=u.Id,OnDate=clock.Today,At=clock.UtcNow});
   }
  }
  await repo.SaveAsync();
 });
}
