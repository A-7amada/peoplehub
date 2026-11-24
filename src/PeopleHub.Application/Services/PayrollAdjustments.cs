using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record PayrollAdjustmentView(PayrollAdjustment Proposal,List<PayrollAdjustmentEvent> Events);
public class PayrollAdjustments(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit,PayrollService payroll)
{
 private void Need(){if(!me.Can(Perm.PayView)||!me.Can(Perm.PayAdjust))throw new DomainException("err.forbidden");}
 public async Task<List<PayrollLine>> SourcesAsync(int targetId){Need();var target=await Run(targetId);var runs=await repo.ToListAsync(repo.Query<PayrollRun>().Where(x=>x.Status==PayrollStatus.Finalized||x.Status==PayrollStatus.Paid));var ids=runs.Where(x=>x.Year*12+x.Month<target.Year*12+target.Month).Select(x=>x.Id).ToArray();return await repo.ToListAsync(repo.Query<PayrollLine>().Where(x=>ids.Contains(x.RunId)).OrderByDescending(x=>x.Id));}
 public async Task<List<PayrollAdjustmentView>> ListAsync(int targetRunId){Need();var p=await repo.ToListAsync(repo.Query<PayrollAdjustment>().Where(x=>x.TargetRunId==targetRunId).OrderByDescending(x=>x.Id));var ids=p.Select(x=>x.Id).ToArray();var events=await repo.ToListAsync(repo.Query<PayrollAdjustmentEvent>().Where(x=>ids.Contains(x.AdjustmentId)).OrderBy(x=>x.Id));return p.Select(x=>new PayrollAdjustmentView(x,events.Where(e=>e.AdjustmentId==x.Id).ToList())).ToList();}
 public Task ProposeAsync(int sourceLineId,int targetRunId,string kind,decimal amount,string reason)=>repo.AtomicAsync(async()=>
 {
  Need();if(!me.Can(Perm.PayPrepare))throw new DomainException("err.forbidden");Validate(reason);if(kind is not("Earning" or "Deduction")||amount<=0||amount>100000000||decimal.Round(amount,2)!=amount)throw new DomainException("err.required");
  var line=await repo.FirstOrDefaultAsync(repo.Query<PayrollLine>().Where(x=>x.Id==sourceLineId))??throw new DomainException("err.notFound");
  var source=await Run(line.RunId);var target=await Run(targetRunId);if(!source.IsLocked||target.Status!=PayrollStatus.Draft||source.Year*12+source.Month>=target.Year*12+target.Month)throw new DomainException("err.payrollState");
  if(!await repo.AnyAsync(repo.Query<PayrollLine>().Where(x=>x.RunId==target.Id&&x.EmployeeId==line.EmployeeId)))throw new DomainException("err.notFound");
  target.CorrectionStamp=Guid.NewGuid().ToString("N");repo.Add(new PayrollAdjustment{SourceRunId=source.Id,SourceLineId=line.Id,SourceVersion=source.CalculationVersion,TargetRunId=target.Id,EmployeeId=line.EmployeeId,Kind=kind,Amount=amount,Reason=reason.Trim(),ProposedByUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});audit.Log("Propose","PayrollAdjustment",target.Id,"Referenced correction proposal; no payroll effect");await repo.SaveAsync();
 });
 public Task DecideAsync(int id,bool approve,string reason)=>repo.AtomicAsync(async()=>
 {
  Need();if(!me.Can(Perm.PayFinance))throw new DomainException("err.forbidden");Validate(reason);var p=await Proposal(id);if(p.ProposedByUserId==me.UserId||p.EmployeeId==me.EmployeeId)throw new DomainException("err.forbidden");var target=await Run(p.TargetRunId);if((await Events(id)).Count!=0||target.Status!=PayrollStatus.Draft)throw new DomainException("err.payrollState");
  target.CorrectionStamp=Guid.NewGuid().ToString("N");AddEvent(p,approve?"Approved":"Rejected",reason);audit.Log("Decide","PayrollAdjustment",id,"Independent correction decision recorded");await repo.SaveAsync();
 });
 public Task CancelAsync(int id,string reason)=>repo.AtomicAsync(async()=>
 {
  Need();Validate(reason);var p=await Proposal(id);if(!me.Can(Perm.PayPrepare)||p.ProposedByUserId!=me.UserId)throw new DomainException("err.forbidden");var target=await Run(p.TargetRunId);var events=await Events(id);if(target.Status!=PayrollStatus.Draft||events.Any(x=>x.Action is "Applied" or "Rejected" or "Cancelled"))throw new DomainException("err.payrollState");target.CorrectionStamp=Guid.NewGuid().ToString("N");repo.Add(new PayrollAdjustmentEvent{AdjustmentId=id,Sequence=events.Count+1,Action="Cancelled",Reason=reason.Trim(),Actor=me.Email,At=clock.UtcNow});audit.Log("Cancel","PayrollAdjustment",id,"Unapplied proposal withdrawn; no payroll change");await repo.SaveAsync();
 });
 public Task ApplyAsync(int id,string reason)=>repo.AtomicAsync(async()=>
 {
  Need();if(!me.Can(Perm.PayPrepare))throw new DomainException("err.forbidden");Validate(reason);var p=await Proposal(id);var events=await Events(id);if(events.Count!=1||events[0].Action!="Approved")throw new DomainException("err.forbidden");var run=await Run(p.TargetRunId);if(run.Status!=PayrollStatus.Draft)throw new DomainException("err.payrollState");
  var line=await repo.FirstOrDefaultAsync(repo.Query<PayrollLine>().Where(x=>x.RunId==p.TargetRunId&&x.EmployeeId==p.EmployeeId))??throw new DomainException("err.notFound");
  // Legacy fixed deductions are unknown to this proposal workflow. Require a current captured calculation first.
  if(string.IsNullOrEmpty(line.EmployeeCodeSnapshot))throw new DomainException("err.payrollState");
  var bonus=line.Bonus+(p.Kind=="Earning"?p.Amount:0);var other=line.OtherDeduction-line.FixedDeduction+(p.Kind=="Deduction"?p.Amount:0);
  if(other<0||p.Kind=="Deduction"&&p.Amount>line.Net)throw new DomainException("err.required");
  var note=(line.AdjustmentNote??"")+"\nReferenced correction #"+p.Id+" from run #"+p.SourceRunId+": "+reason.Trim();await payroll.AdjustLineAsync(line.Id,bonus,other,note);AddEvent(p,"Applied",reason);audit.Log("Apply","PayrollAdjustment",id,"Explicitly applied to draft; original locked payroll unchanged");await repo.SaveAsync();
 });
 private static void Validate(string reason){if(string.IsNullOrWhiteSpace(reason)||reason.Length>2000)throw new DomainException("err.reasonRequired");}
 private Task<PayrollRun> Run(int id)=>Required(repo.Query<PayrollRun>().Where(x=>x.Id==id));
 private Task<PayrollAdjustment> Proposal(int id)=>Required(repo.Query<PayrollAdjustment>().Where(x=>x.Id==id));
 private async Task<T> Required<T>(IQueryable<T> q)where T:class=>await repo.FirstOrDefaultAsync(q)??throw new DomainException("err.notFound");
 private Task<List<PayrollAdjustmentEvent>> Events(int id)=>repo.ToListAsync(repo.Query<PayrollAdjustmentEvent>().Where(x=>x.AdjustmentId==id).OrderBy(x=>x.Id));
 private void AddEvent(PayrollAdjustment p,string action,string reason)=>repo.Add(new PayrollAdjustmentEvent{AdjustmentId=p.Id,Sequence=action=="Applied"?2:1,Action=action,Reason=reason.Trim(),Actor=me.Email,At=clock.UtcNow});
}
