using PeopleHub.Domain.Entities;
namespace PeopleHub.Application.Services;
public record ApprovalAssignee(int Id,string Email,int? EmployeeId);
public partial class ApprovalService
{
 public async Task<List<ApprovalAssignee>> AssigneeCandidatesAsync()
 {
  if(!me.Can(Perm.AdmFlows))return [];
  var working=(await repo.ToListAsync(repo.Query<Employee>())).Where(e=>e.IsWorking).Select(e=>e.Id).ToHashSet();
  return (await repo.ToListAsync(repo.Query<AppUser>().Where(x=>x.IsActive).OrderBy(x=>x.Email))).Where(x=>x.EmployeeId==null||working.Contains(x.EmployeeId.Value)).Select(x=>new ApprovalAssignee(x.Id,x.Email,x.EmployeeId)).ToList();
 }
 public Task ReassignAsync(string kind,int id,int stageIndex,int toUserId,string reason)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.AdmFlows))throw new DomainException("err.forbidden");
  if(string.IsNullOrWhiteSpace(reason)||reason.Length>2000)throw new DomainException("err.reasonRequired");
  var request=await LoadAsync(kind,id);
  if(request.Status!=RequestStatus.Pending||request.CurrentStage==null||request.StageIndex!=stageIndex)throw new DomainException("err.notPending");
  var target=await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x=>x.Id==toUserId&&x.IsActive))??throw new DomainException("err.notFound");
  if(target.EmployeeId is {} employeeId){var employee=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==employeeId));if(employee?.IsWorking!=true)throw new DomainException("err.terminated");}
  if(target.EmployeeId==request.EmployeeId || Assigned(request)?.ToUserId==toUserId)throw new DomainException("err.forbidden");
  request.AssignmentStamp=Guid.NewGuid().ToString("N");
  repo.Add(new ApprovalAssignment{Kind=kind,RequestId=id,StageIndex=stageIndex,ToUserId=target.Id,ToEmail=target.Email,Actor=me.Email,Reason=reason.Trim(),At=clock.UtcNow});
  repo.Add(new ApprovalAction{Kind=kind,RequestId=id,Actor=me.Email,Action="Reassigned:"+request.CurrentStage,Comment=target.Email+": "+reason.Trim(),At=clock.UtcNow});
  audit.Log("ReassignApproval",kind,id,$"stage {stageIndex} to {target.Email}: {reason.Trim()}");
  repo.Add(new Notification{UserId=target.Id,TitleKey="notif.reassigned",Details=$"{kind} #{id}: {reason.Trim()}",Link="/Requests?scope=inbox",CreatedAt=clock.UtcNow});await repo.SaveAsync();
 });
}
