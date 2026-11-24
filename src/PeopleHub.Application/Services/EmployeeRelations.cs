using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record EmployeeCaseView(List<EmployeeCase> Cases,List<EmployeeCaseEvent> Events);
public class EmployeeRelations(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 private void Need(){if(!me.Can(Perm.RelationsManage))throw new DomainException("err.forbidden");}
 public async Task<EmployeeCaseView> GetAsync(){Need();return new(await repo.ToListAsync(repo.Query<EmployeeCase>().OrderByDescending(x=>x.Id)),await repo.ToListAsync(repo.Query<EmployeeCaseEvent>().OrderBy(x=>x.Id)));}
 public Task CreateAsync(int employeeId,string kind,string title,string statement)=>repo.AtomicAsync(async()=>
 {
  Need();if(kind is not("Grievance" or "Warning" or "Investigation")||string.IsNullOrWhiteSpace(title)||title.Length>200||string.IsNullOrWhiteSpace(statement)||statement.Length>8000)throw new DomainException("err.required");if(!await repo.AnyAsync(repo.Query<Employee>().Where(x=>x.Id==employeeId)))throw new DomainException("err.notFound");
  repo.Add(new EmployeeCase{EmployeeId=employeeId,Kind=kind,Title=title.Trim(),Statement=statement.Trim(),CreatedByUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});audit.Log("Create","EmployeeCase",employeeId,"Confidential internal case recorded; contents withheld from general audit");await repo.SaveAsync();
 });
 public Task RecordAsync(int caseId,string kind,string note)=>repo.AtomicAsync(async()=>
 {
  Need();if(string.IsNullOrWhiteSpace(note)||note.Length>8000)throw new DomainException("err.reasonRequired");var c=await repo.FirstOrDefaultAsync(repo.Query<EmployeeCase>().Where(x=>x.Id==caseId))??throw new DomainException("err.notFound");var events=await repo.ToListAsync(repo.Query<EmployeeCaseEvent>().Where(x=>x.CaseId==caseId).OrderBy(x=>x.Id));if(events.Any(x=>x.Kind=="Closed"))throw new DomainException("err.forbidden");
  var proposal=events.LastOrDefault(x=>x.Kind=="ProposedDiscipline");var decision=events.LastOrDefault(x=>x.Kind is "ApprovedDiscipline" or "RejectedDiscipline");bool pending=proposal!=null&&(decision==null||decision.Id<proposal.Id);
  if(kind=="Investigation"){}
  else if(kind=="ProposedDiscipline"){if(pending||events.Any(x=>x.Kind=="ApprovedDiscipline"))throw new DomainException("err.forbidden");}
  else if(kind is "ApprovedDiscipline" or "RejectedDiscipline"){if(!pending||proposal!.ActorUserId==me.UserId||c.CreatedByUserId==me.UserId||c.EmployeeId==me.EmployeeId)throw new DomainException("err.forbidden");}
  else if(kind=="Closed"){if(pending)throw new DomainException("err.forbidden");}
  else throw new DomainException("err.required");
  c.ChangeStamp=Guid.NewGuid().ToString("N");repo.Add(new EmployeeCaseEvent{CaseId=c.Id,Kind=kind,Note=note.Trim(),ActorUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});audit.Log("Record","EmployeeCase",c.Id,"Internal case evidence added; contents withheld. No employee, payroll or notice effect");await repo.SaveAsync();
 });
}
