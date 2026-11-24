using PeopleHub.Domain.Entities;
namespace PeopleHub.Application.Services;
public record PipView(List<PipMilestone> Milestones,List<PipProgress> Progress,bool CanReview);
public partial class PerfService
{
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
