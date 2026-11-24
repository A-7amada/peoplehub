using PeopleHub.Domain.Entities;
namespace PeopleHub.Application.Services;
public record PerformanceTrend(int AppraisalId,string Cycle,DateTime EndsOn,decimal? Score,int OpenMilestones,int CompletedMilestones);
public record PipView(List<PipMilestone> Milestones,List<PipProgress> Progress,bool CanReview);
public partial class PerfService
{
 public async Task<List<PerformanceTrend>> TrendsAsync(int id)
 {
  var current=await GetAsync(id);var employeeId=current.A.EmployeeId;
  var apps=await repo.ToListAsync(repo.Query<PerfAppraisal>().Where(a=>a.EmployeeId==employeeId&&a.Cycle!.Status==CycleStatus.Closed).OrderByDescending(a=>a.Cycle!.End).Take(12));
  var appIds=apps.Select(a=>a.Id).ToList();var milestones=await repo.ToListAsync(repo.Query<PipMilestone>().Where(m=>appIds.Contains(m.AppraisalId)));var rows=new List<PerformanceTrend>();
  foreach(var app in apps){var v=await GetAsync(app.Id);rows.Add(new(app.Id,app.Cycle!.Name,app.Cycle.End,v.Final,milestones.Count(m=>m.AppraisalId==app.Id&&m.Status=="Open"),milestones.Count(m=>m.AppraisalId==app.Id&&m.Status=="Completed")));}return rows.OrderBy(x=>x.EndsOn).ToList();
 }
 private bool CanReviewPip(AppraisalView v)=>v.A.EmployeeId!=me.EmployeeId&&(Hr||v.A.Employee?.ManagerId==me.EmployeeId);
 public async Task<PipView> PipAsync(int id){var v=await GetAsync(id);var milestones=await repo.ToListAsync(repo.Query<PipMilestone>().Where(x=>x.AppraisalId==id).OrderBy(x=>x.DueOn));var ids=milestones.Select(x=>x.Id).ToList();return new(milestones,await repo.ToListAsync(repo.Query<PipProgress>().Where(x=>ids.Contains(x.MilestoneId)).OrderBy(x=>x.Id)),CanReviewPip(v));}
 public Task AddMilestoneAsync(int id,string title,string measure,DateTime dueOn,string reason)=>repo.AtomicAsync(async()=>{
  var v=await GetAsync(id);if(!CanReviewPip(v))throw new DomainException("err.forbidden");
  if(string.IsNullOrWhiteSpace(title)||title.Length>200||string.IsNullOrWhiteSpace(measure)||measure.Length>1000||string.IsNullOrWhiteSpace(reason)||reason.Length>2000||dueOn.Date<clock.Today||dueOn.Year>9998)throw new DomainException("err.required");
  var m=new PipMilestone{AppraisalId=id,Title=title.Trim(),SuccessMeasure=measure.Trim(),DueOn=dueOn.Date,CreatedBy=me.Email,CreatedAt=clock.UtcNow};repo.Add(m);v.A.ResponseStamp=Guid.NewGuid().ToString("N");await repo.SaveAsync();
  repo.Add(new PipProgress{MilestoneId=m.Id,Kind="Created",Note=reason.Trim(),Actor=me.Email,At=clock.UtcNow});audit.Log("CreatePipMilestone","Appraisal",id,title.Trim());await notify.NotifyEmployeeAsync(v.A.EmployeeId,"notif.pip",title.Trim(),$"/Perf/Appraisal/{id}");await repo.SaveAsync();
 });
 public Task UpdateMilestoneAsync(int milestoneId,int progress,string note,string action)=>repo.AtomicAsync(async()=>{
  var m=await repo.FirstOrDefaultAsync(repo.Query<PipMilestone>().Where(x=>x.Id==milestoneId))??throw new DomainException("err.notFound");var v=await GetAsync(m.AppraisalId);
  if(m.Status!="Open"||progress<0||progress>100||string.IsNullOrWhiteSpace(note)||note.Length>2000||action is not("Update" or "Completed" or "Cancelled"))throw new DomainException("err.required");
  if(action!="Update"&&!CanReviewPip(v))throw new DomainException("err.forbidden");
  if(action=="Completed"&&progress!=100)throw new DomainException("err.required");
  v.A.ResponseStamp=Guid.NewGuid().ToString("N");if(action!="Update")m.Status=action;
  repo.Add(new PipProgress{MilestoneId=m.Id,Kind=action,Progress=progress,Note=note.Trim(),Actor=me.Email,At=clock.UtcNow});audit.Log("Pip"+action,"PipMilestone",m.Id,note.Trim());
  if(v.A.EmployeeId!=me.EmployeeId)await notify.NotifyEmployeeAsync(v.A.EmployeeId,"notif.pip",$"{m.Title}: {action}",$"/Perf/Appraisal/{m.AppraisalId}");await repo.SaveAsync();
 });
}
