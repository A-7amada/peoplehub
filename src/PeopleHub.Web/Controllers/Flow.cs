using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

public class FilesController(Loc loc, AttachmentService files) : BaseController(loc)
{
    public async Task<IActionResult> Get(string id)
    {
        try { var a = await files.GetAsync(id); return File(a.Data, a.ContentType, a.FileName); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return Redirect("/"); }
    }
}

public class RequestsController(Loc loc, ApprovalService svc, AppClock clock, DelegationService deleg, EmployeeService emps) : BaseController(loc)
{
    public async Task<IActionResult> Delegations() { ViewBag.Employees = await emps.ManagerCandidatesAsync(); ViewBag.Today = clock.Today; return View(await deleg.MineAsync()); }
    [HttpPost] public Task<IActionResult> Delegate(int to, DateTime start, DateTime end) => Do(() => deleg.AddAsync(to, start, end), "msg.saved", () => RedirectToAction(nameof(Delegations)));
    [HttpPost] public Task<IActionResult> Revoke(int id) => Do(() => deleg.RevokeAsync(id), "msg.saved", () => RedirectToAction(nameof(Delegations)));

    public async Task<IActionResult> Index(string? scope, string? kind, RequestStatus? status)
    {
        scope ??= "mine";
        ViewBag.Scope = scope; ViewBag.Kind = kind; ViewBag.Status = status;
        ViewBag.Inbox = await svc.PendingForMeAsync();
        var rows = await svc.ListAsync(kind, status, scope == "inbox");
        if (scope == "mine") rows = rows.Where(r => r.Employee.Id == int.Parse(User.FindFirst("emp")?.Value ?? "-1")).ToList();
        ViewBag.Today = clock.Today;
        return View(rows);
    }

    [HttpPost] public Task<IActionResult> Approve(string kind, int id, string? comment) => Do(() => svc.ApproveAsync(kind, id, comment), "msg.approved", () => Back("/Requests"));
    [HttpPost] public Task<IActionResult> Reject(string kind, int id, string? comment) => Do(() => svc.RejectAsync(kind, id, comment), "msg.rejected", () => Back("/Requests"));
    [HttpPost] public Task<IActionResult> Cancel(string kind, int id) => Do(() => svc.CancelAsync(kind, id), "msg.saved", () => Back("/Requests"));

    public async Task<IActionResult> Advances() => View(await svc.AdvancesAsync());

    [HttpPost]
    public Task<IActionResult> Advance(decimal amount, int installments, int startYear, int startMonth, string? reason) =>
        Do(() => svc.RequestAdvanceAsync(amount, installments, startYear, startMonth, reason), "msg.requestSent", () => RedirectToAction(nameof(Advances)));

    [HttpPost] public Task<IActionResult> Settle(int id) => Do(() => svc.SettleAdvanceAsync(id), "msg.saved", () => RedirectToAction(nameof(Advances)));
}

public class PayrollController(Loc loc, PayrollService svc, AppClock clock) : BaseController(loc)
{
    public async Task<IActionResult> Index()
    {
        try { ViewBag.Now = clock.Today; return View(await svc.RunsAsync()); }
        catch (DomainException) { return RedirectToAction(nameof(MySlips)); }
    }

    public async Task<IActionResult> Run(int id)
    {
        try { var (run, lines, hist) = await svc.RunAsync(id); ViewBag.Lines = lines; ViewBag.History = hist; return View(run); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return RedirectToAction(nameof(Index)); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(int year, int month)
    {
        try { var id = await svc.CreateAsync(year, month); TempData["ok"] = L["msg.saved"]; return RedirectToAction(nameof(Run), new { id }); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return RedirectToAction(nameof(Index)); }
    }

    private IActionResult Same(int id) => RedirectToAction(nameof(Run), new { id });
    [HttpPost] public Task<IActionResult> Recalc(int id) => Do(() => svc.RecalculateAsync(id), "msg.saved", () => Same(id));
    [HttpPost] public Task<IActionResult> Adjust(int id, int lineId, decimal bonus, decimal other, string? note) => Do(() => svc.AdjustLineAsync(lineId, bonus, other, note), "msg.saved", () => Same(id));
    [HttpPost] public Task<IActionResult> Review(int id) => Do(() => svc.ReviewAsync(id), "msg.saved", () => Same(id));
    [HttpPost] public Task<IActionResult> FinanceApprove(int id) => Do(() => svc.FinanceApproveAsync(id), "msg.saved", () => Same(id));
    [HttpPost] public Task<IActionResult> Finalize(int id) => Do(() => svc.FinalizeAsync(id), "msg.saved", () => Same(id));
    [HttpPost] public Task<IActionResult> Paid(int id) => Do(() => svc.MarkPaidAsync(id), "msg.saved", () => Same(id));
    [HttpPost] public Task<IActionResult> SendBack(int id, string? reason) => Do(() => svc.SendBackAsync(id, reason), "msg.saved", () => Same(id));
    [HttpPost] public Task<IActionResult> Reopen(int id, string? reason) => Do(() => svc.ReopenAsync(id, reason), "msg.saved", () => Same(id));

    public async Task<IActionResult> BankFile(int id)
    {
        try { var csv = await svc.BankFileAsync(id); return File(System.Text.Encoding.UTF8.GetBytes("\uFEFF" + csv), "text/csv", $"bank-transfer-{id}.csv"); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return Same(id); }
    }

    public async Task<IActionResult> MySlips() => View(await svc.MySlipsAsync());

    public async Task<IActionResult> Slip(int id, bool print = false)
    {
        try { var (run, line) = await svc.SlipAsync(id); ViewBag.Run = run; ViewBag.Print = print; return View(line); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return RedirectToAction(nameof(MySlips)); }
    }
}

public class ReportsController(Loc loc, ReportService svc, AppClock clock, ICurrentUser me) : BaseController(loc)
{
    public async Task<IActionResult> Index(string key = "employees", int? year = null, int? month = null)
    {
        if (!me.Can(Perm.Reports)) return Forbidden();
        var y = year ?? clock.Today.Year; var m = month ?? clock.Today.Month;
        ViewBag.Key = key; ViewBag.Year = y; ViewBag.Month = m;
        try { return View(await svc.BuildAsync(key, y, m)); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return Redirect("/"); }
    }

    public async Task<IActionResult> Xlsx(string key, int? year, int? month)
    {
        if (!me.Can(Perm.Reports)) return Forbidden();
        var t = await svc.BuildAsync(key, year ?? clock.Today.Year, month ?? clock.Today.Month);
        return File(XlsxWriter.Build(t.Key, t.Columns, t.Rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"report-{key}.xlsx");
    }

    public async Task<IActionResult> Csv(string key, int? year, int? month)
    {
        var t = await svc.BuildAsync(key, year ?? clock.Today.Year, month ?? clock.Today.Month);
        return File(System.Text.Encoding.UTF8.GetBytes(ReportService.ToCsv(t)), "text/csv", $"report-{key}.csv");
    }
}

public class SystemController(Loc loc, NotificationService notes, AuditService audit) : BaseController(loc)
{
    public async Task<IActionResult> Notifications()
    {
        var list = await notes.ForMeAsync();
        var copy = list.Select(n => (n.Id, n.TitleKey, n.Details, n.Link, n.CreatedAt, n.IsRead)).ToList();
        await notes.MarkAllReadAsync();
        return View(copy);
    }

    [RequirePerm(Perm.Audit)]
    public async Task<IActionResult> Audit(string? entity) { ViewBag.Entity = entity; return View(await audit.RecentAsync(entity)); }
}
