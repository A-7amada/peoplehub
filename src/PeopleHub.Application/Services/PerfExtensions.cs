using PeopleHub.Domain.Entities;
namespace PeopleHub.Application.Services;

public record FeedbackView(string? ReviewerName,string? ReviewerNameAr,int? Score,string? Comment,bool Submitted);
public record PerfTools(List<FeedbackView> Feedback, List<Employee> Employees, List<CompetencyTemplate> Templates, List<CompetencyItem> Items, List<TrainingCourse> Courses, List<PerfBonus> Bonuses, decimal? FeedbackAverage);
public partial class PerfService
{
    private async Task<decimal?> ScoreAsync(PerfAppraisal a, List<PerfGoal> goals)
    {
        var c = a.Cycle ?? await CycleAsync(a.CycleId);
        var f = (await repo.ToListAsync(repo.Query<Feedback360>().Where(f => f.AppraisalId == a.Id))).Where(f => f.Score != null).ToList();
        decimal? avg = c.MinimumFeedbackReviewers is > 0 && f.Count >= c.MinimumFeedbackReviewers ? f.Average(f => (decimal)f.Score!.Value) : null;
        return PerfFormula.Calculate(PerfCalc.GoalScore(goals), a.ManagerScore, avg, c.GoalWeight, c.ManagerWeight, c.FeedbackWeight);
    }

    public async Task ConfigureAsync(int id, int goalWeight, int managerWeight, int feedbackWeight, bool autoPhases, DateTime calibrationOn, DateTime closeOn, bool reminders)
    {
        if (!Hr) throw new DomainException("err.forbidden"); var c = await CycleAsync(id);
        if (c.Status != CycleStatus.Open) throw new DomainException("err.forbidden");
        if (!PerfFormula.Valid(goalWeight, managerWeight, feedbackWeight) || calibrationOn.Date < c.Start.Date || closeOn.Date <= calibrationOn.Date) throw new DomainException("err.perfConfig");
        c.GoalWeight = goalWeight; c.ManagerWeight = managerWeight; c.FeedbackWeight = feedbackWeight; c.AutoPhases = autoPhases; c.CalibrationOn = calibrationOn.Date; c.CloseOn = closeOn.Date; c.Reminders = reminders;
        audit.Log("Configure", "PerfCycle", id, $"weights={goalWeight}/{managerWeight}/{feedbackWeight}, auto={autoPhases}, calibration={calibrationOn:yyyy-MM-dd}, close={closeOn:yyyy-MM-dd}"); await repo.SaveAsync();
    }

    public async Task<PerfTools> ToolsAsync(int id)
    {
        var v = await GetAsync(id);
        var f = await repo.ToListAsync(repo.Query<Feedback360>().Where(f => f.AppraisalId == id));
        bool self=v.A.EmployeeId==me.EmployeeId;
        var complete=f.Where(x=>x.SubmittedAt!=null && x.Score!=null).ToList();
        var threshold=v.A.Cycle!.MinimumFeedbackReviewers;
        bool canAggregate=me.Can(Perm.FeedbackAggregate) && (!self || v.A.Cycle.Status==CycleStatus.Closed);
        decimal? avg=canAggregate && threshold is > 0 && complete.Count>=threshold ? complete.Average(x=>(decimal)x.Score!.Value) : null;
        bool identity=!self && me.Can(Perm.FeedbackIdentity), comments=!self && me.Can(Perm.FeedbackComments);
        var views=identity || comments ? f.Select(x=>new FeedbackView(identity?x.Reviewer?.FullName:null,identity?x.Reviewer?.FullNameAr:null,comments?x.Score:null,comments?x.Comment:null,x.SubmittedAt!=null)).ToList() : new List<FeedbackView>();
        var employees = await repo.ToListAsync(repo.Query<Employee>().Where(e => e.Status == EmployeeStatus.Active));
        return new(views, employees, await repo.ToListAsync(repo.Query<CompetencyTemplate>()), await repo.ToListAsync(repo.Query<CompetencyItem>()), await repo.ToListAsync(repo.Query<TrainingCourse>().Where(c=>c.ApprovalStatus=="Approved"||c.ApprovalStatus=="Legacy")), await repo.ToListAsync(repo.Query<PerfBonus>().Where(b => b.AppraisalId == id)), avg);
    }

