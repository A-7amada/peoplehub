using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

[Authorize]
public abstract class BaseController(Loc loc) : Controller
{
    protected Loc L => loc;

    /// <summary>Runs an action, turning domain rule violations into a flash message.</summary>
    protected async Task<IActionResult> Do(Func<Task> work, string okKey, Func<IActionResult> next)
    {
        try { await work(); TempData["ok"] = loc[okKey]; }
        catch (DomainException ex) { TempData["err"] = loc[ex.Message]; }
        return next();
    }

    protected IActionResult Back(string? fallback = null) =>
        Request.Headers.Referer.ToString() is { Length: > 0 } r && Uri.TryCreate(r, UriKind.Absolute, out var u) && u.Host == Request.Host.Host ? Redirect(u.PathAndQuery) : Redirect(fallback ?? "/");

    protected IActionResult Forbidden() { TempData["err"] = loc["err.forbidden"]; return Redirect("/"); }
}

public class AccountController(Loc loc, AuthService auth) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true ? Redirect("/") : View();

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var u = await auth.LoginAsync(email, password);
        if (u == null) { ViewBag.Error = loc["login.invalid"]; return View(); }
        var claims = new List<Claim> { new(ClaimTypes.Name, u.Email), new(ClaimTypes.Email, u.Email), new("uid", u.Id.ToString()) };
        if (u.EmployeeId != null) claims.Add(new("emp", u.EmployeeId.ToString()!));
        claims.AddRange(u.RoleArray.Select(r => new Claim(ClaimTypes.Role, r)));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, "cookie")));
        return Redirect("/");
    }

    [HttpPost]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }

    [AllowAnonymous, HttpGet]
    public IActionResult Lang(string? to, string? returnUrl)
    {
        var c = to == "ar" ? "ar" : "en";
        Response.Cookies.Append(".AspNetCore.Culture", Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new(c)), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }

    [AllowAnonymous] public IActionResult Denied() => View();
}

public class HomeController(Loc loc, ReportService reports) : BaseController(loc)
{
    public async Task<IActionResult> Index() => View(await reports.DashboardAsync());
    [AllowAnonymous] public IActionResult Error() => View();
}
