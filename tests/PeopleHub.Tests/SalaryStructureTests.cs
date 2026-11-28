using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Payroll;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class SalaryStructureTests
{
 class User(bool grant=true):ICurrentUser{public string Email=>"pay@example.local";public int? EmployeeId=>1;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>grant;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static SalaryStructures S(AppDbContext d,bool grant=true){var u=new User(grant);var c=new AppClock(TimeProvider.System);return new(d,u,c,new AuditService(d,u,c));}
 static async Task<int> Create(AppDbContext d,int? grade=null,string parts=""){grade??=(await d.Set<Employee>().SingleAsync(x=>x.Id==6)).JobTitle!.Grade;await S(d).CreateAsync("Fictional grade structure",grade.Value,15000,parts,"Explicit fictional compensation design");var sid=await d.Set<SalaryStructure>().Select(x=>x.Id).SingleAsync();await S(d).PublishAsync(sid);return sid;}
 [Fact]public async Task NoGrantCannotReadCreateOrApply(){await using var d=await Db();int id=await Create(d);await Assert.ThrowsAsync<DomainException>(()=>S(d,false).ListAsync());await Assert.ThrowsAsync<DomainException>(()=>S(d,false).HistoryAsync());await Assert.ThrowsAsync<DomainException>(()=>S(d,false).CreateAsync("N",1,1000,"","R"));await Assert.ThrowsAsync<DomainException>(()=>S(d,false).ApplyAsync(6,id,"R"));}
 [Fact]public async Task ExplicitApplicationChangesOnlyListedComponentsAndPreservesPayroll(){await using var d=await Db();var comp=await d.Set<SalaryComponent>().FirstAsync(x=>x.IsFixed);int id=await Create(d,parts:$"{comp.Id}=550.00");var old=await d.Set<PayrollLine>().Select(x=>x.Net).ToListAsync();await S(d).ApplyAsync(6,id,"Explicit approval for fictional demo");Assert.Equal(15000,(await d.Set<Employee>().FindAsync(6))!.BasicSalary);Assert.Equal(550,(await d.Set<EmployeeSalaryComponent>().SingleAsync(x=>x.EmployeeId==6&&x.ComponentId==comp.Id)).Amount);Assert.Equal(old,await d.Set<PayrollLine>().Select(x=>x.Net).ToListAsync());var h=await d.Set<SalaryStructureApplication>().SingleAsync();Assert.Contains("15000",h.AfterJson);Assert.Equal(64,h.Sha256.Length);Assert.NotEmpty(h.BeforeJson);Assert.Contains(await d.Set<EmployeeAssignment>().ToListAsync(),x=>x.EmployeeId==6&&x.BasicSalary==15000);}
 [Fact]public async Task GradeMismatchAndPendingEmploymentBlockApplication(){await using var d=await Db();var e=await d.Set<Employee>().SingleAsync(x=>x.Id==6);int id=await Create(d,e.JobTitle!.Grade+1);await Assert.ThrowsAsync<DomainException>(()=>S(d).ApplyAsync(6,id,"No match"));Assert.Empty(await d.Set<SalaryStructureApplication>().ToListAsync());e.JobTitle!.Grade++;d.Add(new EmploymentChange{EmployeeId=6,Kind="Assignment",EffectiveOn=DateTime.UtcNow.AddDays(10)});await d.SaveAsync();await Assert.ThrowsAsync<DomainException>(()=>S(d).ApplyAsync(6,id,"Scheduled change blocks"));}
 [Fact]public async Task InvalidComponentsAndNegativeValuesAreRejected(){await using var d=await Db();foreach(string parts in new[]{"999999=100","1=-1","1=2\n1=3","broken"})await Assert.ThrowsAsync<DomainException>(()=>S(d).CreateAsync("N",1,1000,parts,"R"));await Assert.ThrowsAsync<DomainException>(()=>S(d).CreateAsync("N",1,-1,"","R"));Assert.Empty(await d.Set<SalaryStructure>().ToListAsync());}
 [Fact]public async Task RecordedStructuresAndApplicationsAreImmutable(){await using var d=await Db();int id=await Create(d);await S(d).ApplyAsync(6,id,"Fictional approved change");(await d.Set<SalaryStructure>().SingleAsync()).Basic=1;await Assert.ThrowsAsync<InvalidOperationException>(()=>d.SaveAsync());d.ChangeTracker.Clear();(await d.Set<SalaryStructureApplication>().SingleAsync()).AfterJson="Rewrite";await Assert.ThrowsAsync<InvalidOperationException>(()=>d.SaveAsync());}

 [Theory][InlineData(EmploymentType.FullTime)][InlineData(EmploymentType.PartTime)][InlineData(EmploymentType.Contractor)]public async Task MatchingExplicitCategoryAppliesWithoutChangingEmployeeCategory(EmploymentType category){await using var d=await Db();var e=(await d.Set<Employee>().FindAsync(6))!;e.EmploymentType=category;await d.SaveAsync();await S(d).CreateAsync("Category-specific fictional structure",e.JobTitle!.Grade,15000,"","Fictional category design",category);int id=(await d.Set<SalaryStructure>().SingleAsync()).Id;await S(d).PublishAsync(id);await S(d).ApplyAsync(6,id,"Explicit fictional application");Assert.Equal(15000,e.BasicSalary);Assert.Equal(category,e.EmploymentType);Assert.Equal(category,(await d.Set<SalaryStructure>().SingleAsync()).EmploymentCategory);}
 [Fact]public async Task CategoryMismatchCannotChangeEmployeeSalaryOrApplicationEvidence(){await using var d=await Db();var e=(await d.Set<Employee>().FindAsync(6))!;var salary=e.BasicSalary;int grade=e.JobTitle!.Grade;var other=e.EmploymentType==EmploymentType.Contractor?EmploymentType.FullTime:EmploymentType.Contractor;await S(d).CreateAsync("Mismatched category",grade,15000,"","Fictional plan",other);int id=(await d.Set<SalaryStructure>().SingleAsync()).Id;int audit=await d.Set<AuditLog>().CountAsync();await Assert.ThrowsAsync<DomainException>(()=>S(d).ApplyAsync(6,id,"Mismatch"));Assert.Equal(salary,(await d.Set<Employee>().FindAsync(6))!.BasicSalary);Assert.Empty(await d.Set<SalaryStructureApplication>().ToListAsync());Assert.Equal(audit,await d.Set<AuditLog>().CountAsync());}
 [Fact]public async Task InvalidCategoryCannotCreateStructure(){await using var d=await Db();await Assert.ThrowsAsync<DomainException>(()=>S(d).CreateAsync("Invalid",1,1000,"","Fictional",(EmploymentType)99));Assert.Empty(await d.Set<SalaryStructure>().ToListAsync());}
 [Fact]public async Task UnrestrictedTemplateRemainsApplicableAfterExplicitEmployeeCategoryChange(){await using var d=await Db();int id=await Create(d);Assert.Null((await d.Set<SalaryStructure>().SingleAsync()).EmploymentCategory);var e=(await d.Set<Employee>().FindAsync(6))!;e.EmploymentType=EmploymentType.Contractor;await d.SaveAsync();await S(d).ApplyAsync(6,id,"Explicit application of unrestricted template");Assert.Equal(EmploymentType.Contractor,e.EmploymentType);Assert.Equal(15000,e.BasicSalary);}
 [Fact]public async Task ExistingTemplateUpgradePreservesAmountsAndLeavesCategoryUnrestricted(){await using var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync("20261125064900_GrievanceResponseAppeals");await d.Database.ExecuteSqlRawAsync("INSERT INTO SalaryStructure(Name,JobGrade,Basic,ComponentsJson,Reason,Actor,At,Revision) VALUES ('Fictional legacy',1,12345.67,'{{}}','Recorded','Fictional','2026-11-25 00:00:00','r')");await d.Database.MigrateAsync();var row=await d.Set<SalaryStructure>().SingleAsync();Assert.Null(row.EmploymentCategory);Assert.Equal(12345.67m,row.Basic);Assert.Equal("Fictional legacy",row.Name);}
}
public class SalaryStructureLifecycleTests
{
 class U:ICurrentUser{public string Email=>"pay@example.local";public int? EmployeeId=>1;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>true;public bool Can(string p)=>true;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static SalaryStructures S(AppDbContext d){var u=new U();var c=new AppClock(TimeProvider.System);return new(d,u,c,new AuditService(d,u,c));}
 [Fact]public void OutOfBandIsPureAndRespectsOpenEnds()
 {
  var comps=new List<SalaryComponent>{new(){Id=1,BandMin=100,BandMax=500},new(){Id=2,BandMax=50},new(){Id=3}};
  Assert.Empty(SalaryStructures.OutOfBand(new Dictionary<int,decimal>{{1,100},{1+1,50},{3,9999999}},comps));
  Assert.Equal([1,2],SalaryStructures.OutOfBand(new Dictionary<int,decimal>{{1,99.99m},{2,50.01m},{3,1}},comps));
  Assert.Equal([1],SalaryStructures.OutOfBand(new Dictionary<int,decimal>{{1,500.01m}},comps));
 }
 [Fact]public async Task NewStructureIsDraftCannotBeAppliedUntilPublishedAndPublishIsOnce()
 {
  await using var d=await Db();var s=S(d);var grade=(await d.Set<Employee>().SingleAsync(x=>x.Id==6)).JobTitle!.Grade;
  await s.CreateAsync("Fictional draft",grade,15000,"","Fictional lifecycle test");var st=await d.Set<SalaryStructure>().SingleAsync();
  Assert.Empty(await d.Set<SalaryStructurePublication>().ToListAsync());
  await Assert.ThrowsAsync<DomainException>(()=>s.ApplyAsync(6,st.Id,"Should be refused while draft"));
  await s.PublishAsync(st.Id);var pub=await d.Set<SalaryStructurePublication>().SingleAsync();Assert.Equal(st.Id,pub.StructureId);Assert.Equal("pay@example.local",pub.Actor);Assert.Equal(64,pub.Sha256.Length);
  await Assert.ThrowsAsync<DomainException>(()=>s.PublishAsync(st.Id));
  await s.ApplyAsync(6,st.Id,"Applied after publication");
  Assert.Equal(15000,(await d.Set<Employee>().FindAsync(6))!.BasicSalary);
 }
 [Fact]public async Task BandsBlockCreatePublishAndApplyWhenAmountIsOutside_AndBadBandsAreRefused()
 {
  await using var d=await Db();var s=S(d);var comp=await d.Set<SalaryComponent>().FirstAsync(x=>x.IsFixed);var grade=(await d.Set<Employee>().SingleAsync(x=>x.Id==6)).JobTitle!.Grade;
  await Assert.ThrowsAsync<DomainException>(()=>s.SetBandAsync(comp.Id,500,100,"Fictional bad band"));
  await Assert.ThrowsAsync<DomainException>(()=>s.SetBandAsync(comp.Id,100,500,""));
  await s.SetBandAsync(comp.Id,100,500,"Fictional demo band");
  await Assert.ThrowsAsync<DomainException>(()=>s.CreateAsync("Too high",grade,15000,$"{comp.Id}=900","Fictional outside band"));
  await s.CreateAsync("Inside",grade,15000,$"{comp.Id}=300","Fictional inside band");var st=await d.Set<SalaryStructure>().SingleAsync();
  await s.SetBandAsync(comp.Id,400,500,"Fictional tightened band");           // draft now falls outside
  await Assert.ThrowsAsync<DomainException>(()=>s.PublishAsync(st.Id));
  await s.SetBandAsync(comp.Id,null,null,"Fictional cleared band");await s.PublishAsync(st.Id);
  await s.SetBandAsync(comp.Id,400,500,"Fictional tightened again");
  await Assert.ThrowsAsync<DomainException>(()=>s.ApplyAsync(6,st.Id,"Published structure now outside band"));
 }
}