    public async Task ConfigureFeedbackPrivacyAsync(int cycleId,int minimum)
    {
        if(!me.Can(Perm.AdmRoles)) throw new DomainException("err.forbidden");
        var c=await CycleAsync(cycleId);
        if(c.Status!=CycleStatus.Open || minimum is <1 or >100) throw new DomainException("err.required");
        var appraisalIds=(await repo.ToListAsync(repo.Query<PerfAppraisal>().Where(a=>a.CycleId==cycleId))).Select(a=>a.Id).ToList();
        if(await repo.AnyAsync(repo.Query<Feedback360>().Where(f=>f.SubmittedAt!=null && appraisalIds.Contains(f.AppraisalId)))) throw new DomainException("err.forbidden");
        c.MinimumFeedbackReviewers=minimum;
        audit.Log("PrivacyPolicy","PerfCycle",cycleId,$"minimum reviewers={minimum}; role grants managed separately");await repo.SaveAsync();
    }

    public async Task NominateAsync(int id, int reviewerEmployeeId)
    {
        var v = await GetAsync(id); if (!v.CanManage || reviewerEmployeeId == v.A.EmployeeId) throw new DomainException("err.forbidden");
        if (!await repo.AnyAsync(repo.Query<Employee>().Where(e => e.Id == reviewerEmployeeId && e.Status == EmployeeStatus.Active))) throw new DomainException("err.notFound");
        if (await repo.AnyAsync(repo.Query<Feedback360>().Where(f => f.AppraisalId == id && f.ReviewerEmployeeId == reviewerEmployeeId))) throw new DomainException("err.duplicateFeedback");
        repo.Add(new Feedback360 { AppraisalId = id, ReviewerEmployeeId = reviewerEmployeeId });
        await notify.NotifyEmployeeAsync(reviewerEmployeeId, "notif.feedback", v.A.Employee!.FullName, "/Perf/Feedback");
        audit.Log("Nominate", "Appraisal", id, $"reviewer {reviewerEmployeeId}"); await repo.SaveAsync();
    }
    public async Task<List<(Feedback360 F, PerfAppraisal A)>> FeedbackInboxAsync()
    {
        var fs = await repo.ToListAsync(repo.Query<Feedback360>().Where(f => f.ReviewerEmployeeId == me.EmployeeId));
        var apps = await repo.ToListAsync(repo.Query<PerfAppraisal>());
        return fs.Select(f => (f, apps.First(a => a.Id == f.AppraisalId))).ToList();
    }
    public async Task FeedbackAsync(int id, int score, string? comment)
    {
        var f = await repo.FirstOrDefaultAsync(repo.Query<Feedback360>().Where(f => f.Id == id)) ?? throw new DomainException("err.notFound");
        var a = await repo.FirstOrDefaultAsync(repo.Query<PerfAppraisal>().Where(a => a.Id == f.AppraisalId)) ?? throw new DomainException("err.notFound");
        if (a.Cycle!.AutoPhases && clock.Today < a.Cycle.Start.Date) throw new DomainException("err.forbidden");
        if (f.ReviewerEmployeeId != me.EmployeeId || a.EmployeeId == me.EmployeeId || a.Cycle!.Status != CycleStatus.Open || f.SubmittedAt != null) throw new DomainException("err.forbidden");
        if (!PerformanceReview.ValidScore(score)) throw new DomainException("err.required");
        f.Score = score; f.Comment = comment?.Trim() ?? ""; f.SubmittedAt = clock.UtcNow; await repo.SaveAsync();
    }

    public async Task<List<(CompetencyTemplate T, List<CompetencyItem> Items)>> TemplatesAsync()
    {
        if (!Hr) throw new DomainException("err.forbidden");
        var ts = await repo.ToListAsync(repo.Query<CompetencyTemplate>()); var items = await repo.ToListAsync(repo.Query<CompetencyItem>());
        return ts.Select(t => (t, items.Where(i => i.TemplateId == t.Id).ToList())).ToList();
    }
    public async Task AddTemplateAsync(string name)
    { if (!Hr) throw new DomainException("err.forbidden"); if (string.IsNullOrWhiteSpace(name)) throw new DomainException("err.required"); repo.Add(new CompetencyTemplate { Name = name.Trim() }); await repo.SaveAsync(); }
    public async Task AddCompetencyAsync(int templateId, string title, string? behaviour, int weight)
    {
        if (!Hr) throw new DomainException("err.forbidden");
        if (string.IsNullOrWhiteSpace(title) || weight is < 1 or > 100 || !await repo.AnyAsync(repo.Query<CompetencyTemplate>().Where(t => t.Id == templateId))) throw new DomainException("err.required");
        var items = await repo.ToListAsync(repo.Query<CompetencyItem>().Where(i => i.TemplateId == templateId)); if (items.Sum(i => i.Weight) + weight > 100) throw new DomainException("err.weights");
        repo.Add(new CompetencyItem { TemplateId = templateId, Title = title.Trim(), Behaviour = behaviour?.Trim() ?? "", Weight = weight }); await repo.SaveAsync();
    }
    public async Task ApplyTemplateAsync(int id, int templateId)
    {
        var v = await GetAsync(id); if (!v.CanManage || v.A.ManagerDone || v.Goals.Count > 0) throw new DomainException("err.emptyGoals");
        var items = await repo.ToListAsync(repo.Query<CompetencyItem>().Where(i => i.TemplateId == templateId));
        if (items.Sum(i => i.Weight) != 100) throw new DomainException("err.weights100");
        foreach (var i in items) repo.Add(new PerfGoal { AppraisalId = id, Title = i.Title, Target = i.Behaviour, Weight = i.Weight, IsCompetency = true });
        audit.Log("Template", "Appraisal", id, $"template {templateId}"); await repo.SaveAsync();
    }

