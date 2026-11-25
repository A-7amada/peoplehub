using PeopleHub.Domain.Entities;
namespace PeopleHub.Application.Modules.Requests;
/// <summary>Shared captured-seat eligibility for decisions, initial notifications and reminders.</summary>
public static class ApprovalGroups
{
 public static List<(int Seat,int Employee)> Remaining(string kind,Approvable request,IEnumerable<ApprovalVote> votes,IEnumerable<ApprovalAssignment> assignments,IEnumerable<AppUser> users,IEnumerable<Employee> employees)
 {
  if(ApprovalStage.Group(request.CurrentStage) is not {} group)return [];
  var voted=votes.Where(v=>v.Kind==kind&&v.RequestId==request.Id&&v.StageIndex==request.StageIndex).Select(v=>v.ParticipantEmployeeId).ToHashSet();
  var active=users.Where(u=>u.IsActive&&u.EmployeeId!=null).Select(u=>u.EmployeeId!.Value).ToHashSet();
  var working=employees.Where(e=>e.IsWorking).Select(e=>e.Id).ToHashSet();
  return group.Employees.Where(id=>!voted.Contains(id)).Select(id=>
  {
   var assignment=assignments.Where(a=>a.Kind==kind&&a.RequestId==request.Id&&a.StageIndex==request.StageIndex&&a.ParticipantEmployeeId==id).OrderByDescending(a=>a.Id).FirstOrDefault();
   var target=assignment==null?id:users.FirstOrDefault(u=>u.Id==assignment.ToUserId&&u.IsActive)?.EmployeeId??0;
   return(Seat:id,Employee:target);
  }).Where(x=>x.Employee!=request.EmployeeId&&active.Contains(x.Employee)&&working.Contains(x.Employee)).ToList();
 }
 public static int? SeatFor(string kind,Approvable request,AppUser user,IEnumerable<int> delegatedFrom,IEnumerable<ApprovalVote> votes,IEnumerable<ApprovalAssignment> assignments,IEnumerable<AppUser> users,IEnumerable<Employee> employees)
 {
  if(!user.IsActive||user.EmployeeId is not {} actor||actor==request.EmployeeId||!employees.Any(e=>e.Id==actor&&e.IsWorking))return null;
  if(votes.Any(v=>v.Kind==kind&&v.RequestId==request.Id&&v.StageIndex==request.StageIndex&&v.ActorEmployeeId==actor))return null;
  var slots=Remaining(kind,request,votes,assignments,users,employees);
  var own=slots.FirstOrDefault(x=>x.Employee==actor);
  if(own.Seat>0)return own.Seat;
  var delegated=delegatedFrom.ToHashSet();
  var seat=slots.FirstOrDefault(x=>delegated.Contains(x.Employee));return seat.Seat>0?seat.Seat:null;
 }
 public static string Kind(Approvable a)=>a switch{LeaveRequest=>"Leave",OvertimeRequest=>"Overtime",AttendanceCorrection=>"Correction",AdvanceRequest=>"Advance",_=>""};
}
