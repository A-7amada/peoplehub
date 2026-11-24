using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public class BankReconciliation(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 private void Need(){if(!me.Can(Perm.PayView)||!me.Can(Perm.PayReconcile))throw new DomainException("err.forbidden");}
 public async Task<List<PayrollBankResult>> ResultsAsync(int runId){Need();return await repo.ToListAsync(repo.Query<PayrollBankResult>().Where(x=>x.RunId==runId).OrderBy(x=>x.Id));}
 public Task RecordAsync(int runId,int lineId,string outcome,decimal amount,string reference,string note)=>repo.AtomicAsync(async()=>
 {
  Need();if(outcome is not("Paid" or "Rejected" or "Returned")||string.IsNullOrWhiteSpace(reference)||reference.Trim().Length>200||string.IsNullOrWhiteSpace(note)||note.Trim().Length>2000)throw new DomainException("err.reasonRequired");
  var run=await repo.FirstOrDefaultAsync(repo.Query<PayrollRun>().Where(x=>x.Id==runId))??throw new DomainException("err.notFound");if(!run.IsLocked)throw new DomainException("err.payrollState");
  var line=await repo.FirstOrDefaultAsync(repo.Query<PayrollLine>().Where(x=>x.Id==lineId&&x.RunId==runId))??throw new DomainException("err.notFound");
  if(line.Net<=0||amount!=(outcome=="Rejected"?0:line.Net))throw new DomainException("err.required");
  var events=await repo.ToListAsync(repo.Query<PayrollBankResult>().Where(x=>x.LineId==lineId).OrderBy(x=>x.Version));var last=events.LastOrDefault();
  if(outcome=="Returned"&&last?.Outcome!="Paid"||last?.Outcome=="Paid"&&outcome!="Returned"||last?.Outcome==outcome)throw new DomainException("err.forbidden");
  reference=reference.Trim();if(events.Any(x=>x.Reference==reference&&x.Outcome==outcome))throw new DomainException("err.forbidden");
  repo.Add(new PayrollBankResult{RunId=runId,LineId=lineId,Version=(last?.Version??0)+1,Outcome=outcome,Amount=amount,Reference=reference,Note=note.Trim(),Actor=me.Email,At=clock.UtcNow});
  audit.Log("BankResult","PayrollLine",lineId,"External bank result evidence recorded manually; no transfer, repayment or payroll status change");await repo.SaveAsync();
 });
}