    public Task LinkCourseAsync(int id, int courseId)=>repo.AtomicAsync(async()=>
    {
        var v = await GetAsync(id); if (!(Hr || v.CanManage || v.A.Employee?.ManagerId == me.EmployeeId)) throw new DomainException("err.forbidden");
        var c = await repo.FirstOrDefaultAsync(repo.Query<TrainingCourse>().Where(c => c.Id == courseId)) ?? throw new DomainException("err.notFound");
        if(c.ApprovalStatus is not("Approved" or "Legacy"))throw new DomainException("err.forbidden");
        v.A.ResponseStamp=Guid.NewGuid().ToString("N");
        if (!await repo.AnyAsync(repo.Query<TrainingEnrollment>().Where(e => e.CourseId == courseId && e.EmployeeId == v.A.EmployeeId))) repo.Add(new TrainingEnrollment { CourseId = courseId, EmployeeId = v.A.EmployeeId });
        if (!await repo.AnyAsync(repo.Query<DevItem>().Where(d => d.AppraisalId == id && d.CourseId == courseId))) repo.Add(new DevItem { AppraisalId = id, CourseId = courseId, Action = c.Title, Due = c.Date });
        await notify.NotifyEmployeeAsync(v.A.EmployeeId, "notif.trainingLink", c.Title, "/Talent/Training"); await repo.SaveAsync();
    });

    public async Task ProposeBonusAsync(int id, decimal amount, string reason)
    {
        var v = await GetAsync(id); if (!Hr || v.A.EmployeeId == me.EmployeeId || v.A.Cycle!.Status != CycleStatus.Closed || v.Final == null) throw new DomainException("err.forbidden");
        if (amount <= 0 || string.IsNullOrWhiteSpace(reason)) throw new DomainException("err.required");
        if (await repo.AnyAsync(repo.Query<PerfBonus>().Where(b => b.AppraisalId == id))) throw new DomainException("err.bonusExists");
        repo.Add(new PerfBonus { AppraisalId = id, EmployeeId = v.A.EmployeeId, Amount = amount, Reason = reason.Trim(), ProposedBy = me.Email }); audit.Log("ProposeBonus", "Appraisal", id, $"{amount}: {reason}"); await repo.SaveAsync();
    }
    public async Task<List<PerfBonus>> PendingBonusesAsync()
    { if (!me.Can(Perm.PayPrepare)) throw new DomainException("err.forbidden"); return await repo.ToListAsync(repo.Query<PerfBonus>().Where(b => b.PayrollLineId == null)); }
    public async Task ApplyBonusAsync(int bonusId, int runId, PayrollService payroll)
    {
        if (!me.Can(Perm.PayPrepare)) throw new DomainException("err.forbidden");
        await repo.AtomicAsync(async () => {
        var b = await repo.FirstOrDefaultAsync(repo.Query<PerfBonus>().Where(b => b.Id == bonusId)) ?? throw new DomainException("err.notFound");
        if (b.PayrollLineId != null) throw new DomainException("err.bonusExists");
        var line = await repo.FirstOrDefaultAsync(repo.Query<PayrollLine>().Where(l => l.RunId == runId && l.EmployeeId == b.EmployeeId)) ?? throw new DomainException("err.noPayrollLine");
        var comps = await repo.ToListAsync(repo.Query<EmployeeSalaryComponent>().Where(c => c.EmployeeId == line.EmployeeId));
        var fixedDed = comps.Where(c => c.Component?.Type == ComponentType.Deduction).Sum(c => c.Amount);
        await payroll.AdjustLineAsync(line.Id, line.Bonus + b.Amount, line.OtherDeduction - fixedDed, $"Performance award #{b.Id}: {b.Reason}");
        b.PayrollLineId = line.Id; b.AppliedBy = me.Email; b.AppliedAt = clock.UtcNow;
        audit.Log("ApplyBonus", "Appraisal", b.AppraisalId, $"payroll line {line.Id}"); await repo.SaveAsync();
        });
    }

