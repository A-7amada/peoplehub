using System.Globalization;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Employees;

/// <summary>US-PH2-016 HR letters. Employees request an employment confirmation for themselves; HR with employee-management permission issues
/// or declines it. The issuer cannot be the requester. Salary appears only when the employee asked for it and the issuer may see salaries.</summary>
public class LetterService(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 public const int MaxOpenRequests=3;
 public async Task<List<EmployeeLetter>> ListAsync(int employeeId,AccessScope scope)
 {
  var self=me.EmployeeId==employeeId;
  if(!self&&(!me.Can(Perm.EmpManage)||!await scope.CanSeeAsync(employeeId)))throw new DomainException("err.forbidden");
  return await repo.ToListAsync(repo.Query<EmployeeLetter>().Where(x=>x.EmployeeId==employeeId).OrderByDescending(x=>x.Id));
 }
 public Task RequestAsync(string? addressedTo,bool includeSalary)=>repo.AtomicAsync(async()=>
 {
  var eid=me.EmployeeId??throw new DomainException("err.forbidden");
  if(string.IsNullOrWhiteSpace(addressedTo)||addressedTo.Trim().Length>150)throw new DomainException("err.required");
  var e=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==eid))??throw new DomainException("err.notFound");
  if(!e.IsWorking)throw new DomainException("err.forbidden");
  if(await repo.CountAsync(repo.Query<EmployeeLetter>().Where(x=>x.EmployeeId==eid&&x.Status=="Requested"))>=MaxOpenRequests)throw new DomainException("err.letterLimit");
  repo.Add(new EmployeeLetter{EmployeeId=eid,AddressedTo=addressedTo.Trim(),IncludeSalary=includeSalary,RequestedByUserId=me.UserId,RequestedBy=me.Email,RequestedAt=clock.UtcNow});
  audit.Log("LetterRequest","Employee",e.Code,$"to {addressedTo.Trim()}; salary={includeSalary}");
  await repo.SaveAsync();
 });
 public Task IssueAsync(int letterId)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.EmpManage))throw new DomainException("err.forbidden");
  var l=await repo.FirstOrDefaultAsync(repo.Query<EmployeeLetter>().Where(x=>x.Id==letterId))??throw new DomainException("err.notFound");
  if(l.Status!="Requested"||l.RequestedByUserId==me.UserId)throw new DomainException("err.forbidden");
  if(l.IncludeSalary&&!me.Can(Perm.EmpSalary))throw new DomainException("err.forbidden");
  var e=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==l.EmployeeId))??throw new DomainException("err.notFound");
  if(!e.IsWorking)throw new DomainException("err.forbidden");
  l.Reference=$"LTR-{l.Id:D5}";l.Status="Issued";l.DecidedByUserId=me.UserId;l.DecidedBy=me.Email;l.DecidedAt=clock.UtcNow;
  (l.BodyEn,l.BodyAr)=Compose(e,l,clock.Today);
  audit.Log("LetterIssue","Employee",e.Code,$"{l.Reference}; salary={l.IncludeSalary}");
  await repo.SaveAsync();
 });
 public Task DeclineAsync(int letterId,string? note)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.EmpManage))throw new DomainException("err.forbidden");
  if(string.IsNullOrWhiteSpace(note)||note.Length>1000)throw new DomainException("err.reasonRequired");
  var l=await repo.FirstOrDefaultAsync(repo.Query<EmployeeLetter>().Where(x=>x.Id==letterId))??throw new DomainException("err.notFound");
  if(l.Status!="Requested")throw new DomainException("err.forbidden");
  l.Status="Declined";l.DecidedByUserId=me.UserId;l.DecidedBy=me.Email;l.DecidedAt=clock.UtcNow;l.DecisionNote=note.Trim();
  audit.Log("LetterDecline","Employee",l.EmployeeId.ToString(),note.Trim());
  await repo.SaveAsync();
 });
 /// <summary>A letter is visible to its employee and to HR who can see that employee; only issued letters can be opened.</summary>
 public async Task<(EmployeeLetter Letter,Employee Employee)> OpenAsync(int letterId,AccessScope scope)
 {
  var l=await repo.FirstOrDefaultAsync(repo.Query<EmployeeLetter>().Where(x=>x.Id==letterId))??throw new DomainException("err.notFound");
  if(me.EmployeeId!=l.EmployeeId&&(!me.Can(Perm.EmpManage)||!await scope.CanSeeAsync(l.EmployeeId)))throw new DomainException("err.forbidden");
  if(l.Status!="Issued")throw new DomainException("err.notFound");
  var e=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==l.EmployeeId))??throw new DomainException("err.notFound");
  return(l,e);
 }
 static string StatusAr(EmployeeStatus s)=>s switch{EmployeeStatus.Active=>"على رأس العمل",EmployeeStatus.OnLeave=>"في إجازة",_=>s.ToString()};
 public static (string En,string Ar) Compose(Employee e,EmployeeLetter l,DateTime today)
 {
  var inv=CultureInfo.InvariantCulture;var date=today.ToString("yyyy-MM-dd",inv);var hire=e.HireDate.ToString("yyyy-MM-dd",inv);
  var title=e.JobTitle?.Name??"";var titleAr=string.IsNullOrEmpty(e.JobTitle?.NameAr)?title:e.JobTitle!.NameAr;
  var dept=e.Department?.Name??"";var deptAr=string.IsNullOrEmpty(e.Department?.NameAr)?dept:e.Department!.NameAr;
  var nameAr=string.IsNullOrWhiteSpace(e.FullNameAr)?e.FullName:e.FullNameAr;
  var en=$"Date: {date}\nReference: {l.Reference}\nTo: {l.AddressedTo}\n\nThis letter confirms that {e.FullName} (employee code {e.Code}) is employed by the company as {title} in the {dept} department. The hire date is {hire}. Current status: {e.Status}.";
  var ar=$"التاريخ: {date}\nالمرجع: {l.Reference}\nإلى: {l.AddressedTo}\n\nتفيد هذه الرسالة بأن {nameAr} (رقم الموظف {e.Code}) يعمل لدى الشركة بوظيفة {titleAr} في قسم {deptAr}. تاريخ التعيين {hire}. الحالة الحالية: {StatusAr(e.Status)}.";
  if(l.IncludeSalary)
  {
   var s=e.BasicSalary.ToString("N2",inv);
   en+=$" Monthly basic salary: {s} EGP.";ar+=$" الراتب الأساسي الشهري: {s} جنيه مصري.";
  }
  en+="\n\nIssued at the employee's request. Demonstration document with fictional data.";
  ar+="\n\nصدرت بناءً على طلب الموظف. مستند توضيحي ببيانات وهمية.";
  return(en,ar);
 }
}
