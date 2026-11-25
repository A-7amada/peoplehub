using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Platform;
namespace PeopleHub.Application.Modules.Employees;
public class EmployeeDocuments(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit,NotificationService notify,AttachmentService attachments)
{
 public async Task<List<EmployeeDocument>> ListAsync(int employeeId)
 {
  if(!me.Can(Perm.EmpManage)&&me.EmployeeId!=employeeId)throw new DomainException("err.forbidden");
  return await repo.ToListAsync(repo.Query<EmployeeDocument>().Where(x=>x.EmployeeId==employeeId).OrderBy(x=>x.ExpiresOn));
 }
 public Task AddAsync(int employeeId,string title,DateTime? expiresOn,string fileName,byte[] data)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.EmpManage))throw new DomainException("err.forbidden");if(string.IsNullOrWhiteSpace(title)||title.Length>150||expiresOn?.Year<2000)throw new DomainException("err.required");
  if(!await repo.AnyAsync(repo.Query<Employee>().Where(x=>x.Id==employeeId)))throw new DomainException("err.notFound");
  var f=attachments.Save(fileName,data);if(f.Token==null)throw new DomainException("err.required");
  // The new attachment may not be queryable until saved; persist it inside the enclosing transaction.
  await repo.SaveAsync();var a=await repo.FirstOrDefaultAsync(repo.Query<Attachment>().Where(x=>x.Token==f.Token));a!.OwnerEmployeeId=employeeId;
  repo.Add(new EmployeeDocument{EmployeeId=employeeId,Title=title.Trim(),AttachmentToken=f.Token,ExpiresOn=expiresOn?.Date,UploadedBy=me.Email,At=clock.UtcNow});audit.Log("AddDocument","Employee",employeeId,title.Trim());await repo.SaveAsync();
 });
 public Task AlertAsync()=>repo.AtomicAsync(async()=>
 {
  var until=clock.Today.AddDays(30);var rows=await repo.ToListAsync(repo.Query<EmployeeDocument>().Where(x=>x.ExpiresOn!=null && x.ExpiresOn<=until && (x.LastAlertDate==null || x.LastAlertDate<clock.Today)));
  foreach(var x in rows){await notify.NotifyEmployeeAsync(x.EmployeeId,"notif.documentExpiry",$"{x.Title}: {x.ExpiresOn:yyyy-MM-dd}",$"/Employees/Profile/{x.EmployeeId}?tab=documents");await notify.NotifyPermAsync(Perm.EmpManage,"notif.documentExpiry",$"employee #{x.EmployeeId}: {x.Title}, {x.ExpiresOn:yyyy-MM-dd}",$"/Employees/Profile/{x.EmployeeId}?tab=documents");x.LastAlertDate=clock.Today;}await repo.SaveAsync();
 });
}