    public async Task<int> MigrateLegacyAsync()
    {
        // Called at startup and by HR. Preserve original rows and reviewer/comment/score; idempotent by source ID.
        var old = await repo.ToListAsync(repo.Query<PerformanceReview>()); var existing = await repo.ToListAsync(repo.Query<PerfAppraisal>());
        var count = 0;
        foreach (var group in old.GroupBy(r => r.Period))
        {
            var name = "Imported: " + group.Key;
            var c = await repo.FirstOrDefaultAsync(repo.Query<PerfCycle>().Where(c => c.Name == name));
            if (c == null) { c = new PerfCycle { Name = name, Start = group.Min(r => r.CreatedAt).Date, End = group.Max(r => r.CreatedAt).Date, Status = CycleStatus.Closed, GoalWeight = 0, ManagerWeight = 100, Reminders = false }; repo.Add(c); await repo.SaveAsync(); }
            foreach (var r in group.Where(r => !existing.Any(a => a.LegacyReviewId == r.Id)))
            {
                repo.Add(new PerfAppraisal { CycleId = c.Id, EmployeeId = r.EmployeeId, LegacyReviewId = r.Id, ManagerDone = true, ManagerScore = r.Score, ManagerComment = $"Original reviewer: {r.Reviewer}\n{r.Goals}\n{r.Comment}", FinalScore = r.Score }); count++;
            }
        }
        if (count > 0) { audit.Log("Migrate", "PerformanceReview", "legacy", $"{count} reviews, originals retained"); await repo.SaveAsync(); }
        return count;
    }

    public async Task MaintainAsync()
    {
        var cycles = await repo.ToListAsync(repo.Query<PerfCycle>()); var today = clock.Today;
        foreach (var c in cycles.Where(c => c.Status != CycleStatus.Closed))
        {
            var apps = await repo.ToListAsync(repo.Query<PerfAppraisal>().Where(a => a.CycleId == c.Id));
            if (c.AutoPhases && c.CalibrationOn != null && c.CloseOn != null)
            {
                var phase = PerfFormula.Phase(today, c.CalibrationOn.Value, c.CloseOn.Value);
                if (phase > c.Status)
                {
                    c.Status = phase; audit.Log("AutoPhase", "PerfCycle", c.Id, phase.ToString());
                    if (phase == CycleStatus.Closed) foreach (var a in apps) { await SealAsync(a); await notify.NotifyEmployeeAsync(a.EmployeeId, "notif.review", c.Name, "/Perf"); }
                }
            }
            if (c.Reminders && c.Status == CycleStatus.Open && today >= c.Start.Date)
            {
                foreach (var a in apps)
                {
                    if (!a.SelfDone) await ReminderAsync(c, a.EmployeeId, "self", a.Id);
                    if (!a.ManagerDone && a.Employee?.ManagerId is int mid) await ReminderAsync(c, mid, "manager", a.Id);
                }
                var fs = await repo.ToListAsync(repo.Query<Feedback360>().Where(f => f.SubmittedAt == null));
                foreach (var f in fs.Where(f => apps.Any(a => a.Id == f.AppraisalId))) await ReminderAsync(c, f.ReviewerEmployeeId, "feedback", f.AppraisalId);
            }
        }
        // Course completion is authoritative; linked development items follow its status.
        var dev = await repo.ToListAsync(repo.Query<DevItem>().Where(d => d.CourseId != null)); var allApps = await repo.ToListAsync(repo.Query<PerfAppraisal>()); var enroll = await repo.ToListAsync(repo.Query<TrainingEnrollment>());
        foreach (var d in dev) { var a = allApps.FirstOrDefault(a => a.Id == d.AppraisalId); if (a != null) d.Done = enroll.Any(e => e.EmployeeId == a.EmployeeId && e.CourseId == d.CourseId && e.Completed); }
        await repo.SaveAsync();
    }
    private async Task ReminderAsync(PerfCycle c, int employeeId, string kind, int appraisalId)
    {
        var k = kind + ":" + appraisalId; var day = clock.Today;
        if (await repo.AnyAsync(repo.Query<PerfReminder>().Where(r => r.CycleId == c.Id && r.EmployeeId == employeeId && r.Kind == k && r.Day == day))) return;
        repo.Add(new PerfReminder { CycleId = c.Id, EmployeeId = employeeId, Kind = k, Day = day });
        await notify.NotifyEmployeeAsync(employeeId, "notif.perfReminder", c.Name + " / " + kind, "/Perf");
    }
}
