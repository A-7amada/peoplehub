using System.Collections.Concurrent;
using System.Net.Sockets;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Domain.Security;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Talent;

public record AppraisalView(PerfAppraisal A, List<PerfGoal> Goals, List<DevItem> Dev, decimal? GoalScore, decimal? Computed, decimal? Final, bool CanSelf, bool CanManage, bool CanCalibrate, bool Visible);

public partial class PerfService(IRepository repo, ICurrentUser me, AccessScope scope, AppClock clock, AuditService audit, NotificationService notify)
{
    private bool Hr => me.Can(Perm.TalentManage);
    public Task<List<PerfCycle>> CyclesAsync() => repo.ToListAsync(repo.Query<PerfCycle>().OrderBy(c => c.Name.StartsWith("Imported:")).ThenByDescending(c => c.Id));

    public async Task<PerfCycle> CycleAsync(int id) => await repo.FirstOrDefaultAsync(repo.Query<PerfCycle>().Where(c => c.Id == id)) ?? throw new DomainException("err.notFound");

    public Task CreateCycleAsync(string name, DateTime start, DateTime end)=>repo.AtomicAsync(async()=>
    {
        if (!Hr) throw new DomainException("err.forbidden");
        if (string.IsNullOrWhiteSpace(name) || start.Year < 2000 || end.Year < 2000 || end < start) throw new DomainException("err.required");
        var c = new PerfCycle { Name = name.Trim(), Start = start, End = end, Status = CycleStatus.Open }; repo.Add(c); await repo.SaveAsync();
        foreach (var e in (await repo.ToListAsync(repo.Query<Employee>())).Where(e => e.IsWorking && e.HireDate <= end.Date))
            repo.Add(new PerfAppraisal { CycleId = c.Id, EmployeeId = e.Id });
        audit.Log("Create", "PerfCycle", name, ""); await repo.SaveAsync();
        await notify.NotifyPermAsync(Perm.ScopeTeam, "notif.perfCycle", name, "/Perf"); await repo.SaveAsync();
    });

    public Task AdvanceCycleAsync(int id)=>repo.AtomicAsync(async()=>
    {
        if (!Hr) throw new DomainException("err.forbidden"); var c = await CycleAsync(id);
        if (c.Status == CycleStatus.Open) c.Status = CycleStatus.Calibration;
        else if (c.Status == CycleStatus.Calibration)
        {
            c.Status = CycleStatus.Closed;
            foreach (var a in await repo.ToListAsync(repo.Query<PerfAppraisal>().Where(a => a.CycleId == id)))
            { await SealAsync(a); await notify.NotifyEmployeeAsync(a.EmployeeId, "notif.review", c.Name, "/Perf"); }
        }
        audit.Log("Update", "PerfCycle", c.Name, c.Status.ToString()); await repo.SaveAsync();
    });

    private async Task SealAsync(PerfAppraisal a)
    {
        var goals = await repo.ToListAsync(repo.Query<PerfGoal>().Where(g => g.AppraisalId == a.Id));
        if (a.ManagerDone) a.FinalScore ??= await ScoreAsync(a, goals);
    }

    public async Task<List<AppraisalView>> ListAsync(int cycleId)
    {
        var ids = await scope.VisibleEmployeeIdsAsync();
        var apps = (await repo.ToListAsync(repo.Query<PerfAppraisal>().Where(a => a.CycleId == cycleId))).Where(a => ids == null || ids.Contains(a.EmployeeId) || a.EmployeeId == me.EmployeeId).ToList();
        var res = new List<AppraisalView>(); foreach (var a in apps) { var v = await BuildAsync(a); if (v.Visible) res.Add(PublicView(v)); } return res;
    }

    public async Task<AppraisalView> GetAsync(int id)=>PublicView(await LoadViewAsync(id));

