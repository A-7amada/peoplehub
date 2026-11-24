using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public static class HolidayCalendar
{
 public static bool Applies(Holiday h,Employee e,DateTime day,IEnumerable<EmployeeAssignment> assignments){var a=assignments.Where(x=>x.EmployeeId==e.Id&&x.EffectiveFrom<=day.Date&&(x.EffectiveTo==null||day.Date<x.EffectiveTo)).OrderByDescending(x=>x.EffectiveFrom).ThenByDescending(x=>x.Id).FirstOrDefault();return (h.BranchId==null||h.BranchId==(a?.BranchId??e.BranchId))&&(h.DepartmentId==null||h.DepartmentId==(a?.DepartmentId??e.DepartmentId));}
}
public class HolidayService(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 public Task<List<Holiday>> ListAsync()=>repo.ToListAsync(repo.Query<Holiday>().OrderBy(x=>x.Date));
 public async Task AddAsync(DateTime date,string name,string? nameAr,int? branchId,int? departmentId,string reason)
 {
  if(!me.Can(Perm.OrgManage))throw new DomainException("err.forbidden");if(date.Date<=clock.Today||date.Year>9998||string.IsNullOrWhiteSpace(name)||name.Length>150||string.IsNullOrWhiteSpace(reason))throw new DomainException("err.required");if(branchId!=null&&!await repo.AnyAsync(repo.Query<Branch>().Where(x=>x.Id==branchId))||departmentId!=null&&!await repo.AnyAsync(repo.Query<Department>().Where(x=>x.Id==departmentId)))throw new DomainException("err.notFound");
  if(await repo.AnyAsync(repo.Query<Holiday>().Where(x=>x.Date==date.Date&&x.BranchId==branchId&&x.DepartmentId==departmentId)))throw new DomainException("err.required");repo.Add(new Holiday{Date=date.Date,Name=name.Trim(),NameAr=nameAr?.Trim()??"",BranchId=branchId,DepartmentId=departmentId});audit.Log("AddHoliday","Holiday",date.ToString("yyyy-MM-dd"),$"branch #{branchId}, department #{departmentId}: {reason.Trim()}");await repo.SaveAsync();
 }
}
