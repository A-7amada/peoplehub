using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

public static class UserExt
{
    public static int? EmployeeId(this ClaimsPrincipal u) => int.TryParse(u.FindFirstValue("emp"), out var i) ? i : null;
}

public class AccountController(AuthService auth) : Controller
{
    [HttpGet, AllowAnonymous] public IActionResult Login() => View();

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(string email, string password)
    {
        var u = await auth.LoginAsync(email, password);
        if (u == null) { ViewBag.Error = true; return View(); }
        var claims = new List<Claim> { new(ClaimTypes.Name, u.Email), new(ClaimTypes.Role, u.Role), new("emp", u.EmployeeId?.ToString() ?? "") };
        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, AllowAnonymous]
    public IActionResult Culture(string culture, string? returnUrl)
    {
        culture = culture == "ar" ? "ar" : "en";
        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}

[Authorize]
public class HomeController(DashboardService dash, EmployeeService emps) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Me = User.EmployeeId() is { } id ? await emps.GetAsync(id) : null;
        return View(await dash.GetAsync());
    }

    [AllowAnonymous] public IActionResult Error() => View();
}

[Authorize(Roles = Roles.HR)]
public class EmployeesController(EmployeeService svc, Loc t) : Controller
{
    public async Task<IActionResult> Index(string? q, int? dept, EmployeeStatus? status)
    {
        ViewBag.Departments = await svc.DepartmentsAsync();
        ViewBag.Q = q; ViewBag.Dept = dept; ViewBag.Status = status;
        return View(await svc.SearchAsync(new EmployeeFilter(q, dept, status)));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        ViewBag.Departments = await svc.DepartmentsAsync();
        var e = id is null ? new Employee { HireDate = DateTime.UtcNow.Date } : await svc.GetAsync(id.Value);
        return e == null ? NotFound() : View(e);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Employee model)
    {
        ViewBag.Departments = await svc.DepartmentsAsync();
        try
        {
            if (model.Id == 0) await svc.SaveAsync(model);
            else
            {
                var e = await svc.GetAsync(model.Id);
                if (e == null) return NotFound();
                e.FullName = model.FullName; e.FullNameAr = model.FullNameAr ?? ""; e.Email = model.Email;
                e.JobTitle = model.JobTitle ?? ""; e.DepartmentId = model.DepartmentId; e.HireDate = model.HireDate;
                e.MonthlySalary = model.MonthlySalary; e.AnnualLeaveBalance = model.AnnualLeaveBalance;
                if (e.Status != EmployeeStatus.Terminated) e.Status = model.Status;
                await svc.SaveAsync(e);
            }
            TempData["ok"] = t["ok.saved"];
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex) { ViewBag.Error = t[ex.Message]; return View(model); }
    }

    [HttpPost]
    public async Task<IActionResult> Terminate(int id)
    {
        try { await svc.TerminateAsync(id); TempData["ok"] = t["ok.saved"]; }
        catch (DomainException ex) { TempData["err"] = t[ex.Message]; }
        return RedirectToAction(nameof(Index));
    }
}

[Authorize]
public class LeaveController(LeaveService svc, EmployeeService emps, Loc t) : Controller
{
    private bool IsHr => User.IsInRole(Roles.HR);

    public async Task<IActionResult> Index(LeaveStatus? status)
    {
        var me = User.EmployeeId();
        ViewBag.Me = me is { } id ? await emps.GetAsync(id) : null;
        ViewBag.IsHr = IsHr;
        ViewBag.Status = status;
        return View(await svc.ListAsync(IsHr ? null : me ?? -1, status));
    }

    [HttpPost]
    public async Task<IActionResult> Request(LeaveType type, DateTime from, DateTime to, string? reason)
    {
        try
        {
            await svc.RequestAsync(User.EmployeeId() ?? throw new DomainException("err.forbidden"), type, from, to, reason);
            TempData["ok"] = t["ok.saved"];
        }
        catch (DomainException ex) { TempData["err"] = t[ex.Message]; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = Roles.HR)]
    public Task<IActionResult> Approve(int id) => Act(() => svc.ApproveAsync(id));
    [HttpPost, Authorize(Roles = Roles.HR)]
    public Task<IActionResult> Reject(int id) => Act(() => svc.RejectAsync(id));
    [HttpPost]
    public Task<IActionResult> Cancel(int id) => Act(() => svc.CancelAsync(id, IsHr ? null : User.EmployeeId() ?? -1));

    private async Task<IActionResult> Act(Func<Task> a)
    {
        try { await a(); TempData["ok"] = t["ok.saved"]; }
        catch (DomainException ex) { TempData["err"] = t[ex.Message]; }
        return RedirectToAction(nameof(Index));
    }
}
