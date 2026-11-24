using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record LifecycleView(List<LifecycleTask> Tasks,List<EmployeeDependent> Dependents,EmployeeProbation? Probation,List<HrFieldDefinition> Fields,List<EmployeeFieldValue> Values);
public class EmployeeLifecycle(IRepository repo,ICurrentUser me,AccessScope scope,AppClock clock,AuditService audit,NotificationService notify)
{
 private void Hr(){if(!me.Can(Perm.EmpManage))throw new DomainException("err.forbidden");}
 private async Task Exists(int id){if(!await repo.AnyAsync(repo.Query<Employee>().Where(e=>e.Id==id)))throw new DomainException("err.notFound");}
 public async Task<LifecycleView> GetAsync(int employeeId)
 {
  if(!await scope.CanSeeAsync(employeeId))throw new DomainException("err.forbidden");
  bool privateAccess=me.Can(Perm.EmpManage) || me.EmployeeId==employeeId;
  return new(await repo.ToListAsync(repo.Query<LifecycleTask>().Where(x=>x.EmployeeId==employeeId).OrderBy(x=>x.DueOn)),privateAccess?await repo.ToListAsync(repo.Query<EmployeeDependent>().Where(x=>x.EmployeeId==employeeId)):[],await repo.FirstOrDefaultAsync(repo.Query<EmployeeProbation>().Where(x=>x.EmployeeId==employeeId)),me.Can(Perm.EmpManage)?await repo.ToListAsync(repo.Query<HrFieldDefinition>()):[],me.Can(Perm.EmpManage)?await repo.ToListAsync(repo.Query<EmployeeFieldValue>().Where(x=>x.EmployeeId==employeeId)):[]);
 }
 public async Task AddDependentAsync(int employeeId,string name,string relationship,DateTime? birthDate)
 {
  Hr();await Exists(employeeId);if(string.IsNullOrWhiteSpace(name)||name.Length>150||relationship is not("Spouse" or "Child" or "Other")||birthDate>clock.Today)throw new DomainException("err.required");
  repo.Add(new EmployeeDependent{EmployeeId=employeeId,Name=name.Trim(),Relationship=relationship,BirthDate=birthDate?.Date,Actor=me.Email,At=clock.UtcNow});audit.Log("AddDependent","Employee",employeeId,"Dependent record added");await repo.SaveAsync();
 }
 public async Task RemoveDependentAsync(int id,string reason)
 {
  Hr();if(string.IsNullOrWhiteSpace(reason))throw new DomainException("err.reasonRequired");var d=await repo.FirstOrDefaultAsync(repo.Query<EmployeeDependent>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");repo.Remove(d);audit.Log("RemoveDependent","Employee",d.EmployeeId,$"dependent #{id}: {reason.Trim()}");await repo.SaveAsync();
 }
 public Task AddTaskAsync(int employeeId,string process,string title,int? assignedEmployeeId,DateTime dueOn)=>repo.AtomicAsync(async()=>
 {
  Hr();await Exists(employeeId);if(process is not("Onboarding" or "Offboarding" or "Clearance")||string.IsNullOrWhiteSpace(title)||title.Length>250||dueOn.Year<2000||dueOn.Year>9998)throw new DomainException("err.required");if(assignedEmployeeId!=null)await Exists(assignedEmployeeId.Value);
  var t=new LifecycleTask{EmployeeId=employeeId,Process=process,Title=title.Trim(),AssignedEmployeeId=assignedEmployeeId,DueOn=dueOn.Date};repo.Add(t);audit.Log("AddLifecycleTask","Employee",employeeId,$"{process}: {title.Trim()}");if(assignedEmployeeId!=null)await notify.NotifyEmployeeAsync(assignedEmployeeId.Value,"notif.lifecycle",title.Trim(),$"/Employees/Profile/{employeeId}?tab=lifecycle");await repo.SaveAsync();
 });
 public async Task CompleteTaskAsync(int id,string note)
 {
  var t=await repo.FirstOrDefaultAsync(repo.Query<LifecycleTask>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");if(!me.Can(Perm.EmpManage) && me.EmployeeId!=t.AssignedEmployeeId)throw new DomainException("err.forbidden");if(t.Status!="Open"||string.IsNullOrWhiteSpace(note))throw new DomainException("err.required");t.Status="Complete";t.CompletionNote=note.Trim();t.CompletedAt=clock.UtcNow;t.CompletedBy=me.Email;audit.Log("CompleteLifecycleTask","Employee",t.EmployeeId,$"task #{id}: {note.Trim()}");await repo.SaveAsync();
 }
 public async Task SetProbationAsync(int employeeId,DateTime endsOn,string reason)
 {
  Hr();await Exists(employeeId);var e=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==employeeId));if(endsOn.Date<e!.HireDate.Date||endsOn.Year>9998||string.IsNullOrWhiteSpace(reason))throw new DomainException("err.required");var p=await repo.FirstOrDefaultAsync(repo.Query<EmployeeProbation>().Where(x=>x.EmployeeId==employeeId));if(p?.Decision!="Pending"&&p!=null)throw new DomainException("err.forbidden");if(p==null){p=new(){EmployeeId=employeeId};repo.Add(p);}p.EndsOn=endsOn.Date;p.Actor=me.Email;p.Reason=reason.Trim();audit.Log("SetProbation","Employee",employeeId,$"ends {endsOn:yyyy-MM-dd}: {reason.Trim()}");await repo.SaveAsync();
 }
 public async Task DecideProbationAsync(int employeeId,string decision,string reason)
 {
  Hr();if(decision is not("Confirmed" or "NotConfirmed")||string.IsNullOrWhiteSpace(reason))throw new DomainException("err.required");var p=await repo.FirstOrDefaultAsync(repo.Query<EmployeeProbation>().Where(x=>x.EmployeeId==employeeId))??throw new DomainException("err.notFound");if(p.Decision!="Pending")throw new DomainException("err.forbidden");p.Decision=decision;p.Reason=reason.Trim();p.Actor=me.Email;p.DecisionAt=clock.UtcNow;audit.Log("ProbationDecision","Employee",employeeId,$"{decision}: {reason.Trim()}");await repo.SaveAsync();
 }
 public async Task AddFieldAsync(string label,string? labelAr,string kind)
 {
  Hr();if(string.IsNullOrWhiteSpace(label)||label.Length>100||kind is not("Text" or "Date" or "Number"))throw new DomainException("err.required");if(await repo.AnyAsync(repo.Query<HrFieldDefinition>().Where(x=>x.Label==label.Trim())))throw new DomainException("err.required");repo.Add(new HrFieldDefinition{Label=label.Trim(),LabelAr=labelAr?.Trim()??"",Kind=kind});audit.Log("AddCustomField","HRField",label.Trim(),kind);await repo.SaveAsync();
 }
 public async Task SetFieldAsync(int employeeId,int fieldId,string value)
 {
  Hr();await Exists(employeeId);var f=await repo.FirstOrDefaultAsync(repo.Query<HrFieldDefinition>().Where(x=>x.Id==fieldId && x.Active))??throw new DomainException("err.notFound");value=(value??"").Trim();if(value.Length>1000 || (value.Length>0&&f.Kind=="Date"&&!DateTime.TryParseExact(value,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out _)) || (value.Length>0&&f.Kind=="Number"&&!decimal.TryParse(value,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out _)))throw new DomainException("err.required");var v=await repo.FirstOrDefaultAsync(repo.Query<EmployeeFieldValue>().Where(x=>x.EmployeeId==employeeId && x.FieldId==fieldId));if(v==null){v=new(){EmployeeId=employeeId,FieldId=fieldId};repo.Add(v);}v.Value=value;audit.Log("SetCustomField","Employee",employeeId,$"field #{fieldId} changed");await repo.SaveAsync();
 }
}
