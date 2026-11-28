using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Application.Modules.Leave;
using PeopleHub.Application.Modules.Organization;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Application.Modules.Requests;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

public class AttendanceController(Loc loc, AttendanceService svc, OrgService org, EmployeeService emps, AppClock clock, ICurrentUser me, AttachmentService files,AttendanceExceptions exceptions,DeviceService device) : BaseController(loc)
{
    public async Task<IActionResult> Index(int? year, int? month, int? id)
    {
        var eid = id ?? me.EmployeeId;
        if (eid == null) return RedirectToAction(nameof(Daily));
        var y = year ?? clock.Today.Year; var m = month ?? clock.Today.Month;
        try
        {
            ViewBag.Year = y; ViewBag.Month = m; ViewBag.Emp = await emps.GetAsync(eid.Value) ?? throw new DomainException("err.notFound");
            ViewBag.Today = eid == me.EmployeeId ? await svc.TodayRecordAsync() : null;
            ViewBag.Summary = (await svc.SummariesAsync([ViewBag.Emp], y, m))[eid.Value];
            ViewBag.Own = eid == me.EmployeeId;
            ViewBag.Evidence=await svc.EvidenceAsync(eid.Value,y,m);
            ViewBag.Locked=await svc.IsLockedAsync(eid.Value,y,m);
            return View(await svc.MonthAsync(eid.Value, y, m));
        }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return Redirect("/"); }
    }

    public async Task<IActionResult> Exceptions(DateTime? from,DateTime? to){var end=(to??clock.Today).Date;var start=(from??end.AddDays(-30)).Date;ViewBag.From=start;ViewBag.To=end;try{return View(await exceptions.GetAsync(start,end));}catch(DomainException ex){TempData["err"]=L[ex.Message];return RedirectToAction(nameof(Index));}}
    [HttpPost]public Task<IActionResult> Reprocess()=>Do(device.ReprocessAsync,"msg.saved",()=>RedirectToAction(nameof(Exceptions)));
    public async Task<IActionResult> Daily(DateTime? date, int? dept)
    {
        if (!me.Can(Perm.AttTeam)) return Forbidden();
        var d = (date ?? clock.Today).Date;
        ViewBag.Date = d; ViewBag.Dept = dept; ViewBag.Depts = await org.DepartmentsAsync();
        if (me.Can(Perm.AttManage)) { var rules = new Dictionary<EmploymentType, OvertimeRules>(); foreach (var ty in Enum.GetValues<EmploymentType>()) rules[ty] = await svc.OvertimeRulesAsync(ty); ViewBag.OtRules = rules; }
        return View(await svc.DailyAsync(d, dept));
    }

    [HttpPost,RequireStepUp]public Task<IActionResult> Lock(int employeeId,int year,int month,string reason)=>Do(()=>svc.LockAsync(employeeId,year,month,reason),"msg.saved",()=>RedirectToAction(nameof(Index),new{id=employeeId,year,month}));
    [HttpPost] public Task<IActionResult> CheckIn() => Do(svc.CheckInAsync, "msg.checkedIn", () => Back("/Attendance"));
    [HttpPost] public Task<IActionResult> CheckOut() => Do(svc.CheckOutAsync, "msg.checkedOut", () => Back("/Attendance"));

    [HttpPost]
    public Task<IActionResult> Manual(int employeeId, DateTime date, string? checkIn, string? checkOut, AttendanceSource source,string? reason)
    {
        return Do(() => svc.AddManualAsync(employeeId, date, ParseClock(date,checkIn), ParseClock(date,checkOut), source,reason), "msg.saved", () => RedirectToAction(nameof(Daily), new { date }));
    }

    [HttpPost]
    public Task<IActionResult> Excuse(int employeeId, DateTime date, string? note) =>
        Do(() => svc.ExcuseAbsenceAsync(employeeId, date, note), "msg.saved", () => Back("/Attendance/Daily"));

    [HttpPost]
    public Task<IActionResult> OvertimeRules(EmploymentType type, decimal? maxDaily, decimal? maxMonthly, decimal? weekdayRate, decimal? restDayRate) =>
        Do(() => svc.SetOvertimeRulesAsync(type, maxDaily, maxMonthly, weekdayRate, restDayRate), "msg.saved", () => Redirect("/Attendance/Daily"));

    [HttpPost]
    public Task<IActionResult> Overtime(DateTime date, decimal hours, string? reason) =>
        Do(() => svc.RequestOvertimeAsync(date, hours, reason), "msg.requestSent", () => Redirect("/Requests"));

    [HttpPost]
    public async Task<IActionResult> Correction(DateTime date, string? checkIn, string? checkOut, string? reason, IFormFile? file)
    {
        var bytes = await FileBytes(file);
        return await Do(() => { var (tok, nm) = files.Save(file?.FileName, bytes); return svc.RequestCorrectionAsync(date, ParseClock(date,checkIn), ParseClock(date,checkOut), reason, tok, nm); }, "msg.requestSent", () => Redirect("/Requests"));
    }
}

public class LeaveController(Loc loc, LeaveService svc, ApprovalService approvals, AppClock clock, ICurrentUser me, AttachmentService files, LeaveCalendar calendar) : BaseController(loc)
{
    public async Task<IActionResult> Index()
    {
        if (me.EmployeeId == null) return RedirectToAction(nameof(Team));
        var y = clock.Today.Year;
        ViewBag.Types = await svc.TypesAsync();
        ViewBag.Balances = await svc.BalancesAsync(me.EmployeeId.Value, y);
        ViewBag.Today = clock.Today;
        ViewBag.Ledger=await svc.TransactionsAsync(me.EmployeeId.Value,y);
        return View((await approvals.ListAsync("Leave", null, false)).Where(r => r.Employee.Id == me.EmployeeId).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Request(int typeId, DateTime start, DateTime end, string? reason, string? document, IFormFile? file, string? part, decimal? hours)
    {
        var bytes = await FileBytes(file);
        return await Do(() => { var (tok, nm) = files.Save(file?.FileName, bytes); return svc.RequestAsync(typeId, start, end, reason, nm ?? document, tok, part, hours); }, "msg.requestSent", () => RedirectToAction(nameof(Index)));
    }

    [HttpPost]
    public Task<IActionResult> CancelApproved(int id, string? reason) => Do(() => svc.CancelApprovedAsync(id,reason), "msg.saved", () => RedirectToAction(nameof(Index)));

    public async Task<IActionResult> Calendar(int? year,int? month,int? departmentId)
    {try{return View(await calendar.GetAsync(year??clock.Today.Year,month??clock.Today.Month,departmentId));}catch(DomainException ex){TempData["err"]=L[ex.Message];return RedirectToAction(nameof(Index));}}

    public async Task<IActionResult> Team()
    {
        ViewBag.Today = clock.Today;
        ViewBag.Upcoming = (await approvals.ListAsync("Leave", RequestStatus.Approved, false)).Where(r => r.Detail.Length > 0).Take(15).ToList();
        return View(await svc.OnLeaveAsync(clock.Today));
    }
}
