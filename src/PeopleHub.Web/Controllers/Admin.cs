using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Application.Modules.Requests;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

public class AdminController(Loc loc, AdminService admin, WorkflowService flows, EmployeeService emps, SettingsService settings, DeviceService device, EmailService mail, OperationsService ops, PeopleHub.Application.Modules.Leave.LeavePolicyService leavePolicies) : BaseController(loc)
{
    // ---- system settings (attendance source, device, email, policies)
    [RequirePerm(Perm.AdmSettings)]
    public async Task<IActionResult> Settings()
    {
        ViewBag.Punches = await device.RecentAsync(); ViewBag.Outbox = await mail.OutboxAsync(); ViewBag.MailSummary = await mail.DeliverySummaryAsync();
        return View(await settings.AllAsync());
    }

    [RequirePerm(Perm.AdmSettings), HttpPost]
    [RequireStepUp]
    public Task<IActionResult> SaveSettings(IFormCollection f) =>
        Do(() => settings.SaveAsync(SettingsService.Defaults.Keys.Where(k => f.ContainsKey(k) || k == "email.notify" || k == "zk.simulate").ToDictionary(k => k, k => k == "email.notify" ? (f.ContainsKey(k) && f[k].Contains("true") ? "true" : "false") : k == "zk.simulate" ? (f.ContainsKey(k) && f[k].Contains("true") ? "true" : "false") : f[k].ToString().Trim())), "msg.saved", () => RedirectToAction(nameof(Settings)));

    [RequirePerm(Perm.AdmSettings), HttpPost,RequireStepUp]
    public async Task<IActionResult> TestDevice() { await settings.LoadAsync(); var (ok, msg) = await device.PingAsync(); if (ok) TempData["ok"] = msg; else TempData["err"] = msg; return RedirectToAction(nameof(Settings)); }

    [RequirePerm(Perm.AdmSettings), HttpPost,RequireStepUp]
    public async Task<IActionResult> SyncDevice() { try { var n = await device.SyncAsync(); TempData["ok"] = (L.IsAr ? "تمت المزامنة: " : "Synced: ") + n + (L.IsAr ? " بصمة جديدة" : " new punches"); } catch (DomainException ex) { TempData["err"] = L[ex.Message]; } return RedirectToAction(nameof(Settings)); }

    [RequirePerm(Perm.AdmSettings), HttpPost,RequireStepUp]
    public async Task<IActionResult> FlushMail() { await mail.FlushAsync(); TempData["ok"] = L["msg.saved"]; return RedirectToAction(nameof(Settings)); }

    [RequirePerm(Perm.AdmSettings), HttpPost, RequireStepUp]
    public async Task<IActionResult> RequeueMail() { await mail.RequeueFailedAsync(); TempData["ok"] = L["msg.saved"]; return RedirectToAction(nameof(Settings)); }

    // ---- leave policies (ADR-0022): editable defaults, preview first
    [RequirePerm(Perm.AdmSettings)]
    public async Task<IActionResult> LeavePolicies(int? typeId, string? code)
    {
        ViewBag.Today = DateTime.Today;
        if (typeId != null && !string.IsNullOrWhiteSpace(code))
            try { ViewBag.Preview = await leavePolicies.PreviewAsync(typeId.Value, code, null); ViewBag.PreviewType = typeId; } catch (DomainException ex) { TempData["err"] = L[ex.Message]; }
        return View(await leavePolicies.ListAsync());
    }

    [RequirePerm(Perm.AdmSettings), HttpPost, RequireStepUp]
    public Task<IActionResult> SaveLeavePolicy(int typeId, bool enabled, int waitingMonths, string accrual, bool prorate, int expiryMonths, bool halfDay, bool hourly, int hoursPerDay, string? tiers)
        => Do(() => leavePolicies.SaveAsync(typeId, enabled, waitingMonths, accrual, prorate, expiryMonths, halfDay, hourly, hoursPerDay, tiers), "msg.saved", () => RedirectToAction(nameof(LeavePolicies)));

    [RequirePerm(Perm.AdmSettings)]
    public IActionResult Operations() => View(ops);

    [RequirePerm(Perm.AdmSettings), HttpPost]
    [RequireStepUp]
    public async Task<IActionResult> ExportDatabase()
    {
        Response.Headers.CacheControl = "no-store";
        return File(await ops.ExportAsync(), "application/octet-stream", $"peoplehub-{DateTime.UtcNow:yyyyMMdd-HHmm}.{(ops.Provider == "SQLite" ? "db" : "zip")}");
    }

    // ---- roles and page permissions
    [RequirePerm(Perm.AdmRoles)]
    public async Task<IActionResult> Roles()
    {
        ViewBag.Perms = await admin.PermissionsAsync();
        return View(await admin.RolesAsync());
    }

