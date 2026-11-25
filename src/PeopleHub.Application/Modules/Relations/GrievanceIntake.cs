using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Relations;
public record GrievanceView(List<EmployeeGrievance> Submissions,List<EmployeeGrievanceResponse> Responses,bool CanSubmit,List<EmployeeGrievanceAppeal> Appeals,List<EmployeeGrievanceAppealDecision> Decisions);
public class GrievanceIntake(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 private async Task<Employee?> CurrentEmployeeAsync()
 {
  var u=await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x=>x.Id==me.UserId&&x.IsActive));
  if(u==null||u.EmployeeId==null||u.EmployeeId!=me.EmployeeId)return null;
  return await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==u.EmployeeId&&(x.Status==EmployeeStatus.Active||x.Status==EmployeeStatus.OnLeave)));
 }
 public async Task<GrievanceView> GetAsync()
 {
  var own=await CurrentEmployeeAsync();bool manage=me.Can(Perm.RelationsManage);if(!manage&&own==null)throw new DomainException("err.forbidden");
  var ownId=own?.Id??-1;
  var submissions=await repo.ToListAsync(repo.Query<EmployeeGrievance>().Where(x=>manage||x.EmployeeId==ownId).OrderByDescending(x=>x.Id).Select(x=>new EmployeeGrievance{Id=x.Id,EmployeeId=x.EmployeeId,SubmittedByUserId=x.SubmittedByUserId,Title=x.Title,Statement=x.Statement,Actor=x.Actor,At=x.At}));
  var ids=submissions.Select(x=>x.Id).ToList();return new(submissions,await repo.ToListAsync(repo.Query<EmployeeGrievanceResponse>().Where(x=>ids.Contains(x.GrievanceId)).OrderBy(x=>x.Id).Select(x=>new EmployeeGrievanceResponse{Id=x.Id,GrievanceId=x.GrievanceId,ActorUserId=x.ActorUserId,Actor=x.Actor,Note=x.Note,At=x.At})),own!=null,await repo.ToListAsync(repo.Query<EmployeeGrievanceAppeal>().Where(x=>ids.Contains(x.GrievanceId)).OrderBy(x=>x.Id).Select(x=>new EmployeeGrievanceAppeal{Id=x.Id,GrievanceId=x.GrievanceId,ResponseId=x.ResponseId,Note=x.Note,ActorUserId=x.ActorUserId,Actor=x.Actor,At=x.At})),await repo.ToListAsync(repo.Query<EmployeeGrievanceAppealDecision>().Where(x=>ids.Contains(x.GrievanceId)).OrderBy(x=>x.Id).Select(x=>new EmployeeGrievanceAppealDecision{Id=x.Id,AppealId=x.AppealId,GrievanceId=x.GrievanceId,Decision=x.Decision,Note=x.Note,ActorUserId=x.ActorUserId,Actor=x.Actor,At=x.At})));
 }
 public Task SubmitAsync(string title,string statement)=>repo.AtomicAsync(async()=>
 {
  var e=await CurrentEmployeeAsync()??throw new DomainException("err.forbidden");if(string.IsNullOrWhiteSpace(title)||title.Length>200||string.IsNullOrWhiteSpace(statement)||statement.Length>8000)throw new DomainException("err.required");
  repo.Add(new EmployeeGrievance{EmployeeId=e.Id,SubmittedByUserId=me.UserId,Title=title.Trim(),Statement=statement.Trim(),Actor=me.Email,At=clock.UtcNow});audit.Log("SubmitGrievance","EmployeeGrievance",e.Id,"Private employee intake retained; title and contents withheld. No investigation access or notice created");await repo.SaveAsync();
 });
 public Task RespondAsync(int id,string note)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.RelationsManage))throw new DomainException("err.forbidden");if(string.IsNullOrWhiteSpace(note)||note.Length>8000)throw new DomainException("err.required");var g=await repo.FirstOrDefaultAsync(repo.Query<EmployeeGrievance>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");
  var user=await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x=>x.Id==me.UserId&&x.IsActive))??throw new DomainException("err.forbidden");if(user.EmployeeId==g.EmployeeId||me.EmployeeId==g.EmployeeId||g.SubmittedByUserId==me.UserId)throw new DomainException("err.forbidden");
  g.ChangeStamp=Guid.NewGuid().ToString("N");repo.Add(new EmployeeGrievanceResponse{GrievanceId=id,Note=note.Trim(),ActorUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});audit.Log("RespondGrievance","EmployeeGrievance",id,"Explicit employee-visible response retained; contents withheld. No case or employment/payroll effect");await repo.SaveAsync();
 });
 public Task AppealAsync(int responseId,string note)=>repo.AtomicAsync(async()=>
 {
  var own=await CurrentEmployeeAsync()??throw new DomainException("err.forbidden");if(string.IsNullOrWhiteSpace(note)||note.Length>8000)throw new DomainException("err.required");
  var response=await repo.FirstOrDefaultAsync(repo.Query<EmployeeGrievanceResponse>().Where(x=>x.Id==responseId))??throw new DomainException("err.notFound");
  var g=await repo.FirstOrDefaultAsync(repo.Query<EmployeeGrievance>().Where(x=>x.Id==response.GrievanceId))??throw new DomainException("err.notFound");
  if(g.EmployeeId!=own.Id||g.SubmittedByUserId!=me.UserId||await repo.AnyAsync(repo.Query<EmployeeGrievanceAppeal>().Where(x=>x.ResponseId==responseId)))throw new DomainException("err.forbidden");
  g.ChangeStamp=Guid.NewGuid().ToString("N");repo.Add(new EmployeeGrievanceAppeal{GrievanceId=g.Id,ResponseId=response.Id,Note=note.Trim(),ActorUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});audit.Log("AppealGrievanceResponse","EmployeeGrievance",g.Id,"Private employee-visible response appeal retained; contents withheld. No legal, case or employment effect");await repo.SaveAsync();
 });
 public Task DecideAppealAsync(int appealId,string decision,string note)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.RelationsManage))throw new DomainException("err.forbidden");if(decision is not("ResponseUpheld" or "ReconsiderationRecommended")||string.IsNullOrWhiteSpace(note)||note.Length>8000)throw new DomainException("err.required");
  var a=await repo.FirstOrDefaultAsync(repo.Query<EmployeeGrievanceAppeal>().Where(x=>x.Id==appealId))??throw new DomainException("err.notFound");var g=await repo.FirstOrDefaultAsync(repo.Query<EmployeeGrievance>().Where(x=>x.Id==a.GrievanceId))??throw new DomainException("err.notFound");var r=await repo.FirstOrDefaultAsync(repo.Query<EmployeeGrievanceResponse>().Where(x=>x.Id==a.ResponseId&&x.GrievanceId==g.Id))??throw new DomainException("err.notFound");
  var user=await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x=>x.Id==me.UserId&&x.IsActive))??throw new DomainException("err.forbidden");var responder=await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x=>x.Id==r.ActorUserId));
  if(user.EmployeeId==g.EmployeeId||me.EmployeeId==g.EmployeeId||g.SubmittedByUserId==me.UserId||a.ActorUserId==me.UserId||r.ActorUserId==me.UserId||responder?.EmployeeId is {} employee&&(user.EmployeeId==employee||me.EmployeeId==employee)||await repo.AnyAsync(repo.Query<EmployeeGrievanceAppealDecision>().Where(x=>x.AppealId==appealId)))throw new DomainException("err.forbidden");
  g.ChangeStamp=Guid.NewGuid().ToString("N");repo.Add(new EmployeeGrievanceAppealDecision{GrievanceId=g.Id,AppealId=a.Id,Decision=decision,Note=note.Trim(),ActorUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});audit.Log("DecideGrievanceAppeal","EmployeeGrievance",g.Id,"Independent employee-visible internal disposition retained; contents withheld. Source response unchanged; no case, notice, legal or sanction effect");await repo.SaveAsync();
 });
}
