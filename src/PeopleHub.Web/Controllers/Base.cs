using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Identity;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Application.Modules.Reporting;
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
        try { if (!ModelState.IsValid) throw new DomainException("err.required"); await work(); TempData["ok"] = loc[okKey]; }
        catch (DomainException ex) { TempData["err"] = loc[ex.Message]; }
        return next();
    }

    protected static DateTime? ParseClock(DateTime date,string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        if(!TimeSpan.TryParseExact(value,[@"h\:mm",@"hh\:mm",@"hh\:mm\:ss"],System.Globalization.CultureInfo.InvariantCulture,out var time)||time<TimeSpan.Zero||time>=TimeSpan.FromDays(1))throw new DomainException("err.required");
        return date.Date+time;
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

public class AccountController(Loc loc, AuthService auth, AccountService accounts, Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dp, AppClock clock, IConfiguration config,SessionService sessions) : Controller
{
    private Microsoft.AspNetCore.DataProtection.IDataProtector Prot => dp.CreateProtector("peoplehub.2fa");

    private async Task SignIn(AppUser u)
    {
        var sid=await sessions.StartAsync(u.Id,Request.Headers.UserAgent.ToString(),HttpContext.Connection.RemoteIpAddress?.ToString()??"");
        var claims = new List<Claim> { new(ClaimTypes.Name, u.Email), new(ClaimTypes.Email, u.Email), new("uid", u.Id.ToString()), new("stamp", u.SecurityStamp),new("sid",sid) };
        if (u.EmployeeId != null) claims.Add(new("emp", u.EmployeeId.ToString()!));
        claims.AddRange(u.RoleArray.Select(r => new Claim(ClaimTypes.Role, r)));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, "cookie")));
    }

    [AllowAnonymous, HttpPost]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Verify(string pending, string code)
    {
        try
        {
            var parts = Prot.Unprotect(pending ?? "").Split('|');
            if (parts.Length != 2 || new DateTime(long.Parse(parts[1]), DateTimeKind.Utc) < clock.UtcNow) throw new Exception();
            var u = await accounts.ByIdAsync(int.Parse(parts[0]));
            if (u != null && await auth.VerifySecondFactorAsync(u, code)) { await SignIn(u); return Redirect("/"); }
        }
        catch { }
        ViewBag.Error = loc["err.badCode"]; ViewBag.Pending = pending; return View("Login");
    }

    [AllowAnonymous, HttpGet] public IActionResult Forgot() => View();
    [AllowAnonymous, HttpPost]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Forgot(string email)
    {
        var baseUrl = config["PUBLIC_BASE_URL"];
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https")
            await accounts.RequestResetAsync(email, baseUrl!.TrimEnd('/'));
        ViewBag.Done = true;
        return View();
    }
    [AllowAnonymous, HttpGet] public async Task<IActionResult> Reset(string token) { ViewBag.Token = token; ViewBag.Valid = await accounts.TokenValidAsync(token ?? ""); return View(); }
    [AllowAnonymous, HttpPost]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Reset(string token, string password)
    {
        try { if (await accounts.ResetAsync(token ?? "", password)) { TempData["ok"] = loc["msg.saved"]; return RedirectToAction(nameof(Login)); } ViewBag.Error = loc["err.badToken"]; }
        catch (DomainException ex) { ViewBag.Error = loc[ex.Message]; }
        ViewBag.Token = token; ViewBag.Valid = await accounts.TokenValidAsync(token ?? ""); return View();
    }

    [Authorize,HttpGet]public async Task<IActionResult> StepUp(string? returnUrl){ViewBag.ReturnUrl=Url.IsLocalUrl(returnUrl)?returnUrl:"/";ViewBag.Totp=(await accounts.CurrentAsync())?.TotpEnabled==true;return View();}
    [Authorize,HttpPost,Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> StepUp(string password,string? code,string? returnUrl)
    {
      if(await sessions.ElevateAsync(User.FindFirst("sid")?.Value,password,code)){TempData["ok"]=loc.IsAr?"تم التحقق. أعد تنفيذ الإجراء بنفسك، لم يتم إرساله تلقائياً.":"Verified. Repeat the intended action yourself, nothing was submitted automatically.";return LocalRedirect(Url.IsLocalUrl(returnUrl)&&!returnUrl!.StartsWith("/Account/StepUp",StringComparison.OrdinalIgnoreCase)?returnUrl!:"/");}
      ViewBag.Error=loc.IsAr?"تعذر التحقق من بياناتك.":"Could not verify your credentials.";ViewBag.ReturnUrl=Url.IsLocalUrl(returnUrl)?returnUrl:"/";ViewBag.Totp=(await accounts.CurrentAsync())?.TotpEnabled==true;return View();
    }

    [Authorize] public async Task<IActionResult> Security(string? setup) { ViewBag.User = await accounts.CurrentAsync(); ViewBag.Setup = setup == "1";ViewBag.Sessions=await sessions.MineAsync();ViewBag.CurrentSessionHash=SessionService.Hash(User.FindFirst("sid")?.Value??""); return View(); }
    [Authorize,HttpPost]public async Task<IActionResult> RevokeSession(int id){try{await sessions.RevokeAsync(id);TempData["ok"]=loc["msg.saved"];}catch(DomainException ex){TempData["err"]=loc[ex.Message];}return RedirectToAction(nameof(Security));}
    [Authorize, HttpPost] public async Task<IActionResult> ChangePassword(string current, string next) { try { await accounts.ChangePasswordAsync(current, next); TempData["ok"] = loc["msg.saved"]; } catch (DomainException ex) { TempData["err"] = loc[ex.Message]; } return RedirectToAction(nameof(Security)); }
    [Authorize, HttpPost] public async Task<IActionResult> BeginTotp() { try { await accounts.BeginTotpAsync(); return RedirectToAction(nameof(Security), new { setup = 1 }); } catch (DomainException ex) { TempData["err"] = loc[ex.Message]; return RedirectToAction(nameof(Security)); } }
    [Authorize, HttpPost] public async Task<IActionResult> EnableTotp(string code) { try { await accounts.EnableTotpAsync(code); TempData["ok"] = loc["msg.saved"]; return RedirectToAction(nameof(Security)); } catch (DomainException ex) { TempData["err"] = loc[ex.Message]; return RedirectToAction(nameof(Security), new { setup = 1 }); } }
    [Authorize, HttpPost] public async Task<IActionResult> DisableTotp(string password) { try { await accounts.DisableTotpAsync(password); TempData["ok"] = loc["msg.saved"]; } catch (DomainException ex) { TempData["err"] = loc[ex.Message]; } return RedirectToAction(nameof(Security)); }

    [AllowAnonymous, HttpGet]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true ? Redirect("/") : View();

    [AllowAnonymous, HttpPost]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(string email, string password)
    {
        var u = await auth.LoginAsync(email, password);
        if (u == null) { ViewBag.Error = loc["login.invalid"]; return View(); }
        if (u.TotpEnabled) { ViewBag.Pending = Prot.Protect($"{u.Id}|{clock.UtcNow.AddMinutes(5).Ticks}"); return View(); }
        await SignIn(u);
        return Redirect("/");
    }

    [HttpPost]
    public async Task<IActionResult> Logout() { await sessions.RevokeCurrentAsync(User.FindFirst("sid")?.Value);await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }

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
