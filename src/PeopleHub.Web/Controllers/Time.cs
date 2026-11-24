using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

public class AttendanceController(Loc loc, AttendanceService svc, OrgService org, EmployeeService emps, AppClock clock, ICurrentUser me, AttachmentService files) : BaseController(loc)
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
            return View(await svc.MonthAsync(eid.Value, y, m));
        }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return Redirect("/"); }
    }

    public async Task<IActionResult> Daily(DateTime? date, int? dept)
    {
        if (!me.Can(Perm.AttTeam)) return Forbidden();
        var d = (date ?? clock.Today).Date;
        ViewBag.Date = d; ViewBag.Dept = dept; ViewBag.Depts = await org.DepartmentsAsync();
        return View(await svc.DailyAsync(d, dept));
    }

    [HttpPost] public Task<IActionResult> CheckIn() => Do(svc.CheckInAsync, "msg.checkedIn", () => Back("/Attendance"));
    [HttpPost] public Task<IActionResult> CheckOut() => Do(svc.CheckOutAsync, "msg.checkedOut", () => Back("/Attendance"));

    [HttpPost]
    public Task<IActionResult> Manual(int employeeId, DateTime date, string? checkIn, string? checkOut, AttendanceSource source)
    {
        DateTime? T(string? s) => TimeSpan.TryParse(s, out var t) ? date.Date + t : null;
        return Do(() => svc.AddManualAsync(employeeId, date, T(checkIn), T(checkOut), source), "msg.saved", () => RedirectToAction(nameof(Daily), new { date }));
    }

    [HttpPost]
    public Task<IActionResult> Excuse(int employeeId, DateTime date, string? note) =>
        Do(() => svc.ExcuseAbsenceAsync(employeeId, date, note), "msg.saved", () => Back("/Attendance/Daily"));

    [HttpPost]
    public Task<IActionResult> Overtime(DateTime date, decimal hours, string? reason) =>
        Do(() => svc.RequestOvertimeAsync(date, hours, reason), "msg.requestSent", () => Redirect("/Requests"));

    [HttpPost]
    public async Task<IActionResult> Correction(DateTime date, string? checkIn, string? checkOut, string? reason, IFormFile? file)
    {
        DateTime? T(string? s) => TimeSpan.TryParse(s, out var t) ? date.Date + t : null;
        var bytes = await FileBytes(file);
        return await Do(() => { var (tok, nm) = files.Save(file?.FileName, bytes); return svc.RequestCorrectionAsync(date, T(checkIn), T(checkOut), reason, tok, nm); }, "msg.requestSent", () => Redirect("/Requests"));
    }
}

public class LeaveController(Loc loc, LeaveService svc, ApprovalService approvals, AppClock clock, ICurrentUser me, AttachmentService files) : BaseController(loc)
{
    public async Task<IActionResult> Index()
    {
        if (me.EmployeeId == null) return RedirectToAction(nameof(Team));
        var y = clock.Today.Year;
        ViewBag.Types = await svc.TypesAsync();
        ViewBag.Balances = await svc.BalancesAsync(me.EmployeeId.Value, y);
        ViewBag.Today = clock.Today;
        return View((await approvals.ListAsync("Leave", null, false)).Where(r => r.Employee.Id == me.EmployeeId).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Request(int typeId, DateTime start, DateTime end, string? reason, string? document, IFormFile? file)
    {
        var bytes = await FileBytes(file);
        return await Do(() => { var (tok, nm) = files.Save(file?.FileName, bytes); return svc.RequestAsync(typeId, start, end, reason, nm ?? document, tok); }, "msg.requestSent", () => RedirectToAction(nameof(Index)));
    }

    [HttpPost]
    public Task<IActionResult> CancelApproved(int id) => Do(() => svc.CancelApprovedAsync(id), "msg.saved", () => RedirectToAction(nameof(Index)));

    public async Task<IActionResult> Team()
    {
        ViewBag.Today = clock.Today;
        ViewBag.Upcoming = (await approvals.ListAsync("Leave", RequestStatus.Approved, false)).Where(r => r.Detail.Length > 0).Take(15).ToList();
        return View(await svc.OnLeaveAsync(clock.Today));
    }
}
