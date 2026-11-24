using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
/// <summary>Single-instance, service-awake maintenance. No external notifications.</summary>
public class TrainingExpiry(IRepository repo,AppClock clock)
{
 public Task AlertAsync()=>repo.AtomicAsync(async()=>
 {
  var today=clock.Today;var until=today.AddDays(30);
  var done=await repo.ToListAsync(repo.Query<TrainingCertificateAlert>().Where(x=>x.OnDate==today));var ids=done.Select(x=>x.CertificateId).ToList();
  var certs=await repo.ToListAsync(repo.Query<TrainingCertificate>().Where(x=>x.ExpiresOn!=null&&x.ExpiresOn<=until&&!ids.Contains(x.Id)));
  if(certs.Count==0)return;
  var users=await repo.ToListAsync(repo.Query<AppUser>().Where(x=>x.IsActive));
  foreach(var c in certs)
  {
   foreach(var u in users.Where(x=>x.EmployeeId==c.EmployeeId||PermissionCache.Has(x.RoleArray,Perm.TalentManage)))repo.Add(new Notification{UserId=u.Id,TitleKey="notif.trainingExpiry",Details=$"Certificate #{c.Id}, employee #{c.EmployeeId}: {c.ExpiresOn:yyyy-MM-dd}",Link="/Talent/Training",CreatedAt=clock.UtcNow});
   repo.Add(new TrainingCertificateAlert{CertificateId=c.Id,OnDate=today,At=clock.UtcNow});
  }
  await repo.SaveAsync();
 });
}
