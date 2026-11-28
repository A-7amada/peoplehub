using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Requests;
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
  var votes=await repo.ToListAsync(repo.Query<ApprovalVote>());
  foreach(var(kind,r)in requests)
  {
   if(!int.TryParse(settings.FirstOrDefault(x=>x.Key=="Workflow.ReminderHours."+kind)?.Value,out var hours)||hours<1||hours>8760||r.CurrentStage==null)continue;
   var since=actions.Where(x=>x.Kind==kind&&x.RequestId==r.Id&&(x.Action.StartsWith("Approved:")||x.Action.StartsWith("Reassigned:"))).Select(x=>x.At).Append(r.CreatedAt).Max();if(since.AddHours(hours)>clock.UtcNow)continue;
   var assignment=assignments.Where(x=>x.Kind==kind&&x.RequestId==r.Id&&x.StageIndex==r.StageIndex).OrderByDescending(x=>x.Id).FirstOrDefault();
   foreach(var u in users.Where(x=>x.EmployeeId!=r.EmployeeId&&(x.EmployeeId==null||employees.Any(e=>e.Id==x.EmployeeId&&e.IsWorking))))
   {
    bool eligible=u.RoleArray.Contains(Roles.SystemAdmin)||(assignment!=null?assignment.ToUserId==u.Id:ApprovalStage.EmployeeId(r.CurrentStage) is {} resolved?resolved==u.EmployeeId:r.CurrentStage==ApprovalStage.Manager?r.Employee?.ManagerId!=null&&r.Employee.ManagerId==u.EmployeeId:r.CurrentStage.Split('|').Intersect(u.RoleArray).Any());
    var group=ApprovalStage.Group(r.CurrentStage)!=null;
    if(group)eligible=ApprovalGroups.SeatFor(kind,r,u,delegations.Where(d=>d.ToEmployeeId==u.EmployeeId).Select(d=>d.FromEmployeeId),votes,assignments,users,employees)!=null;
    if(!group&&!eligible&&assignment==null&&u.EmployeeId is {} employeeId){foreach(var d in delegations.Where(x=>x.ToEmployeeId==employeeId)){var sourceRoles=users.Where(x=>x.EmployeeId==d.FromEmployeeId).SelectMany(x=>x.RoleArray);if(ApprovalStage.EmployeeId(r.CurrentStage) is {} resolvedDelegate?resolvedDelegate==d.FromEmployeeId:r.CurrentStage==ApprovalStage.Manager?r.Employee?.ManagerId==d.FromEmployeeId:r.CurrentStage.Split('|').Intersect(sourceRoles).Any()){eligible=true;break;}}}
    if(!eligible||done.Any(x=>x.Kind==kind&&x.RequestId==r.Id&&x.StageIndex==r.StageIndex&&x.UserId==u.Id))continue;
    repo.Add(new Notification{UserId=u.Id,TitleKey="notif.approvalReminder",Details=$"{kind} #{r.Id}, stage {r.StageIndex+1}",Link="/Requests?scope=inbox",CreatedAt=clock.UtcNow});repo.Add(new ApprovalReminder{Kind=kind,RequestId=r.Id,StageIndex=r.StageIndex,UserId=u.Id,OnDate=clock.Today,At=clock.UtcNow});
   }
  }
  // US-PH3-007 escalation: a second, later notice to a configurable role. It only notifies; it never approves, rejects or reassigns.
  var escalationSettings=await repo.ToListAsync(repo.Query<AppSetting>().Where(x=>x.Key.StartsWith("Workflow.EscalationHours.")||x.Key=="Workflow.EscalationRole"));
  var role=escalationSettings.FirstOrDefault(x=>x.Key=="Workflow.EscalationRole")?.Value;if(string.IsNullOrWhiteSpace(role))role=Roles.HRManager;
  var escalated=await repo.ToListAsync(repo.Query<ApprovalReminder>().Where(x=>x.Kind.StartsWith("Escalation:")));
  foreach(var(kind,r)in requests)
  {
   if(!int.TryParse(escalationSettings.FirstOrDefault(x=>x.Key=="Workflow.EscalationHours."+kind)?.Value,out var hours)||hours<1||hours>8760||r.CurrentStage==null)continue;
   var since=actions.Where(x=>x.Kind==kind&&x.RequestId==r.Id&&(x.Action.StartsWith("Approved:")||x.Action.StartsWith("Reassigned:"))).Select(x=>x.At).Append(r.CreatedAt).Max();if(since.AddHours(hours)>clock.UtcNow)continue;
   foreach(var u in users.Where(x=>x.EmployeeId!=r.EmployeeId&&x.RoleArray.Contains(role)&&(x.EmployeeId==null||employees.Any(e=>e.Id==x.EmployeeId&&e.IsWorking))))
   {
    if(escalated.Any(x=>x.Kind=="Escalation:"+kind&&x.RequestId==r.Id&&x.StageIndex==r.StageIndex&&x.UserId==u.Id))continue;
    repo.Add(new Notification{UserId=u.Id,TitleKey="notif.approvalEscalation",Details=$"{kind} #{r.Id}, stage {r.StageIndex+1}",Link="/Requests?scope=inbox",CreatedAt=clock.UtcNow});
    var marker=new ApprovalReminder{Kind="Escalation:"+kind,RequestId=r.Id,StageIndex=r.StageIndex,UserId=u.Id,OnDate=clock.Today,At=clock.UtcNow};repo.Add(marker);escalated.Add(marker);
   }
  }
  await repo.SaveAsync();
 });

 /// <summary>Pending requests whose current stage has waited at least the reminder or escalation hours, for the overdue report. Read-only.</summary>
 public async Task<List<(string Kind,int RequestId,int Stage,double HoursWaiting,bool Escalated)>> OverdueAsync()
 {
  var settings=await repo.ToListAsync(repo.Query<AppSetting>().Where(x=>x.Key.StartsWith("Workflow.ReminderHours.")||x.Key.StartsWith("Workflow.EscalationHours.")));
  int H(string key){return int.TryParse(settings.FirstOrDefault(x=>x.Key==key)?.Value,out var h)&&h>=1&&h<=8760?h:0;}
  var requests=new List<(string Kind,Approvable Request)>();
  requests.AddRange((await repo.ToListAsync(repo.Query<LeaveRequest>().Where(x=>x.Status==RequestStatus.Pending))).Select(x=>("Leave",(Approvable)x)));
  requests.AddRange((await repo.ToListAsync(repo.Query<OvertimeRequest>().Where(x=>x.Status==RequestStatus.Pending))).Select(x=>("Overtime",(Approvable)x)));
  requests.AddRange((await repo.ToListAsync(repo.Query<AttendanceCorrection>().Where(x=>x.Status==RequestStatus.Pending))).Select(x=>("Correction",(Approvable)x)));
  requests.AddRange((await repo.ToListAsync(repo.Query<AdvanceRequest>().Where(x=>x.Status==RequestStatus.Pending))).Select(x=>("Advance",(Approvable)x)));
  var actions=await repo.ToListAsync(repo.Query<ApprovalAction>());var result=new List<(string,int,int,double,bool)>();
  foreach(var(kind,r)in requests)
  {
   if(r.CurrentStage==null)continue;var limit=H("Workflow.EscalationHours."+kind);var remind=H("Workflow.ReminderHours."+kind);var due=new[]{limit,remind}.Where(x=>x>0).DefaultIfEmpty(0).Min();if(due==0)continue;
   var since=actions.Where(x=>x.Kind==kind&&x.RequestId==r.Id&&(x.Action.StartsWith("Approved:")||x.Action.StartsWith("Reassigned:"))).Select(x=>x.At).Append(r.CreatedAt).Max();var waited=(clock.UtcNow-since).TotalHours;
   if(waited>=due)result.Add((kind,r.Id,r.StageIndex+1,Math.Round(waited,1),limit>0&&waited>=limit));
  }
  return result.OrderByDescending(x=>x.Item4).ToList();
 }
}
