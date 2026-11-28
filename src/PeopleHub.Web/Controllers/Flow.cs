using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Application.Modules.Organization;
using PeopleHub.Application.Modules.Payroll;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Application.Modules.Reporting;
using PeopleHub.Application.Modules.Requests;
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
        ViewBag.Assignees=await svc.AssigneeCandidatesAsync();
        return View(rows);
    }

    [RequireStepUp,HttpPost] public Task<IActionResult> Reassign(string kind,int id,int stageIndex,int toUserId,string reason,int? participantEmployeeId=null)=>Do(()=>svc.ReassignAsync(kind,id,stageIndex,toUserId,reason,participantEmployeeId),"msg.saved",()=>Back("/Requests"));

    [HttpPost] public Task<IActionResult> Approve(string kind, int id, string? comment) => Do(() => svc.ApproveAsync(kind, id, comment), "msg.approved", () => Back("/Requests"));
    [HttpPost] public Task<IActionResult> Reject(string kind, int id, string? comment) => Do(() => svc.RejectAsync(kind, id, comment), "msg.rejected", () => Back("/Requests"));
    [HttpPost] public Task<IActionResult> Cancel(string kind, int id) => Do(() => svc.CancelAsync(kind, id), "msg.saved", () => Back("/Requests"));

    public async Task<IActionResult> Advances() => View(await svc.AdvancesAsync());

    [HttpPost]
    public Task<IActionResult> Advance(decimal amount, int installments, int startYear, int startMonth, string? reason) =>
        Do(() => svc.RequestAdvanceAsync(amount, installments, startYear, startMonth, reason), "msg.requestSent", () => RedirectToAction(nameof(Advances)));

    [RequireStepUp,HttpPost] public Task<IActionResult> CancelUnpaid(int id) => Do(() => svc.CancelUnpaidAdvanceAsync(id), "msg.saved", () => RedirectToAction(nameof(Advances)));
    [RequireStepUp,HttpPost] public Task<IActionResult> Settle(int id) => Do(() => svc.SettleAdvanceAsync(id), "msg.saved", () => RedirectToAction(nameof(Advances)));
}

public class PayrollController(Loc loc, PayrollService svc, AppClock clock) : BaseController(loc)
{
    public async Task<IActionResult> Index()
    {
        try { ViewBag.Now = clock.Today; ViewBag.Distinct = await svc.DistinctApproversAsync(); ViewBag.Prorate = await svc.ProrateJoiningAsync(); return View(await svc.RunsAsync()); }
        catch (DomainException) { return RedirectToAction(nameof(MySlips)); }
    }

