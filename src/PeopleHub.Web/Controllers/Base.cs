using Microsoft.AspNetCore.DataProtection;
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

    protected static async Task<byte[]?> FileBytes(IFormFile? f)
    {
        if (f == null || f.Length == 0) return null;
        if (f.Length > AttachmentService.MaxBytes) return new byte[AttachmentService.MaxBytes + 1]; // rejected by the service with a clear message
        using var ms = new MemoryStream(); await f.CopyToAsync(ms); return ms.ToArray();
    }

    protected IActionResult Back(string? fallback = null) =>
        Request.Headers.Referer.ToString() is { Length: > 0 } r && Uri.TryCreate(r, UriKind.Absolute, out var u) && u.Host == Request.Host.Host ? Redirect(u.PathAndQuery) : Redirect(fallback ?? "/");

    protected IActionResult Forbidden() { TempData["err"] = loc["err.forbidden"]; return Redirect("/"); }
}

public class AccountController(Loc loc, AuthService auth, AccountService accounts, Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dp, AppClock clock) : Controller
{
    private Microsoft.AspNetCore.DataProtection.IDataProtector Prot => dp.CreateProtector("peoplehub.2fa");

    private async Task SignIn(AppUser u)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, u.Email), new(ClaimTypes.Email, u.Email), new("uid", u.Id.ToString()) };
        if (u.EmployeeId != null) claims.Add(new("emp", u.EmployeeId.ToString()!));
        claims.AddRange(u.RoleArray.Select(r => new Claim(ClaimTypes.Role, r)));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, "cookie")));
    }

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Verify(string pending, string code)
    {
        try
        {
            var parts = Prot.Unprotect(pending ?? "").Split('|');
            if (parts.Length != 2 || new DateTime(long.Parse(parts[1]), DateTimeKind.Utc) < clock.UtcNow) throw new Exception();
            var u = await accounts.ByIdAsync(int.Parse(parts[0]));
            if (u?.TotpSecret != null && PeopleHub.Domain.Security.Totp.Verify(u.TotpSecret, code, clock.UtcNow)) { await SignIn(u); return Redirect("/"); }
        }
        catch { }
        ViewBag.Error = loc["err.badCode"]; ViewBag.Pending = pending; return View("Login");
    }

    [AllowAnonymous, HttpGet] public IActionResult Forgot() => View();
    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Forgot(string email)
    {
        var token = await accounts.RequestResetAsync(email, $"{Request.Scheme}://{Request.Host}");
        await Task.CompletedTask;
        ViewBag.Done = true; ViewBag.DemoLink = token == null ? null : $"/Account/Reset?token={token}"; // demo only: shown because no SMTP is configured
        return View();
    }
    [AllowAnonymous, HttpGet] public async Task<IActionResult> Reset(string token) { ViewBag.Token = token; ViewBag.Valid = await accounts.TokenValidAsync(token ?? ""); return View(); }
    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Reset(string token, string password)
    {
        try { if (await accounts.ResetAsync(token ?? "", password)) { TempData["ok"] = loc["msg.saved"]; return RedirectToAction(nameof(Login)); } ViewBag.Error = loc["err.badToken"]; }
        catch (DomainException ex) { ViewBag.Error = loc[ex.Message]; }
        ViewBag.Token = token; ViewBag.Valid = await accounts.TokenValidAsync(token ?? ""); return View();
    }

    [Authorize] public async Task<IActionResult> Security(string? setup) { ViewBag.User = await accounts.CurrentAsync(); ViewBag.Setup = setup == "1"; return View(); }
    [Authorize, HttpPost] public async Task<IActionResult> ChangePassword(string current, string next) { try { await accounts.ChangePasswordAsync(current, next); TempData["ok"] = loc["msg.saved"]; } catch (DomainException ex) { TempData["err"] = loc[ex.Message]; } return RedirectToAction(nameof(Security)); }
    [Authorize, HttpPost] public async Task<IActionResult> BeginTotp() { await accounts.BeginTotpAsync(); return RedirectToAction(nameof(Security), new { setup = 1 }); }
    [Authorize, HttpPost] public async Task<IActionResult> EnableTotp(string code) { try { await accounts.EnableTotpAsync(code); TempData["ok"] = loc["msg.saved"]; return RedirectToAction(nameof(Security)); } catch (DomainException ex) { TempData["err"] = loc[ex.Message]; return RedirectToAction(nameof(Security), new { setup = 1 }); } }
    [Authorize, HttpPost] public async Task<IActionResult> DisableTotp(string password) { try { await accounts.DisableTotpAsync(password); TempData["ok"] = loc["msg.saved"]; } catch (DomainException ex) { TempData["err"] = loc[ex.Message]; } return RedirectToAction(nameof(Security)); }

    [AllowAnonymous, HttpGet]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true ? Redirect("/") : View();

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var u = await auth.LoginAsync(email, password);
        if (u == null) { ViewBag.Error = loc["login.invalid"]; return View(); }
        if (u.TotpEnabled) { ViewBag.Pending = Prot.Protect($"{u.Id}|{clock.UtcNow.AddMinutes(5).Ticks}"); return View(); }
        await SignIn(u);
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
