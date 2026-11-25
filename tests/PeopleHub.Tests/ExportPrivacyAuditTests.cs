using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
/// <summary>SEC-001 export and service audit. The sweep proves what the current report and export code returns to a user with only the general Reports grant. The file list is a ratchet, not proof that every use is guarded.</summary>
public class ExportPrivacyAuditTests
{
 class User(int employee=4,params string[] grants):ICurrentUser{public string Email=>"audit@example.local";public int? EmployeeId=>employee;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>grants.Contains(p);}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static readonly string[] Sentinels=["SENTINEL-NID-29006110000001","SENTINEL-ADDRESS","SENTINEL-IBAN","SENTINEL-BANK","SENTINEL-INS","SENTINEL-NOTE","SENTINEL-REASON"];
 static ReportService R(AppDbContext d,User who){var c=new AppClock(TimeProvider.System);var scope=new AccessScope(d,who);var audit=new AuditService(d,who,c);var notify=new NotificationService(d,who,c);var flows=new WorkflowService(d,who,audit);var emp=new EmployeeService(d,scope,who,audit,c);return new(d,scope,who,new AttendanceService(d,scope,who,audit,notify,c,flows),new ApprovalService(d,scope,who,audit,notify,c,flows,new DelegationService(d,who,audit,c)),emp,new LeaveService(d,scope,who,audit,notify,c,flows),c);}
 [Fact]public async Task ReportsOnlyUserSeesNoSensitiveValuesInAnyStandardReportOrBuilderDataset()
 {
  await using var d=await Db();
  var e=(await d.Set<Employee>().FindAsync(6))!;e.NationalId=Sentinels[0];e.Address=Sentinels[1];e.Iban=Sentinels[2];e.BankName=Sentinels[3];e.InsuranceNumber=Sentinels[4];e.BirthDate=new DateTime(1987,3,3);e.BasicSalary=987654;
  d.Add(new LeaveRequest{EmployeeId=6,LeaveTypeId=1,StartDate=new(2026,12,1),EndDate=new(2026,12,1),Days=1,Reason=Sentinels[6]});
  d.Add(new CareerEvent{EmployeeId=6,Type=CareerEventType.Transfer,Details=Sentinels[5]});await d.SaveAsync();
  var u=new User(4,Perm.ScopeCompany,Perm.Reports);var all=new List<string>();var tables=0;
  foreach(var key in ReportService.ReportKeys)
  {
   try{var t=await R(d,u).BuildAsync(key,2026,11);tables++;all.AddRange(t.Columns);all.AddRange(t.Rows.SelectMany(x=>x));}catch(DomainException){/* denied without a finance grant is fine */}
  }
  foreach(var ds in ReportBuilderService.Columns.Keys)
  {
   var t=await new ReportBuilderService(d,new AccessScope(d,u),u).RunAsync(ds,null,null);tables++;all.AddRange(t.Columns);all.AddRange(t.Rows.SelectMany(x=>x));
  }
  Assert.True(tables>=6,"sweep produced too few tables");
  var text=string.Join("\n",all);
  Assert.Contains(e.FullName,text); // the employee is in scope, so the absence of private values is meaningful
  foreach(var s in Sentinels)Assert.DoesNotContain(s,text);
  Assert.DoesNotContain("987654",text);Assert.DoesNotContain("1987-03-03",text);
  Assert.DoesNotContain("salary",all,StringComparer.OrdinalIgnoreCase);
 }
 [Fact]public async Task SalaryColumnAppearsOnlyWithSalaryGrant()
 {
  await using var d=await Db();var with=await new ReportBuilderService(d,new AccessScope(d,new User(4,Perm.ScopeCompany,Perm.Reports,Perm.EmpSalary)),new User(4,Perm.ScopeCompany,Perm.Reports,Perm.EmpSalary)).RunAsync("employees",null,null);
  Assert.Contains("salary",with.Columns);
 }
 static string Root(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d!=null&&!Directory.Exists(Path.Combine(d.FullName,"src")))d=d.Parent;Assert.NotNull(d);return d!.FullName;}
 /// <summary>Every source file that touches a private Employee field must be on this audited list. A new file fails the test until someone reviews its guard and adds it here.</summary>
 static readonly string[] Audited=
 [
  "src/PeopleHub.Application/Services/EmployeeImportValidation.cs","src/PeopleHub.Application/Services/EmployeeImportBatches.cs","src/PeopleHub.Application/Services/SalaryStructures.cs","src/PeopleHub.Application/Services/ReportService.cs","src/PeopleHub.Application/Services/PayrollService.cs","src/PeopleHub.Application/Services/EmployeeService.cs","src/PeopleHub.Application/Services/Platform.cs","src/PeopleHub.Application/Services/EmploymentTimeline.cs",
  "src/PeopleHub.Web/Controllers/Hr.cs","src/PeopleHub.Web/Controllers/Platform.cs"
 ];
 [Fact]public void OnlyAuditedFilesReferencePrivateEmployeeFields()
 {
  var rx=new Regex(@"\.(BasicSalary|Iban|NationalId|BankName|InsuranceNumber|BirthDate)\b|\bBasicSalary\s*=",RegexOptions.Compiled);var found=new SortedSet<string>();
  foreach(var dir in new[]{"src/PeopleHub.Application","src/PeopleHub.Web/Controllers","src/PeopleHub.Web/Services"})
   foreach(var f in Directory.GetFiles(Path.Combine(Root(),dir),"*.cs",SearchOption.AllDirectories).Where(x=>!x.Contains("/obj/")&&!x.Contains("/bin/")))
    if(rx.IsMatch(File.ReadAllText(f)))found.Add(Path.GetRelativePath(Root(),f).Replace('\\','/'));
  var unexpected=found.Except(Audited).ToList();
  Assert.True(unexpected.Count==0,"Unaudited files reference private employee fields: "+string.Join(", ",unexpected));
 }
}
