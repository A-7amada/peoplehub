using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record CaseInvestigator(int UserId,string Email);
public record EmployeeCaseView(List<EmployeeCase> Cases,List<EmployeeCaseEvent> Events,List<EmployeeCaseAccess>? Access=null);
public class EmployeeRelations(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 private void Need(){if(!me.Can(Perm.RelationsManage))throw new DomainException("err.forbidden");}
 public async Task<bool> CanEnterAsync()=>me.Can(Perm.RelationsManage)||(await GrantedCaseIdsAsync()).Count>0;
 private async Task<List<int>> GrantedCaseIdsAsync()
 {
  var user=await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x=>x.Id==me.UserId&&x.IsActive));
  if(user==null||user.EmployeeId!=null&&!await repo.AnyAsync(repo.Query<Employee>().Where(x=>x.Id==user.EmployeeId&&(x.Status==EmployeeStatus.Active||x.Status==EmployeeStatus.OnLeave))))return [];
  var ids=(await repo.ToListAsync(repo.Query<EmployeeCaseAccess>().Where(x=>x.UserId==me.UserId))).GroupBy(x=>x.CaseId).Where(g=>g.OrderByDescending(x=>x.Id).First().Granted).Select(g=>g.Key).ToList();
  return await repo.ToListAsync(repo.Query<EmployeeCase>().Where(x=>ids.Contains(x.Id)&&(user.EmployeeId==null||x.EmployeeId!=user.EmployeeId)).Select(x=>x.Id));
 }
 private async Task<bool> CanInvestigateAsync(int caseId)=>me.Can(Perm.RelationsManage)||(await GrantedCaseIdsAsync()).Contains(caseId);
 public async Task<EmployeeCaseView> GetAsync()
 {
  bool all=me.Can(Perm.RelationsManage);var ids=all?new List<int>():await GrantedCaseIdsAsync();
  if(!all&&ids.Count==0)throw new DomainException("err.forbidden");
  return new(await repo.ToListAsync(repo.Query<EmployeeCase>().Where(x=>all||ids.Contains(x.Id)).OrderByDescending(x=>x.Id)),await repo.ToListAsync(repo.Query<EmployeeCaseEvent>().Where(x=>all||ids.Contains(x.CaseId)).OrderBy(x=>x.Id)),all?await repo.ToListAsync(repo.Query<EmployeeCaseAccess>().OrderBy(x=>x.Id)):[]);
 }
 public async Task<List<CaseInvestigator>> CandidatesAsync()
 {
  Need();var employees=await repo.ToListAsync(repo.Query<Employee>().Select(x=>new {x.Id,x.Status}));
  return (await repo.ToListAsync(repo.Query<AppUser>().Where(x=>x.IsActive).OrderBy(x=>x.Email))).Where(u=>u.EmployeeId==null||employees.Any(e=>e.Id==u.EmployeeId&&(e.Status==EmployeeStatus.Active||e.Status==EmployeeStatus.OnLeave))).Select(u=>new CaseInvestigator(u.Id,u.Email)).ToList();
 }
 public Task SetAccessAsync(int caseId,int userId,bool granted,string reason)=>repo.AtomicAsync(async()=>
 {
  Need();if(string.IsNullOrWhiteSpace(reason)||reason.Length>2000)throw new DomainException("err.reasonRequired");
  var c=await repo.FirstOrDefaultAsync(repo.Query<EmployeeCase>().Where(x=>x.Id==caseId))??throw new DomainException("err.notFound");
  var user=await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x=>x.Id==userId))??throw new DomainException("err.notFound");
  if(granted&&(!user.IsActive||user.EmployeeId==c.EmployeeId||user.EmployeeId!=null&&!await repo.AnyAsync(repo.Query<Employee>().Where(x=>x.Id==user.EmployeeId&&(x.Status==EmployeeStatus.Active||x.Status==EmployeeStatus.OnLeave)))))throw new DomainException("err.forbidden");
  var latest=(await repo.ToListAsync(repo.Query<EmployeeCaseAccess>().Where(x=>x.CaseId==caseId&&x.UserId==userId))).OrderByDescending(x=>x.Id).FirstOrDefault();
  if((latest?.Granted??false)==granted)throw new DomainException("err.forbidden");
  c.ChangeStamp=Guid.NewGuid().ToString("N");
  repo.Add(new EmployeeCaseAccess{CaseId=caseId,UserId=userId,Granted=granted,Reason=reason.Trim(),ActorUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});
  audit.Log("CaseAccess","EmployeeCase",caseId,"Explicit per-case investigator access changed; identities and reason withheld from general audit");await repo.SaveAsync();
 });
 public Task CreateAsync(int employeeId,string kind,string title,string statement)=>repo.AtomicAsync(async()=>
 {
  Need();if(kind is not("Grievance" or "Warning" or "Investigation")||string.IsNullOrWhiteSpace(title)||title.Length>200||string.IsNullOrWhiteSpace(statement)||statement.Length>8000)throw new DomainException("err.required");if(!await repo.AnyAsync(repo.Query<Employee>().Where(x=>x.Id==employeeId)))throw new DomainException("err.notFound");
  repo.Add(new EmployeeCase{EmployeeId=employeeId,Kind=kind,Title=title.Trim(),Statement=statement.Trim(),CreatedByUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});audit.Log("Create","EmployeeCase",employeeId,"Confidential internal case recorded; contents withheld from general audit");await repo.SaveAsync();
 });
 public Task RecordAsync(int caseId,string kind,string note)=>repo.AtomicAsync(async()=>
 {
  var c=await repo.FirstOrDefaultAsync(repo.Query<EmployeeCase>().Where(x=>x.Id==caseId))??throw new DomainException("err.notFound");
  if(!await CanInvestigateAsync(caseId))throw new DomainException("err.forbidden");
  if(!me.Can(Perm.RelationsManage)&&kind!="Investigation")throw new DomainException("err.forbidden");
  if(string.IsNullOrWhiteSpace(note)||note.Length>8000)throw new DomainException("err.reasonRequired");var events=await repo.ToListAsync(repo.Query<EmployeeCaseEvent>().Where(x=>x.CaseId==caseId).OrderBy(x=>x.Id));if(events.Any(x=>x.Kind=="Closed"))throw new DomainException("err.forbidden");
  var proposal=events.LastOrDefault(x=>x.Kind=="ProposedDiscipline");var decision=events.LastOrDefault(x=>x.Kind is "ApprovedDiscipline" or "RejectedDiscipline");bool pending=proposal!=null&&(decision==null||decision.Id<proposal.Id);
  if(kind=="Investigation"){}
  else if(kind=="ProposedDiscipline"){if(pending||events.Any(x=>x.Kind=="ApprovedDiscipline"))throw new DomainException("err.forbidden");}
  else if(kind is "ApprovedDiscipline" or "RejectedDiscipline"){if(!pending||proposal!.ActorUserId==me.UserId||c.CreatedByUserId==me.UserId||c.EmployeeId==me.EmployeeId)throw new DomainException("err.forbidden");}
  else if(kind=="Closed"){if(pending)throw new DomainException("err.forbidden");}
  else throw new DomainException("err.required");
  c.ChangeStamp=Guid.NewGuid().ToString("N");repo.Add(new EmployeeCaseEvent{CaseId=c.Id,Kind=kind,Note=note.Trim(),ActorUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});audit.Log("Record","EmployeeCase",c.Id,"Internal case evidence added; contents withheld. No employee, payroll or notice effect");await repo.SaveAsync();
 });
}
