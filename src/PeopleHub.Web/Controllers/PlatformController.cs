using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Common;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

/// <summary>Platform staff area (ADR-0023 stage 3): sign in, then list, create, suspend, reactivate and archive customer companies.
/// Customer pages never accept a platform session (the cookie check in Program.cs), and this area shows no customer data.</summary>
public class PlatformController(Loc loc, PlatformAuthService auth, TenantAdminService tenants, AppClock clock) : Controller
{
    bool IsStaff => User.FindFirst("platform")?.Value == "1";

    [AllowAnonymous, HttpGet]
    public IActionResult Login() => IsStaff ? RedirectToAction(nameof(Index)) : View();

    [AllowAnonymous, HttpPost]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(string email, string password, string? code)
    {
        var u = await auth.LoginAsync(email, password, clock.UtcNow, code);
        if (u == null) { ViewBag.Error = loc["login.invalid"]; return View(); }
        var claims = new List<Claim> { new(ClaimTypes.Name, u.Email), new(ClaimTypes.Email, u.Email), new("uid", u.Id.ToString()), new("stamp", u.SecurityStamp), new("platform", "1") };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, "cookie")));
        return RedirectToAction(nameof(Index));
    }


    int? Uid => int.TryParse(User.FindFirst("uid")?.Value, out var i) ? i : null;

    [Authorize, HttpGet]
    public async Task<IActionResult> Security()
    {
        if (!IsStaff || Uid is not { } id) return Forbid();
        Response.Headers.CacheControl = "no-store, private";
        ViewBag.Me = await auth.FindAsync(id); ViewBag.Codes = TempData["codes"] as string;
        return View();
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> BeginTotp()
    {
        if (!IsStaff || Uid is not { } id) return Forbid();
        try { await auth.BeginTotpAsync(id); } catch (DomainException e) { TempData["err"] = loc[e.Message]; }
        return RedirectToAction(nameof(Security));
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> EnableTotp(string code)
    {
        if (!IsStaff || Uid is not { } id) return Forbid();
        try
        {
            var codes = await auth.EnableTotpAsync(id, code, clock.UtcNow);
            var u = await auth.FindAsync(id);
            var claims = new List<Claim> { new(ClaimTypes.Name, u!.Email), new(ClaimTypes.Email, u.Email), new("uid", u.Id.ToString()), new("stamp", u.SecurityStamp), new("platform", "1") };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, "cookie")));
            TempData["codes"] = string.Join("  ", codes);
        }
        catch (DomainException e) { TempData["err"] = loc[e.Message]; }
        return RedirectToAction(nameof(Security));
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> DisableTotp(string password)
    {
        if (!IsStaff || Uid is not { } id) return Forbid();
        try
        {
            await auth.DisableTotpAsync(id, password);
            var u = await auth.FindAsync(id);
            var claims = new List<Claim> { new(ClaimTypes.Name, u!.Email), new(ClaimTypes.Email, u.Email), new("uid", u.Id.ToString()), new("stamp", u.SecurityStamp), new("platform", "1") };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, "cookie")));
            TempData["ok"] = loc["msg.saved"];
        }
        catch (DomainException e) { TempData["err"] = loc[e.Message]; }
        return RedirectToAction(nameof(Security));
    }

    [HttpPost] public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }

    [Authorize, HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsStaff) return Forbid();
        ViewBag.Audit = await tenants.AuditAsync();
        return View(await tenants.ListAsync());
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Create(string name, string slug, string adminEmail, string adminPassword, string reason)
    {
        if (!IsStaff) return Forbid();
        try { await tenants.CreateAsync(name, slug, adminEmail, adminPassword, User.Identity?.Name ?? "", reason); TempData["ok"] = loc["msg.saved"]; }
        catch (DomainException e) { TempData["err"] = loc[e.Message]; }
        return RedirectToAction(nameof(Index));
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> SetStatus(int id, string to, string reason)
    {
        if (!IsStaff) return Forbid();
        var who = User.Identity?.Name ?? "";
        try
        {
            if (to == "Suspended") await tenants.SuspendAsync(id, who, reason); else if (to == "Archived") await tenants.ArchiveAsync(id, who, reason); else await tenants.ReactivateAsync(id, who, reason);
            TempData["ok"] = loc["msg.saved"];
        }
        catch (DomainException e) { TempData["err"] = loc[e.Message]; }
        return RedirectToAction(nameof(Index));
    }
}
