using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record RecruitmentInterviewView(List<CandidateInterview> Interviews,List<RecruitmentEvent> History);
public class RecruitmentInterviews(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 private void Need(){if(!me.Can(Perm.TalentManage))throw new DomainException("err.forbidden");}
 public async Task<RecruitmentInterviewView> GetAsync(){Need();return new(await repo.ToListAsync(repo.Query<CandidateInterview>().OrderBy(x=>x.StartsAt)),await repo.ToListAsync(repo.Query<RecruitmentEvent>().OrderByDescending(x=>x.At)));}
 public Task ScheduleAsync(int candidateId,int interviewerEmployeeId,DateTime localStart,int minutes,string place,string note)=>repo.AtomicAsync(async()=>
 {
  Need();var zone=TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");localStart=DateTime.SpecifyKind(localStart,DateTimeKind.Unspecified);if(zone.IsInvalidTime(localStart)||zone.IsAmbiguousTime(localStart))throw new DomainException("err.dates");var start=TimeZoneInfo.ConvertTimeToUtc(localStart,zone);
  if(start<=clock.UtcNow||localStart.Year>9998||minutes<15||minutes>480||string.IsNullOrWhiteSpace(place)||place.Length>300||string.IsNullOrWhiteSpace(note)||note.Length>2000)throw new DomainException("err.required");
  var c=await repo.FirstOrDefaultAsync(repo.Query<Candidate>().Where(x=>x.Id==candidateId))??throw new DomainException("err.notFound");if(c.Stage is CandidateStage.Hired or CandidateStage.Rejected)throw new DomainException("err.forbidden");
  var interviewer=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==interviewerEmployeeId&&x.Status==EmployeeStatus.Active))??throw new DomainException("err.notFound");
  var end=start.AddMinutes(minutes);var all=await repo.ToListAsync(repo.Query<CandidateInterview>().Where(x=>x.Status=="Scheduled"&&(x.CandidateId==candidateId||x.InterviewerEmployeeId==interviewerEmployeeId)));
  if(all.Any(x=>x.StartsAt<end&&x.StartsAt.AddMinutes(x.Minutes)>start))throw new DomainException("err.overlap");
  // Touch candidate and interviewer revisions to serialize overlapping schedules for the same participant.
  c.InterviewStamp=Guid.NewGuid().ToString("N");interviewer.LeaveRequestStamp=Guid.NewGuid().ToString("N");
  var item=new CandidateInterview{CandidateId=c.Id,InterviewerEmployeeId=interviewer.Id,StartsAt=start,Minutes=minutes,Place=place.Trim(),Note=note.Trim(),Actor=me.Email,CreatedAt=clock.UtcNow};repo.Add(item);await repo.SaveAsync();repo.Add(new RecruitmentEvent{CandidateId=c.Id,InterviewId=item.Id,Kind="InterviewScheduled",Note=$"{localStart:yyyy-MM-dd HH:mm} Africa/Cairo, {minutes} minutes; {note.Trim()}",Actor=me.Email,At=clock.UtcNow});audit.Log("InterviewScheduled","Candidate",c.Id,"Internal plan only, no invitations sent");await repo.SaveAsync();
 });
 public Task CloseAsync(int interviewId,bool complete,string note)=>repo.AtomicAsync(async()=>
 {
  Need();if(string.IsNullOrWhiteSpace(note)||note.Length>2000)throw new DomainException("err.reasonRequired");var item=await repo.FirstOrDefaultAsync(repo.Query<CandidateInterview>().Where(x=>x.Id==interviewId))??throw new DomainException("err.notFound");if(item.Status!="Scheduled"||complete&&item.StartsAt.AddMinutes(item.Minutes)>clock.UtcNow)throw new DomainException("err.forbidden");item.Status=complete?"Completed":"Cancelled";repo.Add(new RecruitmentEvent{CandidateId=item.CandidateId,InterviewId=item.Id,Kind="Interview"+item.Status,Note=note.Trim(),Actor=me.Email,At=clock.UtcNow});audit.Log("Interview"+item.Status,"Candidate",item.CandidateId,note.Trim());await repo.SaveAsync();
 });
}
