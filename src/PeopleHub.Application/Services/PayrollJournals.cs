using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using System.Globalization;
namespace PeopleHub.Application.Services;
public record CostCentreJournalRow(string CostCenter,int EmployeeCount,decimal Gross,decimal Deductions,decimal Net);
public record CostCentreJournal(int RunId,int Year,int Month,int Version,List<CostCentreJournalRow> Rows);
/// <summary>Balanced internal planning projection, no accounting posting or bank instruction.</summary>
public class PayrollJournals(IRepository repo,ICurrentUser me)
{
 public async Task<CostCentreJournal> BuildAsync(int runId)
 {
  if(!me.Can(Perm.PayView)||!me.Can(Perm.ReportsFin))throw new DomainException("err.forbidden");
  var run=await repo.FirstOrDefaultAsync(repo.Query<PayrollRun>().Where(x=>x.Id==runId))??throw new DomainException("err.notFound");if(!run.IsLocked)throw new DomainException("err.payrollState");
  var lines=await repo.ToListAsync(repo.Query<PayrollLine>().Where(x=>x.RunId==runId));
  if(lines.Any(x=>Math.Abs(x.Gross-x.TotalDeductions-x.Net)>0.01m))throw new DomainException("err.required");
  var rows=lines.GroupBy(x=>string.IsNullOrWhiteSpace(x.CostCenterSnapshot)?"[Unallocated / legacy]":x.CostCenterSnapshot).OrderBy(x=>x.Key).Select(g=>new CostCentreJournalRow(g.Key,g.Count(),g.Sum(x=>x.Gross),g.Sum(x=>x.TotalDeductions),g.Sum(x=>x.Net))).ToList();
  if(rows.Any(x=>Math.Abs(x.Gross-x.Deductions-x.Net)>0.01m))throw new DomainException("err.required");return new(run.Id,run.Year,run.Month,run.CalculationVersion,rows);
 }
 private static string SafeCell(string value)=>(value.TrimStart() is var t && t.Length>0 && "=+-@".Contains(t[0]))?"'"+value:value;
 public async Task<string> CsvAsync(int id)
 {
  if(!me.Can(Perm.PayExport))throw new DomainException("err.forbidden");var journal=await BuildAsync(id);var b=new System.Text.StringBuilder("Run,Period,CalculationVersion,CostCenter,Employees,GrossExpense,DeductionsClearing,NetPayable,Currency\n");foreach(var r in journal.Rows)b.AppendLine(string.Join(',',id,$"{journal.Year}-{journal.Month:00}",journal.Version,PayrollService.Csv(SafeCell(r.CostCenter)),r.EmployeeCount,r.Gross.ToString("0.00",CultureInfo.InvariantCulture),r.Deductions.ToString("0.00",CultureInfo.InvariantCulture),r.Net.ToString("0.00",CultureInfo.InvariantCulture),"EGP"));return b.ToString();
 }
}
