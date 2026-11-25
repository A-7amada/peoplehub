using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record JobStatus(string Name,string LastStatus,DateTime? LastStartedAt,DateTime? LastFinishedAt,int ConsecutiveFailures,long RunCount,string LastError,bool Leased);
/// <summary>ARC-004: durable run records and a lease for the in-process maintenance jobs. A lease stops two loops or two instances running one job at once; a crashed run is released when its lease passes, so the next wake runs the job again (the jobs themselves are date-based and idempotent). It is not an external scheduler: nothing runs while the host sleeps.</summary>
public class ScheduledJobs(IRepository repo,AppClock clock)
{
 public async Task<bool> TryClaimAsync(string name,TimeSpan lease)
 {
  var now=clock.UtcNow;
  var s=await repo.FirstOrDefaultAsync(repo.Query<ScheduledJobState>().Where(x=>x.Name==name));
  if(s==null){s=new ScheduledJobState{Name=name};repo.Add(s);}
  else if(s.LeaseUntil!=null&&s.LeaseUntil>now)return false;
  s.LeaseUntil=now+lease;s.LastStartedAt=now;s.LastStatus="Running";
  try{await repo.SaveAsync();return true;}
  catch(Exception ex) when(ex.GetType().Name.StartsWith("DbUpdate")){return false;}
 }
 public async Task CompleteAsync(string name,Exception? error)
 {
  var s=await repo.FirstOrDefaultAsync(repo.Query<ScheduledJobState>().Where(x=>x.Name==name));
  if(s==null)return;
  s.LastFinishedAt=clock.UtcNow;s.LeaseUntil=null;s.RunCount++;
  if(error==null){s.LastStatus="Succeeded";s.LastError="";s.ConsecutiveFailures=0;}
  else{s.LastStatus="Failed";s.LastError=error.GetType().Name;s.ConsecutiveFailures++;}
  await repo.SaveAsync();
 }
 public async Task<List<JobStatus>> ListAsync()
 {
  var now=clock.UtcNow;
  var rows=await repo.ToListAsync(repo.Query<ScheduledJobState>().OrderBy(x=>x.Name));
  return rows.Select(x=>new JobStatus(x.Name,x.LastStatus,x.LastStartedAt,x.LastFinishedAt,x.ConsecutiveFailures,x.RunCount,x.LastError,x.LeaseUntil!=null&&x.LeaseUntil>now)).ToList();
 }
}
