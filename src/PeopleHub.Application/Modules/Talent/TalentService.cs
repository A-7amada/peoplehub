using System.Collections.Concurrent;
using System.Net.Sockets;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Domain.Security;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Talent;

public record TrainingParticipant(int Id,int CourseId,int EmployeeId,string FullName,string FullNameAr,bool Completed);
public class TalentService(IRepository repo, ICurrentUser me, AccessScope scope, AppClock clock, AuditService audit, NotificationService notify)
{
    private void Need() { if (!me.Can(Perm.TalentManage)) throw new DomainException("err.forbidden"); }

    public Task<List<JobOpening>> JobsAsync() { Need(); return repo.ToListAsync(repo.Query<JobOpening>().OrderByDescending(j => j.IsOpen).ThenByDescending(j => j.CreatedAt)); }
    public Task<List<Candidate>> CandidatesAsync() { Need(); return repo.ToListAsync(repo.Query<Candidate>().OrderBy(c => c.Stage).ThenByDescending(c => c.CreatedAt)); }
    public async Task AddJobAsync(string title,string? titleAr,int? dept,int positions,string? justification=null)
    {
        Need();if(string.IsNullOrWhiteSpace(title)||title.Length>200||positions<1||positions>1000||string.IsNullOrWhiteSpace(justification)||justification.Length>2000)throw new DomainException("err.required");if(dept!=null&&!await repo.AnyAsync(repo.Query<Department>().Where(x=>x.Id==dept)))throw new DomainException("err.notFound");
        repo.Add(new JobOpening{Title=title.Trim(),TitleAr=titleAr?.Trim()??"",DepartmentId=dept,Positions=positions,Justification=justification.Trim(),ApprovalStatus="Pending",RequestedByUserId=me.UserId,IsOpen=false,CreatedAt=clock.UtcNow});audit.Log("Requisition","Job",title,"Pending recruitment approval, not published externally");await repo.SaveAsync();
    }
    public Task DecideJobAsync(int id,bool approve,string reason)=>repo.AtomicAsync(async()=>
    {
        Need();var j=await repo.FirstOrDefaultAsync(repo.Query<JobOpening>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");if(j.ApprovalStatus!="Pending"||j.RequestedByUserId==me.UserId)throw new DomainException("err.forbidden");if(string.IsNullOrWhiteSpace(reason)||reason.Length>2000)throw new DomainException("err.reasonRequired");j.ApprovalStatus=approve?"Approved":"Rejected";j.ApprovedByUserId=me.UserId;j.ApprovalReason=reason.Trim();j.DecidedAt=clock.UtcNow;j.IsOpen=approve;audit.Log("DecideRequisition","Job",id,$"{j.ApprovalStatus}: {reason.Trim()}");await repo.SaveAsync();
    });
    public async Task CloseJobAsync(int id){Need();var j=await repo.FirstOrDefaultAsync(repo.Query<JobOpening>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");if(j.ApprovalStatus is not("Approved" or "Legacy"))throw new DomainException("err.forbidden");j.IsOpen=!j.IsOpen;audit.Log("ToggleOpening","Job",id,j.IsOpen?"Open":"Closed");await repo.SaveAsync();}
    public async Task AddCandidateAsync(int jobId, string name, string? email, string? note)
    {
        Need(); if (string.IsNullOrWhiteSpace(name)) throw new DomainException("err.required");
        if (!await repo.AnyAsync(repo.Query<JobOpening>().Where(x => x.Id == jobId && x.IsOpen && (x.ApprovalStatus=="Approved"||x.ApprovalStatus=="Legacy")))) throw new DomainException("err.notFound");
        repo.Add(new Candidate { JobOpeningId = jobId, Name = name.Trim(), Email = email?.Trim() ?? "", Note = note?.Trim() ?? "", CreatedAt = clock.UtcNow });
        await repo.SaveAsync();
    }
    public Task MoveCandidateAsync(int id, bool reject)=>repo.AtomicAsync(async()=>
    {
        Need(); var c = await repo.FirstOrDefaultAsync(repo.Query<Candidate>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
        if(c.Stage is CandidateStage.Hired or CandidateStage.Rejected)throw new DomainException("err.forbidden");
        if(!reject&&c.Stage==CandidateStage.Offer){var offers=await repo.ToListAsync(repo.Query<CandidateOffer>().Where(x=>x.CandidateId==c.Id));var latest=offers.OrderBy(x=>x.Id).LastOrDefault();if(latest==null||!await repo.AnyAsync(repo.Query<CandidateOfferEvent>().Where(x=>x.OfferId==latest.Id&&x.Kind=="Accepted")))throw new DomainException("err.forbidden");}
        c.InterviewStamp=Guid.NewGuid().ToString("N");
        if(reject){var offers=await repo.ToListAsync(repo.Query<CandidateOffer>().Where(x=>x.CandidateId==c.Id));var ids=offers.Select(x=>x.Id).ToList();var events=await repo.ToListAsync(repo.Query<CandidateOfferEvent>().Where(x=>ids.Contains(x.OfferId)).OrderBy(x=>x.Id));foreach(var offer in offers){var state=events.LastOrDefault(x=>x.OfferId==offer.Id)?.Kind??"Proposed";if(state is "Proposed" or "Approved")repo.Add(new CandidateOfferEvent{OfferId=offer.Id,Kind="Withdrawn",Note="Candidate moved to rejected stage",ActorUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});}}
        if (reject) c.Stage = CandidateStage.Rejected; else c.Stage = Candidate.Next(c.Stage) ?? c.Stage;
        repo.Add(new RecruitmentEvent{CandidateId=c.Id,Kind="StageChanged",Note=c.Stage.ToString(),Actor=me.Email,At=clock.UtcNow});
        audit.Log("Update", "Candidate", c.Name, c.Stage.ToString()); await repo.SaveAsync();
    });

    public async Task<List<PerformanceReview>> ReviewsAsync()
    {
        var ids = await scope.VisibleEmployeeIdsAsync();
        var q = repo.Query<PerformanceReview>().OrderByDescending(r => r.CreatedAt);
        var all = await repo.ToListAsync(q);
        return all.Where(r => ids == null || ids.Contains(r.EmployeeId)).ToList();
    }
    public Task AddReviewAsync(int employeeId, string period, string? goals, string? comment, int score) => throw new DomainException("err.usePerf");

    public async Task<TrainingBudgetSummary> TrainingBudgetAsync(int year)
    {
        Need();if(year<2000||year>9998)throw new DomainException("err.dates");
        var start=new DateTime(year,1,1);var end=start.AddYears(1);
        var courses=await repo.ToListAsync(repo.Query<TrainingCourse>().Where(x=>x.Date>=start&&x.Date<end).Select(x=>new {x.ApprovalStatus,x.Currency,x.PlannedBudget}));
        var priced=courses.Where(x=>x.PlannedBudget is >=0 && x.Currency.Length==3 && x.Currency.All(c=>c>='A'&&c<='Z')).ToList();
        var rows=priced.GroupBy(x=>new{Status=x.ApprovalStatus is "Pending" or "Approved" or "Rejected"?x.ApprovalStatus:"Legacy",x.Currency}).OrderBy(x=>x.Key.Status).ThenBy(x=>x.Key.Currency).Select(g=>new TrainingBudgetRow(g.Key.Status,g.Key.Currency,g.Count(),g.Sum(x=>x.PlannedBudget!.Value))).ToList();
        return new(year,courses.Count,courses.Count-priced.Count,rows);
    }

    public async Task<List<TrainingCourse>> CoursesAsync()
    {
        var courses=await repo.ToListAsync(repo.Query<TrainingCourse>().OrderBy(c=>c.Date));
        if(me.Can(Perm.TalentManage))return courses;
        return courses.Select(c=>new TrainingCourse{Id=c.Id,Title=c.Title,TitleAr=c.TitleAr,Hours=c.Hours,Date=c.Date,ApprovalStatus=c.ApprovalStatus}).ToList();
    }
    public async Task<List<TrainingParticipant>> EnrollmentsAsync() {
        var ids = me.Can(Perm.TalentManage) ? null : await scope.VisibleEmployeeIdsAsync();
        return await repo.ToListAsync(repo.Query<TrainingEnrollment>().Where(e => ids == null || ids.Contains(e.EmployeeId)).Select(e=>new TrainingParticipant(e.Id,e.CourseId,e.EmployeeId,e.Employee!.FullName,e.Employee.FullNameAr,e.Completed)));
    }
    public async Task AddCourseAsync(string title, string? titleAr, int hours, DateTime date,string? need=null,decimal? budget=null,string? currency=null)
    { Need(); if (string.IsNullOrWhiteSpace(title)||title.Length>200||hours<=0||hours>1000||date.Date<clock.Today||date.Year>9998||string.IsNullOrWhiteSpace(need)||need.Length>2000||budget<0||budget>1000000000||budget!=null&&(currency==null||currency.Length!=3||!currency.All(x=>x>='A'&&x<='Z'))) throw new DomainException("err.required"); repo.Add(new TrainingCourse { Title=title.Trim(),TitleAr=titleAr?.Trim()??"",Hours=hours,Date=date.Date,Need=need.Trim(),PlannedBudget=budget,Currency=budget==null?"":currency!,ApprovalStatus="Pending",CreatedByUserId=me.UserId });audit.Log("ProposeCourse","Training",title,"Pending course approval, planning only, no spending");await repo.SaveAsync(); }
    public Task DecideCourseAsync(int id,bool approve,string reason)=>repo.AtomicAsync(async()=>
    {Need();var course=await repo.FirstOrDefaultAsync(repo.Query<TrainingCourse>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");if(course.ApprovalStatus!="Pending"||course.CreatedByUserId==me.UserId)throw new DomainException("err.forbidden");if(string.IsNullOrWhiteSpace(reason)||reason.Length>2000)throw new DomainException("err.reasonRequired");course.ApprovalStatus=approve?"Approved":"Rejected";course.DecidedByUserId=me.UserId;course.DecidedAt=clock.UtcNow;course.DecisionReason=reason.Trim();audit.Log("DecideCourse","Training",id,$"{course.ApprovalStatus}: {reason.Trim()}");await repo.SaveAsync();});
    public Task EnrollAsync(int courseId, int employeeId)=>repo.AtomicAsync(async()=>
    {
        if (employeeId != me.EmployeeId) Need();
        if (!await repo.AnyAsync(repo.Query<TrainingCourse>().Where(x => x.Id == courseId && (x.ApprovalStatus=="Approved"||x.ApprovalStatus=="Legacy"))) || !await repo.AnyAsync(repo.Query<Employee>().Where(x => x.Id == employeeId))) throw new DomainException("err.notFound");
        var employee=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==employeeId))??throw new DomainException("err.notFound");
        if (await repo.AnyAsync(repo.Query<TrainingEnrollment>().Where(x => x.CourseId == courseId && x.EmployeeId == employeeId))) throw new DomainException("err.alreadyEnrolled");
        employee.LeaveRequestStamp=Guid.NewGuid().ToString("N");
        repo.Add(new TrainingEnrollment { CourseId = courseId, EmployeeId = employeeId });audit.Log("Enroll","Training",courseId,$"employee #{employeeId}"); await repo.SaveAsync();
    });
    public async Task CompleteAsync(int id) { Need(); var e = await repo.FirstOrDefaultAsync(repo.Query<TrainingEnrollment>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound"); e.Completed = true; await repo.SaveAsync(); }
}

