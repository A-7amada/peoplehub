using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class PayrollJournalTests
{
 class User(bool view=true,bool fin=true,bool export=true):ICurrentUser{public string Email=>"finance@example.local";public int? EmployeeId=>1;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>p==Perm.PayView?view:p==Perm.ReportsFin?fin:p==Perm.PayExport&&export;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 [Fact]public async Task RequiresFinancialViewAndSeparateExportGrant(){await using var d=await Db();await Assert.ThrowsAsync<DomainException>(()=>new PayrollJournals(d,new User(false)).BuildAsync(1));await Assert.ThrowsAsync<DomainException>(()=>new PayrollJournals(d,new User(true,false)).BuildAsync(1));await Assert.ThrowsAsync<DomainException>(()=>new PayrollJournals(d,new User(true,true,false)).CsvAsync(1));}
 [Fact]public async Task CapturedCostCentresOnlyAndBalancedTotalsWithoutPrivateIdentities(){await using var d=await Db();var lines=await d.Set<PayrollLine>().Where(x=>x.RunId==1).ToListAsync();lines[0].CostCenterSnapshot="=Historical-A";await d.SaveAsync();var s=new PayrollJournals(d,new User());var j=await s.BuildAsync(1);Assert.Equal(lines.Sum(x=>x.Gross),j.Rows.Sum(x=>x.Gross));Assert.Equal(lines.Sum(x=>x.Net),j.Rows.Sum(x=>x.Net));Assert.Contains(j.Rows,x=>x.CostCenter=="=Historical-A");Assert.Contains(j.Rows,x=>x.CostCenter=="[Unallocated / legacy]");var csv=await s.CsvAsync(1);Assert.DoesNotContain("IBAN",csv);Assert.DoesNotContain("Ahmed",csv);Assert.Contains("EGP",csv);Assert.Contains("'=Historical-A",csv);}
 [Fact]public async Task DraftAndUnbalancedEvidenceAreRejected(){await using var d=await Db();var run=await d.Set<PayrollRun>().FindAsync(1);run!.Status=PayrollStatus.Draft;await d.SaveAsync();await Assert.ThrowsAsync<DomainException>(()=>new PayrollJournals(d,new User()).BuildAsync(1));run.Status=PayrollStatus.Finalized;(await d.Set<PayrollLine>().FirstAsync(x=>x.RunId==1)).Net+=100;await d.SaveAsync();await Assert.ThrowsAsync<DomainException>(()=>new PayrollJournals(d,new User()).BuildAsync(1));}
}
