using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Employees;
public record AssetView(List<CompanyAsset> Assets,List<AssetMovement> History);
public class AssetService(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 void Hr(){if(!me.Can(Perm.EmpManage))throw new DomainException("err.forbidden");}
 public async Task<AssetView> GetAsync(int employeeId)
 {
  if(!me.Can(Perm.EmpManage)&&me.EmployeeId!=employeeId)throw new DomainException("err.forbidden");
  bool hr=me.Can(Perm.EmpManage);var history=await repo.ToListAsync(repo.Query<AssetMovement>().Where(x=>x.EmployeeId==employeeId).OrderByDescending(x=>x.At).Take(100));
  return new(await repo.ToListAsync(repo.Query<CompanyAsset>().Where(x=>x.AssignedEmployeeId==employeeId|| (hr&&x.Status=="Available"))),history);
 }
 public async Task<List<CompanyAsset>> InventoryAsync(){Hr();return await repo.ToListAsync(repo.Query<CompanyAsset>().OrderBy(x=>x.Tag));}
 public Task RegisterAsync(string tag,string name,string serial,string condition)=>repo.AtomicAsync(async()=>
 {
  Hr();tag=tag.Trim().ToUpperInvariant();if(tag.Length is <1 or >50||string.IsNullOrWhiteSpace(name)||name.Length>150||serial.Length>100||string.IsNullOrWhiteSpace(condition)||condition.Length>500)throw new DomainException("err.required");if(await repo.AnyAsync(repo.Query<CompanyAsset>().Where(x=>x.Tag==tag)))throw new DomainException("err.required");
  var asset=new CompanyAsset{Tag=tag,Name=name.Trim(),Serial=serial.Trim(),Condition=condition.Trim()};repo.Add(asset);await repo.SaveAsync();Record(asset,null,"Registered",condition,"Initial inventory record");await repo.SaveAsync();
 });
 void Record(CompanyAsset a,int? employeeId,string kind,string condition,string evidence){repo.Add(new AssetMovement{AssetId=a.Id,EmployeeId=employeeId,Kind=kind,Condition=condition.Trim(),Evidence=evidence.Trim(),Actor=me.Email,At=clock.UtcNow});audit.Log("Asset"+kind,"Asset",a.Id,$"{a.Tag}; employee #{employeeId}: {evidence.Trim()}");}
 public Task MoveAsync(int assetId,string action,int? employeeId,DateTime? dueOn,string condition,string evidence)=>repo.AtomicAsync(async()=>
 {
  Hr();if(string.IsNullOrWhiteSpace(condition)||condition.Length>500||string.IsNullOrWhiteSpace(evidence)||evidence.Length>1000||dueOn?.Date<clock.Today)throw new DomainException("err.required");
  var a=await repo.FirstOrDefaultAsync(repo.Query<CompanyAsset>().Where(x=>x.Id==assetId))??throw new DomainException("err.notFound");int? holder=a.AssignedEmployeeId;
  switch(action)
  {
   case "Assign":
    if(a.Status!="Available"||employeeId==null)throw new DomainException("err.forbidden");var e=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==employeeId))??throw new DomainException("err.notFound");if(!e.IsWorking)throw new DomainException("err.forbidden");a.AssignedEmployeeId=employeeId;a.DueOn=dueOn?.Date;a.Status="Assigned";holder=employeeId;break;
   case "Return":if(a.Status!="Assigned"||a.AssignedEmployeeId!=employeeId)throw new DomainException("err.forbidden");a.AssignedEmployeeId=null;a.DueOn=null;a.Status="Available";break;
   case "Maintenance":if(a.Status!="Available")throw new DomainException("err.forbidden");a.Status="Maintenance";break;
   case "Release":if(a.Status!="Maintenance")throw new DomainException("err.forbidden");a.Status="Available";break;
   case "Retire":if(a.Status!="Available")throw new DomainException("err.forbidden");a.Status="Retired";break;
   default:throw new DomainException("err.required");
  }
  a.Condition=condition.Trim();Record(a,holder,action,condition,evidence);await repo.SaveAsync();
 });
}