    public async Task<IActionResult> Run(int id)
    {
        try { var (run, lines, hist) = await svc.RunAsync(id); ViewBag.Lines = lines; ViewBag.History = hist; ViewBag.Snapshots=await svc.SnapshotsAsync(id); return View(run); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return RedirectToAction(nameof(Index)); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(int year, int month)
    {
        try { if (!ModelState.IsValid) throw new DomainException("err.required"); var id = await svc.CreateAsync(year, month); TempData["ok"] = L["msg.saved"]; return RedirectToAction(nameof(Run), new { id }); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return RedirectToAction(nameof(Index)); }
    }

    private IActionResult Same(int id) => RedirectToAction(nameof(Run), new { id });
    [RequireStepUp,HttpPost] public Task<IActionResult> Recalc(int id) => Do(() => svc.RecalculateAsync(id), "msg.saved", () => Same(id));
    [RequireStepUp,HttpPost] public Task<IActionResult> Adjust(int id, int lineId, decimal bonus, decimal other, string? note) => Do(() => svc.AdjustLineAsync(lineId, bonus, other, note), "msg.saved", () => Same(id));
    [HttpPost] public Task<IActionResult> Review(int id) => Do(() => svc.ReviewAsync(id), "msg.saved", () => Same(id));
    [RequireStepUp,HttpPost] public Task<IActionResult> ProrateJoining(bool on) => Do(() => svc.SetProrateJoiningAsync(on), "msg.saved", () => RedirectToAction(nameof(Index)));
    [RequireStepUp,HttpPost] public Task<IActionResult> DistinctApprovers(bool on) => Do(() => svc.SetDistinctApproversAsync(on), "msg.saved", () => RedirectToAction(nameof(Index)));
    [RequireStepUp,HttpPost] public Task<IActionResult> FinanceApprove(int id) => Do(() => svc.FinanceApproveAsync(id), "msg.saved", () => Same(id));
    [RequireStepUp,HttpPost] public Task<IActionResult> Finalize(int id) => Do(() => svc.FinalizeAsync(id), "msg.saved", () => Same(id));
    [RequireStepUp,HttpPost] public Task<IActionResult> Paid(int id) => Do(() => svc.MarkPaidAsync(id), "msg.saved", () => Same(id));
    [HttpPost] public Task<IActionResult> SendBack(int id, string? reason) => Do(() => svc.SendBackAsync(id, reason), "msg.saved", () => Same(id));
    [HttpPost] public Task<IActionResult> Reopen(int id, string? reason) => Do(() => svc.ReopenAsync(id, reason), "msg.saved", () => Same(id));

    [RequireStepUp]
    public async Task<IActionResult> BankFile(int id)
    {
        try { var csv = await svc.BankFileAsync(id); return File(System.Text.Encoding.UTF8.GetBytes("\uFEFF" + csv), "text/csv", $"bank-transfer-{id}.csv"); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return Same(id); }
    }

    [RequireStepUp]
    public async Task<IActionResult> Snapshot(int id)
    {
        try {return File(System.Text.Encoding.UTF8.GetBytes(await svc.SnapshotFileAsync(id)),"application/json",$"payroll-snapshot-{id}.json");}
        catch(DomainException) {return Forbidden();}
    }

    public async Task<IActionResult> MySlips() => View(await svc.MySlipsAsync());

    public async Task<IActionResult> Slip(int id, bool print = false)
    {
        try { var (run, line) = await svc.SlipAsync(id); ViewBag.Run = run; ViewBag.Print = print; return View(line); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return RedirectToAction(nameof(MySlips)); }
    }
}

public class ReportsController(Loc loc, ReportService svc, AppClock clock, ICurrentUser me, ReportBuilderService builder, OrgService org) : BaseController(loc)
{
    public async Task<IActionResult> Index(string key = "employees", int? year = null, int? month = null)
    {
        if (!me.Can(Perm.Reports)) return Forbidden();
        var y = year ?? clock.Today.Year; var m = month ?? clock.Today.Month;
        ViewBag.Key = key; ViewBag.Year = y; ViewBag.Month = m;
        try { return View(await svc.BuildAsync(key, y, m)); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return Redirect("/"); }
    }

    public async Task<IActionResult> Builder(string dataset = "employees", string[]? cols = null, int? dept = null, bool run = false)
    {
        if (!me.Can(Perm.Reports)) return Forbidden();
        if (!ReportBuilderService.Columns.ContainsKey(dataset)) return BadRequest();
        ViewBag.Dataset = dataset; ViewBag.Cols = cols ?? ReportBuilderService.Columns[dataset]; ViewBag.Dept = dept; ViewBag.Depts = await org.DepartmentsAsync();
        try { return View(run ? await builder.RunAsync(dataset, cols, dept) : null); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return Redirect("/"); }
    }

    public async Task<IActionResult> BuilderFile(string dataset, string[]? cols, int? dept, string format = "xlsx")
    {
        if (!me.Can(Perm.Reports)) return Forbidden();
        if (!ReportBuilderService.Columns.ContainsKey(dataset ?? "")) return BadRequest();
        try {
            var t = await builder.RunAsync(dataset!, cols, dept);
            return format == "csv" ? File(System.Text.Encoding.UTF8.GetBytes(ReportService.ToCsv(t)), "text/csv", $"{t.Key}.csv")
                : File(XlsxWriter.Build(t.Key, t.Columns, t.Rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{t.Key}.xlsx");
        } catch (DomainException) { return BadRequest(); }
    }

    public async Task<IActionResult> Xlsx(string key, int? year, int? month) => await Export(key, year, month, false);
    public async Task<IActionResult> Csv(string key, int? year, int? month) => await Export(key, year, month, true);
    private async Task<IActionResult> Export(string key, int? year, int? month, bool csv)
    {
        if (!me.Can(Perm.Reports)) return Forbidden();
        try {
            var t = await svc.BuildAsync(key, year ?? clock.Today.Year, month ?? clock.Today.Month);
            return csv ? File(System.Text.Encoding.UTF8.GetBytes(ReportService.ToCsv(t)), "text/csv", $"report-{key}.csv")
                : File(XlsxWriter.Build(t.Key, t.Columns, t.Rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"report-{key}.xlsx");
        } catch (DomainException) { return BadRequest(); }
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

public class BankReconciliationController(Loc loc,BankReconciliation svc,PayrollService payroll):BaseController(loc)
{
 public async Task<IActionResult> Index(int id){try{var(run,lines,_)=await payroll.RunAsync(id);ViewBag.Run=run;ViewBag.Lines=lines;return View(await svc.ResultsAsync(id));}catch(DomainException){return Forbidden();}}
 [HttpPost,RequireStepUp]public Task<IActionResult> Record(int runId,int lineId,string outcome,decimal amount,string reference,string note)=>Do(()=>svc.RecordAsync(runId,lineId,outcome,amount,reference,note),"msg.saved",()=>RedirectToAction(nameof(Index),new{id=runId}));
}

public class SalaryStructuresController(Loc loc,SalaryStructures svc,EmployeeService employees):BaseController(loc)
{
 public async Task<IActionResult> Index(){try{ViewBag.Components=await employees.ComponentsAsync();ViewBag.History=await svc.HistoryAsync();ViewBag.Published=await svc.PublicationsAsync();return View(await svc.ListAsync());}catch(DomainException){return Forbidden();}}
 [HttpPost,RequireStepUp]public Task<IActionResult> Create(string name,int grade,decimal basic,string componentAmounts,string reason,EmploymentType? category=null)=>Do(()=>svc.CreateAsync(name,grade,basic,componentAmounts,reason,category),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost,RequireStepUp]public Task<IActionResult> Publish(int structureId)=>Do(()=>svc.PublishAsync(structureId),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost,RequireStepUp]public Task<IActionResult> Band(int componentId,decimal? min,decimal? max,string reason)=>Do(()=>svc.SetBandAsync(componentId,min,max,reason),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost,RequireStepUp]public Task<IActionResult> Apply(int employeeId,int structureId,string reason)=>Do(()=>svc.ApplyAsync(employeeId,structureId,reason),"msg.saved",()=>RedirectToAction(nameof(Index)));
}

public class PayrollJournalsController(Loc loc,PayrollJournals svc):BaseController(loc)
{
 public async Task<IActionResult> Index(int id){Response.Headers.CacheControl="no-store";try{return View(await svc.BuildAsync(id));}catch(DomainException){return Forbidden();}}
 public async Task<IActionResult> Csv(int id){try{Response.Headers.CacheControl="no-store";return File(System.Text.Encoding.UTF8.GetBytes("\uFEFF"+await svc.CsvAsync(id)),"text/csv",$"internal-cost-centre-{id}.csv");}catch(DomainException){return Forbidden();}}
}

public class PayrollAdjustmentsController(Loc loc,PayrollAdjustments svc):BaseController(loc)
{
 public async Task<IActionResult> Index(int id){Response.Headers.CacheControl="no-store";try{ViewBag.Target=id;ViewBag.Sources=await svc.SourcesAsync(id);return View(await svc.ListAsync(id));}catch(DomainException){return Forbidden();}}
 [RequireStepUp,HttpPost]public Task<IActionResult> Propose(int id,int sourceLineId,string kind,decimal amount,string reason)=>Do(()=>svc.ProposeAsync(sourceLineId,id,kind,amount,reason),"msg.saved",()=>RedirectToAction(nameof(Index),new{id}));
 [RequireStepUp,HttpPost]public Task<IActionResult> Decide(int id,int adjustmentId,bool approve,string reason)=>Do(()=>svc.DecideAsync(adjustmentId,approve,reason),"msg.saved",()=>RedirectToAction(nameof(Index),new{id}));
 [RequireStepUp,HttpPost]public Task<IActionResult> Cancel(int id,int adjustmentId,string reason)=>Do(()=>svc.CancelAsync(adjustmentId,reason),"msg.saved",()=>RedirectToAction(nameof(Index),new{id}));
 [RequireStepUp,HttpPost]public Task<IActionResult> Apply(int id,int adjustmentId,string reason)=>Do(()=>svc.ApplyAsync(adjustmentId,reason),"msg.saved",()=>RedirectToAction(nameof(Index),new{id}));
}

public class PayrollAnomaliesController(Loc loc,PayrollAnomalies svc):BaseController(loc)
{
 public async Task<IActionResult> Index(int id){Response.Headers.CacheControl="no-store";try{ViewBag.Target=id;ViewBag.Thresholds=await svc.ThresholdsAsync();return View(await svc.ScanAsync(id));}catch(DomainException){return Forbidden();}}
 [RequireStepUp,HttpPost]public Task<IActionResult> Thresholds(int id,decimal netChangePercent,int lateMinutes,int absentDays,decimal overtimeHours)=>Do(()=>svc.SetThresholdsAsync(netChangePercent,lateMinutes,absentDays,overtimeHours),"msg.saved",()=>RedirectToAction(nameof(Index),new{id}));
}
