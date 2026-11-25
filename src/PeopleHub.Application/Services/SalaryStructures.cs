using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
namespace PeopleHub.Application.Services;
public class SalaryStructures(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 private void Need(){if(!me.Can(Perm.EmpPay)||!me.Can(Perm.EmpSalary))throw new DomainException("err.forbidden");}
 public async Task<List<SalaryStructure>> ListAsync(){Need();return await repo.ToListAsync(repo.Query<SalaryStructure>().OrderByDescending(x=>x.Id));}
 public Task<List<SalaryStructureApplication>> HistoryAsync(){Need();return repo.ToListAsync(repo.Query<SalaryStructureApplication>().OrderByDescending(x=>x.Id).Take(100));}
 public Task CreateAsync(string name,int grade,decimal basic,string componentAmounts,string reason,EmploymentType? category=null)=>repo.AtomicAsync(async()=>
 {
  Need();if(category!=null&&!Enum.IsDefined(category.Value))throw new DomainException("err.required");if(componentAmounts?.Length>10000)throw new DomainException("err.required");if(string.IsNullOrWhiteSpace(name)||name.Length>160||grade<1||grade>100||basic<0||basic>100000000||string.IsNullOrWhiteSpace(reason)||reason.Length>2000)throw new DomainException("err.required");
  var parts=new Dictionary<int,decimal>();foreach(var row in (componentAmounts??"").Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)){var p=row.Split('=');if(p.Length!=2||!int.TryParse(p[0],out int id)||!decimal.TryParse(p[1],System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out decimal value)||value<0||value>100000000||!parts.TryAdd(id,value))throw new DomainException("err.required");}
  var valid=await repo.ToListAsync(repo.Query<SalaryComponent>().Where(x=>x.IsFixed));if(parts.Keys.Any(id=>valid.All(c=>c.Id!=id)))throw new DomainException("err.required");
  var json=JsonSerializer.Serialize(parts.OrderBy(x=>x.Key).ToDictionary(x=>x.Key,x=>x.Value));repo.Add(new SalaryStructure{Name=name.Trim(),JobGrade=grade,EmploymentCategory=category,Basic=basic,ComponentsJson=json,Reason=reason.Trim(),Actor=me.Email,At=clock.UtcNow});audit.Log("Create","SalaryStructure",grade,"Immutable grade structure recorded; no employee/payroll change");await repo.SaveAsync();
 });
 public Task ApplyAsync(int employeeId,int structureId,string reason)=>repo.AtomicAsync(async()=>
 {
  Need();if(!me.Can(Perm.EmpManage)||string.IsNullOrWhiteSpace(reason)||reason.Length>2000)throw new DomainException("err.forbidden");
  var e=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==employeeId))??throw new DomainException("err.notFound");var s=await repo.FirstOrDefaultAsync(repo.Query<SalaryStructure>().Where(x=>x.Id==structureId))??throw new DomainException("err.notFound");
  if(!e.IsWorking||e.JobTitle?.Grade!=s.JobGrade||s.EmploymentCategory!=null&&e.EmploymentType!=s.EmploymentCategory||await repo.AnyAsync(repo.Query<EmploymentChange>().Where(x=>x.EmployeeId==employeeId&&!x.Cancelled&&x.AppliedAt==null)))throw new DomainException("err.forbidden");
  var components=JsonSerializer.Deserialize<Dictionary<int,decimal>>(s.ComponentsJson)??[];var valid=await repo.ToListAsync(repo.Query<SalaryComponent>().Where(x=>x.IsFixed));if(components.Keys.Any(id=>valid.All(c=>c.Id!=id)))throw new DomainException("err.required");
  var current=await repo.ToListAsync(repo.Query<EmployeeSalaryComponent>().Where(x=>x.EmployeeId==employeeId));
  var before=JsonSerializer.Serialize(new{Basic=e.BasicSalary,Components=current.Select(x=>new{x.ComponentId,x.Amount})});
  var prior=await repo.ToListAsync(repo.Query<EmployeeAssignment>().Where(x=>x.EmployeeId==employeeId).OrderBy(x=>x.EffectiveFrom).ThenBy(x=>x.Id));
  if(prior.Count==0){var baseline=EmployeeAssignment.Capture(e,clock.Today,me.Email,"Observed pre-change baseline; earlier effective dates unknown");baseline.EffectiveTo=clock.Today;repo.Add(baseline);}else prior.Last().EffectiveTo=clock.Today;
  e.BasicSalary=s.Basic;e.LeaveRequestStamp=Guid.NewGuid().ToString("N");foreach(var part in components){var row=current.FirstOrDefault(x=>x.ComponentId==part.Key);if(row==null)repo.Add(new EmployeeSalaryComponent{EmployeeId=employeeId,ComponentId=part.Key,Amount=part.Value});else row.Amount=part.Value;}
  var after=JsonSerializer.Serialize(new{Basic=s.Basic,Components=components,EmploymentCategory=s.EmploymentCategory});repo.Add(new SalaryStructureApplication{EmployeeId=employeeId,StructureId=s.Id,BeforeJson=before,AfterJson=after,Reason=reason.Trim(),Actor=me.Email,At=clock.UtcNow,Sha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(before+"\n"+after)))});
  repo.Add(EmployeeAssignment.Capture(e,clock.Today,me.Email,"Explicit grade structure: "+reason.Trim()));repo.Add(new CareerEvent{EmployeeId=employeeId,Type=CareerEventType.SalaryChange,Date=clock.Today,Details="Grade structure #"+s.Id+": "+reason.Trim(),RecordedBy=me.Email});audit.Log("Apply","SalaryStructure",s.Id,"Applied explicitly to employee #"+employeeId+"; historical payroll unchanged");await repo.SaveAsync();
 });
}
