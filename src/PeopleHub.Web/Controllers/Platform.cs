using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

/// <summary>Push endpoint for a device bridge. A ZKTeco machine on a private LAN cannot be reached from the cloud, so a small bridge on that LAN reads the punches and posts them here with the device key.</summary>
[AllowAnonymous, IgnoreAntiforgeryToken, Route("api/device")]
public class DeviceApiController(DeviceService device, SettingsService settings) : Controller
{
    public record PunchDto(string UserId, DateTime Time);

    [HttpPost("punches")]
    public async Task<IActionResult> Punches([FromBody] List<PunchDto>? punches)
    {
        await settings.LoadAsync();
        var key = Request.Headers["X-Device-Key"].ToString(); var real = SettingsService.Get("zk.apiKey");
        if (string.IsNullOrEmpty(real) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(key), System.Text.Encoding.UTF8.GetBytes(real))) return Unauthorized(new { error = "bad device key" });
        if (!SettingsService.DeviceMode) return Conflict(new { error = "attendance source is set to System, device punches are ignored" });
        if (punches == null || punches.Count == 0 || punches.Count > 5000) return BadRequest(new { error = "send 1 to 5000 punches" });
        try { var n = await device.IngestAsync(punches.Select(p => new DeviceService.PunchIn(p.UserId, p.Time))); return Ok(new { accepted = n }); }
        catch(DomainException ex) { return BadRequest(new { error=ex.Message }); }
    }
}

public class TalentController(Loc loc, TalentService svc, OrgService org, EmployeeService emps, ICurrentUser me, AppClock clock, AccessScope scope,TrainingEvidence evidence) : BaseController(loc)
{
    public async Task<IActionResult> Recruitment()
    {
        if (!me.Can(Perm.TalentManage)) return Forbidden();
        ViewBag.Jobs = await svc.JobsAsync(); ViewBag.Depts = await org.DepartmentsAsync();
        return View(await svc.CandidatesAsync());
    }
    [HttpPost] public Task<IActionResult> AddJob(string title, string? titleAr, int? dept, int positions) => Do(() => svc.AddJobAsync(title, titleAr, dept, positions), "msg.saved", () => RedirectToAction(nameof(Recruitment)));
    [HttpPost] public Task<IActionResult> ToggleJob(int id) => Do(() => svc.CloseJobAsync(id), "msg.saved", () => RedirectToAction(nameof(Recruitment)));
    [HttpPost] public Task<IActionResult> AddCandidate(int jobId, string name, string? email, string? note) => Do(() => svc.AddCandidateAsync(jobId, name, email, note), "msg.saved", () => RedirectToAction(nameof(Recruitment)));
    [HttpPost] public Task<IActionResult> Move(int id, bool reject) => Do(() => svc.MoveCandidateAsync(id, reject), "msg.saved", () => RedirectToAction(nameof(Recruitment)));

    public IActionResult Performance() => Redirect("/Perf");
    [HttpPost] public Task<IActionResult> AddReview(int employeeId, string period, string? goals, string? comment, int score) => Do(() => svc.AddReviewAsync(employeeId, period, goals, comment, score), "msg.saved", () => RedirectToAction(nameof(Performance)));

    public async Task<IActionResult> Training()
    {
        ViewBag.Evidence = await evidence.GetAsync();
        ViewBag.Enrollments = await svc.EnrollmentsAsync();
        return View(await svc.CoursesAsync());
    }
    [HttpPost] public Task<IActionResult> AddCourse(string title, string? titleAr, int hours, DateTime date,string need,decimal? budget,string? currency) => Do(() => svc.AddCourseAsync(title, titleAr, hours, date,need,budget,currency), "msg.saved", () => RedirectToAction(nameof(Training)));
    [HttpPost] public Task<IActionResult> DecideCourse(int id,bool approve,string reason)=>Do(()=>svc.DecideCourseAsync(id,approve,reason),"msg.saved",()=>RedirectToAction(nameof(Training)));
    [HttpPost] public Task<IActionResult> Enroll(int courseId, int? employeeId) => Do(() => svc.EnrollAsync(courseId, employeeId ?? me.EmployeeId ?? 0), "msg.saved", () => RedirectToAction(nameof(Training)));
    [HttpPost] public Task<IActionResult> TrainingOutcome(int enrollmentId,string kind,string note)=>Do(()=>evidence.OutcomeAsync(enrollmentId,kind,note),"msg.saved",()=>RedirectToAction(nameof(Training)));
    [HttpPost] public async Task<IActionResult> TrainingCertificate(int enrollmentId,string issuer,string reference,DateTime issuedOn,DateTime? expiresOn,IFormFile? file){var data=await FileBytes(file);return await Do(()=>evidence.CertificateAsync(enrollmentId,issuer,reference,issuedOn,expiresOn,file?.FileName??"",data??[]),"msg.saved",()=>RedirectToAction(nameof(Training)));}
    [HttpPost] public Task<IActionResult> Complete(int id) => Do(() => svc.CompleteAsync(id), "msg.saved", () => RedirectToAction(nameof(Training)));

    [RequirePerm(Perm.EmpPay)]
    public async Task<IActionResult> Settlement(int? id, DateTime? lastDay, decimal leaveDays = 0)
    {
        ViewBag.Employees = await emps.ManagerCandidatesAsync();
        if (id != null)
        {
            var e = await emps.GetAsync(id.Value);
            if (e == null) { TempData["err"] = L["err.notFound"]; return RedirectToAction(nameof(Settlement)); }
            ViewBag.Emp = e;
            ViewBag.Last = (lastDay ?? clock.Today).Date; ViewBag.LeaveDays = leaveDays;
            try { ViewBag.Result = PeopleHub.Domain.Entities.Settlement.Calculate(e.BasicSalary, e.HireDate, (lastDay ?? clock.Today).Date, leaveDays, decimal.TryParse(SettingsService.Get("eosb.days"), out var d) ? d : 15); }
            catch (DomainException ex) { TempData["err"] = L[ex.Message]; }
        }
        return View();
    }
}