    private async Task<AppraisalView> LoadViewAsync(int id)
    {
        var a = await repo.FirstOrDefaultAsync(repo.Query<PerfAppraisal>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
        var v = await BuildAsync(a); if (!v.Visible) throw new DomainException("err.forbidden"); return v;
    }

    private AppraisalView PublicView(AppraisalView source)
    {
        T Copy<T>(T value)=>System.Text.Json.JsonSerializer.Deserialize<T>(System.Text.Json.JsonSerializer.Serialize(value))!;
        var a=Copy(source.A);var goals=source.Goals.Select(Copy).ToList();
        if(a.Employee is {} e)a.Employee=new Employee{Id=e.Id,Code=e.Code,FullName=e.FullName,FullNameAr=e.FullNameAr,ManagerId=e.ManagerId,JobTitleId=e.JobTitleId,JobTitle=e.JobTitle==null?null:new JobTitle{Id=e.JobTitle.Id,Name=e.JobTitle.Name,NameAr=e.JobTitle.NameAr,Grade=e.JobTitle.Grade}};
        bool hide=a.EmployeeId==me.EmployeeId&&a.Cycle!.Status!=CycleStatus.Closed;
        if(hide){a.ManagerScore=null;a.ManagerComment="";a.CalibrationNote="";a.FinalScore=null;foreach(var g in goals)g.ManagerScore=null;}
        return source with{A=a,Goals=goals,Dev=source.Dev.Select(Copy).ToList(),GoalScore=hide?null:source.GoalScore,Computed=hide?null:source.Computed,Final=hide?null:source.Final};
    }

    private async Task<AppraisalView> BuildAsync(PerfAppraisal a)
    {
        var goals = await repo.ToListAsync(repo.Query<PerfGoal>().Where(g => g.AppraisalId == a.Id).OrderBy(g => g.Id));
        var dev = await repo.ToListAsync(repo.Query<DevItem>().Where(d => d.AppraisalId == a.Id).OrderBy(d => d.Id));
        var ids = await scope.VisibleEmployeeIdsAsync();
        var cycle = a.Cycle ?? await CycleAsync(a.CycleId); var open = cycle.Status == CycleStatus.Open && (!cycle.AutoPhases || clock.Today >= cycle.Start.Date);
        bool self = a.EmployeeId == me.EmployeeId, mgr = me.EmployeeId!=null && a.Employee?.ManagerId == me.EmployeeId && !self;
        bool visible = Hr || self || mgr;
        var gs = PerfCalc.GoalScore(goals); var comp = await ScoreAsync(a, goals);
        // before the cycle closes an employee sees only their own input, never the manager's score
        return new(a, goals, dev, gs, comp, a.FinalScore ?? (cycle.Status == CycleStatus.Closed ? comp : null),
            self && open && !a.SelfDone, (mgr || Hr) && !self && open, Hr && cycle.Status == CycleStatus.Calibration && !self, visible);
    }

    private async Task<(PerfAppraisal A, PerfCycle C)> LoadAsync(int id)
    {
        var a = await repo.FirstOrDefaultAsync(repo.Query<PerfAppraisal>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
        return (a, await CycleAsync(a.CycleId));
    }

    public async Task AddGoalAsync(int appraisalId, string title, string? target, int weight)
    {
        var v = await LoadViewAsync(appraisalId); if (!v.CanManage) throw new DomainException("err.forbidden");
        if (string.IsNullOrWhiteSpace(title) || weight is < 1 or > 100) throw new DomainException("err.required");
        if (v.A.ManagerDone) throw new DomainException("err.forbidden");
        if (v.Goals.Sum(g => g.Weight) + weight > 100) throw new DomainException("err.weights");
        repo.Add(new PerfGoal { AppraisalId = appraisalId, Title = title.Trim(), Target = target?.Trim() ?? "", Weight = weight });
        await notify.NotifyEmployeeAsync(v.A.EmployeeId, "notif.goal", title, $"/Perf/Appraisal/{appraisalId}"); await repo.SaveAsync();
    }

    public async Task UpdateProgressAsync(int goalId, int progress, int? selfScore)
    {
        var g = await repo.FirstOrDefaultAsync(repo.Query<PerfGoal>().Where(x => x.Id == goalId)) ?? throw new DomainException("err.notFound");
        var v = await LoadViewAsync(g.AppraisalId); if (!v.CanSelf && !v.CanManage) throw new DomainException("err.forbidden");
        g.Progress = Math.Clamp(progress, 0, 100); if (v.CanSelf && selfScore is >= 1 and <= 5) g.SelfScore = selfScore; await repo.SaveAsync();
    }

    public async Task SubmitSelfAsync(int id, int score, string? comment)
    {
        var v = await LoadViewAsync(id); if (!v.CanSelf) throw new DomainException("err.forbidden");
        if (!PerformanceReview.ValidScore(score)) throw new DomainException("err.required");
        v.A.SelfScore = score; v.A.SelfComment = comment?.Trim() ?? ""; v.A.SelfDone = true; await repo.SaveAsync();
    }

    public async Task SubmitManagerAsync(int id, int score, string? comment, Dictionary<int, int> goalScores)
    {
        var v = await LoadViewAsync(id); if (!v.CanManage) throw new DomainException("err.forbidden");
        if (!PerformanceReview.ValidScore(score)) throw new DomainException("err.required");
        if (v.Goals.Count > 0 && !PerfCalc.WeightsOk(v.Goals)) throw new DomainException("err.weights100");
        if (v.Goals.Any(g => !goalScores.TryGetValue(g.Id, out var s) || !PerformanceReview.ValidScore(s))) throw new DomainException("err.required");
        foreach (var g in v.Goals) g.ManagerScore = goalScores[g.Id];
        v.A.ManagerScore = score; v.A.ManagerComment = comment?.Trim() ?? ""; v.A.ManagerDone = true;
        audit.Log("Update", "Appraisal", v.A.Id.ToString(), $"manager score {score}"); await repo.SaveAsync();
    }

    public async Task CalibrateAsync(int id, decimal score, string? note)
    {
        var v = await LoadViewAsync(id); if (!v.CanCalibrate) throw new DomainException("err.forbidden");
        if (!v.A.ManagerDone) throw new DomainException("err.managerFirst");
        if (score < 1 || score > 5 || string.IsNullOrWhiteSpace(note)) throw new DomainException("err.calibNote");
        v.A.FinalScore = Math.Round(score, 2); v.A.CalibrationNote = note.Trim();
        audit.Log("Update", "Appraisal", v.A.Id.ToString(), $"calibrated to {score}: {note}"); await repo.SaveAsync();
    }

    public async Task AddDevAsync(int id, string action, DateTime? due)
    {
        var v = await LoadViewAsync(id); if (!(v.CanManage || Hr || v.A.Employee?.ManagerId == me.EmployeeId || v.A.EmployeeId == me.EmployeeId)) throw new DomainException("err.forbidden");
        if (string.IsNullOrWhiteSpace(action)) throw new DomainException("err.required");
        repo.Add(new DevItem { AppraisalId = id, Action = action.Trim(), Due = due }); await repo.SaveAsync();
    }
    public async Task ToggleDevAsync(int itemId)
    {
        var d = await repo.FirstOrDefaultAsync(repo.Query<DevItem>().Where(x => x.Id == itemId)) ?? throw new DomainException("err.notFound");
        if (d.CourseId != null) throw new DomainException("err.courseManaged");
        var v = await LoadViewAsync(d.AppraisalId);
        if (!(Hr || v.A.EmployeeId == me.EmployeeId || v.A.Employee?.ManagerId == me.EmployeeId)) throw new DomainException("err.forbidden");
        d.Done = !d.Done; await repo.SaveAsync();
    }
}

