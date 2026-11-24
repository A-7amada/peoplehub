using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

public class PerfController(Loc loc, PerfService svc, ICurrentUser me, PayrollService payroll) : BaseController(loc)
{
    public async Task<IActionResult> Index(int? cycle)
    {
        await svc.MaintainAsync();
        var cycles = await svc.CyclesAsync(); ViewBag.Cycles = cycles;
        var c = cycles.FirstOrDefault(x => x.Id == cycle) ?? cycles.FirstOrDefault(); ViewBag.Cycle = c;
        return View(c == null ? new List<AppraisalView>() : await svc.ListAsync(c.Id));
    }
    public async Task<IActionResult> Appraisal(int id)
    {
        try { await svc.MaintainAsync(); ViewBag.Tools = await svc.ToolsAsync(id); return View(await svc.GetAsync(id)); } catch (DomainException ex) { TempData["err"] = L[ex.Message]; return Redirect("/Perf"); }
    }
    IActionResult Ap(int id) => Redirect("/Perf/Appraisal/" + id);
    [HttpPost] public Task<IActionResult> NewCycle(string name, DateTime start, DateTime end) => Do(() => svc.CreateCycleAsync(name, start, end), "msg.saved", () => Redirect("/Perf"));
    [HttpPost] public Task<IActionResult> Advance(int id) => Do(() => svc.AdvanceCycleAsync(id), "msg.saved", () => Redirect("/Perf?cycle=" + id));
    [HttpPost] public Task<IActionResult> AddGoal(int id, string title, string? target, int weight) => Do(() => svc.AddGoalAsync(id, title, target, weight), "msg.saved", () => Ap(id));
    [HttpPost] public Task<IActionResult> Progress(int id, int goalId, int progress, int? selfScore) => Do(() => svc.UpdateProgressAsync(goalId, progress, selfScore), "msg.saved", () => Ap(id));
    [HttpPost] public Task<IActionResult> Self(int id, int score, string? comment) => Do(() => svc.SubmitSelfAsync(id, score, comment), "msg.saved", () => Ap(id));
    [HttpPost]
    public Task<IActionResult> Manager(int id, int score, string? comment, IFormCollection f)
    {
        var gs = f.Keys.Where(k => k.StartsWith("g_") && int.TryParse(k[2..], out _)).ToDictionary(k => int.Parse(k[2..]), k => int.TryParse(f[k], out var v) ? v : 0);
        return Do(() => svc.SubmitManagerAsync(id, score, comment, gs), "msg.saved", () => Ap(id));
    }
    [HttpPost] public Task<IActionResult> Calibrate(int id, decimal score, string? note) => Do(() => svc.CalibrateAsync(id, score, note), "msg.saved", () => Ap(id));
    [HttpPost] public Task<IActionResult> AddDev(int id, string action, DateTime? due) => Do(() => svc.AddDevAsync(id, action, due), "msg.saved", () => Ap(id));
    [HttpPost] public Task<IActionResult> ToggleDev(int id, int itemId) => Do(() => svc.ToggleDevAsync(itemId), "msg.saved", () => Ap(id));

    [HttpPost] public Task<IActionResult> Configure(int id, int goalWeight, int managerWeight, int feedbackWeight, bool autoPhases, DateTime calibrationOn, DateTime closeOn, bool reminders) => Do(() => svc.ConfigureAsync(id, goalWeight, managerWeight, feedbackWeight, autoPhases, calibrationOn, closeOn, reminders), "msg.saved", () => Redirect("/Perf?cycle=" + id));
    [HttpPost] public Task<IActionResult> FeedbackPrivacy(int cycleId,int minimum) => Do(()=>svc.ConfigureFeedbackPrivacyAsync(cycleId,minimum),"msg.saved",()=>Redirect("/Perf?cycle="+cycleId));
    public async Task<IActionResult> Feedback() => View(await svc.FeedbackInboxAsync());
    [HttpPost] public Task<IActionResult> SubmitFeedback(int id, int score, string? comment) => Do(() => svc.FeedbackAsync(id, score, comment), "msg.saved", () => Redirect("/Perf/Feedback"));
    [HttpPost] public Task<IActionResult> Nominate(int id, int reviewerEmployeeId) => Do(() => svc.NominateAsync(id, reviewerEmployeeId), "msg.saved", () => Ap(id));
    public async Task<IActionResult> Templates() { try { return View(await svc.TemplatesAsync()); } catch (DomainException) { return Forbidden(); } }
    [HttpPost] public Task<IActionResult> AddTemplate(string name) => Do(() => svc.AddTemplateAsync(name), "msg.saved", () => Redirect("/Perf/Templates"));
    [HttpPost] public Task<IActionResult> AddCompetency(int templateId, string title, string? behaviour, int weight) => Do(() => svc.AddCompetencyAsync(templateId, title, behaviour, weight), "msg.saved", () => Redirect("/Perf/Templates"));
    [HttpPost] public Task<IActionResult> ApplyTemplate(int id, int templateId) => Do(() => svc.ApplyTemplateAsync(id, templateId), "msg.saved", () => Ap(id));
    [HttpPost] public Task<IActionResult> LinkCourse(int id, int courseId) => Do(() => svc.LinkCourseAsync(id, courseId), "msg.saved", () => Ap(id));
    [HttpPost] public Task<IActionResult> ProposeBonus(int id, decimal amount, string reason) => Do(() => svc.ProposeBonusAsync(id, amount, reason), "msg.saved", () => Ap(id));
    public async Task<IActionResult> Bonuses() { try { ViewBag.Runs = await payroll.RunsAsync(); return View(await svc.PendingBonusesAsync()); } catch (DomainException) { return Forbidden(); } }
    [HttpPost] public Task<IActionResult> ApplyBonus(int bonusId, int runId) => Do(() => svc.ApplyBonusAsync(bonusId, runId, payroll), "msg.saved", () => Redirect("/Perf/Bonuses"));
    [HttpPost] public async Task<IActionResult> Migrate() { if (!me.Can(Perm.TalentManage)) return Forbidden(); var n = await svc.MigrateLegacyAsync(); TempData["ok"] = "Imported: " + n; return Redirect("/Perf"); }
}
