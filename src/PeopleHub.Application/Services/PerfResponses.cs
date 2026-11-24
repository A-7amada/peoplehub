using PeopleHub.Domain.Entities;
namespace PeopleHub.Application.Services;
public partial class PerfService
{
 public async Task<List<PerformanceResponse>> ResponsesAsync(int id){await GetAsync(id);return await repo.ToListAsync(repo.Query<PerformanceResponse>().Where(x=>x.AppraisalId==id).OrderBy(x=>x.Id));}
 public Task RespondAsync(int id,string kind,string note)=>repo.AtomicAsync(async()=>
 {
  var v=await GetAsync(id);if(v.A.EmployeeId!=me.EmployeeId||v.A.Cycle!.Status!=CycleStatus.Closed||v.Final==null)throw new DomainException("err.forbidden");if(kind is not("Acknowledged" or "Appeal")||string.IsNullOrWhiteSpace(note)||note.Length>2000)throw new DomainException("err.required");if(await repo.AnyAsync(repo.Query<PerformanceResponse>().Where(x=>x.AppraisalId==id&&x.Kind==kind)))throw new DomainException("err.forbidden");
  repo.Add(new PerformanceResponse{AppraisalId=id,Kind=kind,Note=note.Trim(),Actor=me.Email,At=clock.UtcNow});v.A.ResponseStamp=Guid.NewGuid().ToString("N"); // serialize response/decision races
  audit.Log("Performance"+kind,"Appraisal",id,note.Trim());if(kind=="Appeal")await notify.NotifyPermAsync(Perm.TalentManage,"notif.perfAppeal",$"appraisal #{id}",$"/Perf/Appraisal/{id}");await repo.SaveAsync();
 });
 public Task DecideAppealAsync(int id,int appealId,string decision,string note)=>repo.AtomicAsync(async()=>
 {
  var v=await GetAsync(id);if(!Hr||v.A.EmployeeId==me.EmployeeId||v.A.Cycle!.Status!=CycleStatus.Closed)throw new DomainException("err.forbidden");if(decision is not("Upheld" or "ChangeRecommended")||string.IsNullOrWhiteSpace(note)||note.Length>2000)throw new DomainException("err.required");var appeal=await repo.FirstOrDefaultAsync(repo.Query<PerformanceResponse>().Where(x=>x.Id==appealId&&x.AppraisalId==id&&x.Kind=="Appeal"))??throw new DomainException("err.notFound");if(await repo.AnyAsync(repo.Query<PerformanceResponse>().Where(x=>x.AppealId==appeal.Id)))throw new DomainException("err.forbidden");
  v.A.ResponseStamp=Guid.NewGuid().ToString("N");repo.Add(new PerformanceResponse{AppraisalId=id,AppealId=appeal.Id,Kind=decision,Note=note.Trim(),Actor=me.Email,At=clock.UtcNow});audit.Log("AppealDecision","Appraisal",id,$"{decision}: {note.Trim()}");await notify.NotifyEmployeeAsync(v.A.EmployeeId,"notif.perfAppealDecision",$"appraisal #{id}: {decision}",$"/Perf/Appraisal/{id}");await repo.SaveAsync();
 });
}