    [RequirePerm(Perm.AdmRoles), HttpPost]
    [RequireStepUp]
    public Task<IActionResult> SavePermissions(string[]? grant) => Do(() => admin.SavePermissionsAsync(grant ?? []), "msg.saved", () => RedirectToAction(nameof(Roles)));

    [RequirePerm(Perm.AdmRoles), HttpPost]
    [RequireStepUp]
    public Task<IActionResult> AddRole(string code, string name, string? nameAr) => Do(() => admin.AddRoleAsync(code, name, nameAr), "msg.saved", () => RedirectToAction(nameof(Roles)));

    [RequirePerm(Perm.AdmRoles), HttpPost]
    [RequireStepUp]
    public Task<IActionResult> DeleteRole(string code) => Do(() => admin.DeleteRoleAsync(code), "msg.saved", () => RedirectToAction(nameof(Roles)));

    // ---- users
    [RequirePerm(Perm.AdmUsers)]
    public async Task<IActionResult> Users()
    {
        ViewBag.Roles = await admin.RolesAsync();
        ViewBag.Employees = await emps.ManagerCandidatesAsync();
        return View(await admin.UsersAsync());
    }

    [RequirePerm(Perm.AdmUsers), HttpPost]
    [RequireStepUp]
    public Task<IActionResult> SaveUser(int id, string[]? roles, bool active) => Do(() => admin.SetUserRolesAsync(id, roles, active), "msg.saved", () => RedirectToAction(nameof(Users)));

    [RequirePerm(Perm.AdmUsers), HttpPost]
    [RequireStepUp]
    public Task<IActionResult> CreateUser(string email, string password, string[]? roles, int? employeeId) =>
        Do(() => admin.CreateUserAsync(email, password, roles, employeeId), "msg.saved", () => RedirectToAction(nameof(Users)));

    // ---- approval cycles
    [RequirePerm(Perm.AdmFlows)]
    public async Task<IActionResult> Workflows()
    {
        ViewBag.Roles = await admin.RolesAsync();
        ViewBag.RequiredComments=await flows.RequiredApprovalCommentsAsync();ViewBag.ReminderHours=await flows.ReminderHoursAsync();
        ViewBag.Revisions=await flows.RevisionsAsync();return View(await flows.ListAsync());
    }
    [RequirePerm(Perm.AdmFlows)]
    public async Task<IActionResult> WorkflowCompare(string left,string right)
    {
        try{var r=await flows.CompareAsync(left??"",right??"");ViewBag.Left=r.Left;ViewBag.Right=r.Right;return View(r.Changes);}catch(DomainException){return Forbidden();}
    }
    [RequirePerm(Perm.AdmFlows)]
    public async Task<IActionResult> WorkflowSimulate(string? kind,string? code,decimal size=0,bool escalate=false)
    {
        WorkflowService.Simulation? sim=null;string? error=null;
        if(!string.IsNullOrWhiteSpace(code)){try{sim=await flows.SimulateAsync(kind??"Leave",code,size,escalate);}catch(DomainException ex){error=loc[ex.Message];}}
        ViewBag.Error=error;ViewBag.Kind=kind??"Leave";ViewBag.Code=code;ViewBag.Size=size;ViewBag.Escalate=escalate;return View(sim);
    }

    [RequirePerm(Perm.AdmFlows),HttpPost,RequireStepUp]public Task<IActionResult> ReminderPolicy(string kind,int hours)=>Do(()=>flows.SetReminderHoursAsync(kind,hours),"msg.saved",()=>RedirectToAction(nameof(Workflows)));
    [RequirePerm(Perm.AdmFlows), HttpPost,RequireStepUp]
    public Task<IActionResult> ApprovalComment(string kind,bool required) => Do(()=>flows.SetApprovalCommentAsync(kind,required),"msg.saved",()=>RedirectToAction(nameof(Workflows)));

    [RequirePerm(Perm.AdmFlows), HttpPost,RequireStepUp]
    public Task<IActionResult> AddStep(string kind, ApproverType type, string[]? roles, decimal? threshold,ApprovalRule rule=ApprovalRule.AnyRole,int? requiredCount=null) =>
        Do(() => {if(!ModelState.IsValid)throw new DomainException("err.required");return flows.AddStepAsync(kind, type, roles, threshold,rule,requiredCount);}, "msg.saved", () => RedirectToAction(nameof(Workflows)));

    [RequirePerm(Perm.AdmFlows), HttpPost,RequireStepUp]
    public Task<IActionResult> RemoveStep(int id) => Do(() => flows.RemoveStepAsync(id), "msg.saved", () => RedirectToAction(nameof(Workflows)));

    [RequirePerm(Perm.AdmFlows), HttpPost,RequireStepUp]
    public Task<IActionResult> MoveStep(int id, int dir) => Do(() => flows.MoveStepAsync(id, dir), "msg.saved", () => RedirectToAction(nameof(Workflows)));
}
