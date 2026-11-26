using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Employees;
public class RehireService(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 static bool Final(EmployeeStatus s)=>s is EmployeeStatus.Terminated or EmployeeStatus.Resigned or EmployeeStatus.Retired;
 public async Task<List<RehireRequest>> ListAsync(int employeeId,AccessScope scope)
 {
  if(!me.Can(Perm.EmpManage)||!await scope.CanSeeAsync(employeeId))throw new DomainException("err.forbidden");
  return await repo.ToListAsync(repo.Query<RehireRequest>().Where(x=>x.EmployeeId==employeeId).OrderByDescending(x=>x.Id));
 }
 /// <summary>Any HR user may ask; only the second HR Manager can approve.</summary>
 public Task RequestAsync(int employeeId,string? reason)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.EmpManage))throw new DomainException("err.forbidden");
  if(string.IsNullOrWhiteSpace(reason)||reason.Length>1000)throw new DomainException("err.reasonRequired");
  var e=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==employeeId))??throw new DomainException("err.notFound");
  if(!Final(e.Status))throw new DomainException("err.rehireNotFinal");
  if(await repo.AnyAsync(repo.Query<RehireRequest>().Where(x=>x.EmployeeId==employeeId&&x.Status=="Pending")))throw new DomainException("err.rehirePending");
  repo.Add(new RehireRequest{EmployeeId=employeeId,FromStatus=e.Status,Reason=reason.Trim(),RequestedByUserId=me.UserId,RequestedBy=me.Email,RequestedAt=clock.UtcNow});
  audit.Log("RehireRequest","Employee",e.Code,$"from {e.Status}; {reason.Trim()}");
  await repo.SaveAsync();
 });
 public Task DecideAsync(int requestId,bool approve,string? note)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.EmpManage)||!me.IsInRole(Roles.HRManager))throw new DomainException("err.rehireManagerOnly");
  if(string.IsNullOrWhiteSpace(note)||note.Length>1000)throw new DomainException("err.reasonRequired");
  var r=await repo.FirstOrDefaultAsync(repo.Query<RehireRequest>().Where(x=>x.Id==requestId))??throw new DomainException("err.notFound");
  if(r.Status!="Pending")throw new DomainException("err.rehireDecided");
  if(r.RequestedByUserId==me.UserId)throw new DomainException("err.rehireSelf");
  var e=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==r.EmployeeId))??throw new DomainException("err.notFound");
  r.DecidedByUserId=me.UserId;r.DecidedBy=me.Email;r.DecidedAt=clock.UtcNow;r.DecisionNote=note.Trim();
  if(approve)
  {
   repo.Add(e.Rehire(me.Email,$"Rehire approved: {r.Reason} (requested by {r.RequestedBy})",clock.UtcNow));
   e.LeaveRequestStamp=Guid.NewGuid().ToString("N");
   r.Status="Approved";
  }
  else r.Status="Rejected";
  audit.Log(approve?"RehireApprove":"RehireReject","Employee",e.Code,$"request #{r.Id}: {note.Trim()}");
  await repo.SaveAsync();
 });
}
