using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

public class AdminController(Loc loc, AdminService admin, WorkflowService flows, EmployeeService emps, SettingsService settings, DeviceService device, EmailService mail) : BaseController(loc)
{
    // ---- system settings (attendance source, device, email, policies)
    [RequirePerm(Perm.AdmSettings)]
    public async Task<IActionResult> Settings()
    {
        ViewBag.Punches = await device.RecentAsync(); ViewBag.Outbox = await mail.OutboxAsync();
        return View(await settings.AllAsync());
    }

    [RequirePerm(Perm.AdmSettings), HttpPost]
    public Task<IActionResult> SaveSettings(IFormCollection f) =>
        Do(() => settings.SaveAsync(SettingsService.Defaults.Keys.Where(k => f.ContainsKey(k) || k == "email.notify").ToDictionary(k => k, k => k == "email.notify" ? (f.ContainsKey(k) && f[k].Contains("true") ? "true" : "false") : k == "zk.simulate" ? (f.ContainsKey(k) && f[k].Contains("true") ? "true" : "false") : f[k].ToString().Trim())), "msg.saved", () => RedirectToAction(nameof(Settings)));

    [RequirePerm(Perm.AdmSettings), HttpPost]
    public async Task<IActionResult> TestDevice() { await settings.LoadAsync(); var (ok, msg) = await device.PingAsync(); if (ok) TempData["ok"] = msg; else TempData["err"] = msg; return RedirectToAction(nameof(Settings)); }

    [RequirePerm(Perm.AdmSettings), HttpPost]
    public async Task<IActionResult> SyncDevice() { try { var n = await device.SyncAsync(); TempData["ok"] = (L.IsAr ? "تمت المزامنة: " : "Synced: ") + n + (L.IsAr ? " بصمة جديدة" : " new punches"); } catch (DomainException ex) { TempData["err"] = L[ex.Message]; } return RedirectToAction(nameof(Settings)); }

    [RequirePerm(Perm.AdmSettings), HttpPost]
    public async Task<IActionResult> FlushMail() { await mail.FlushAsync(); TempData["ok"] = L["msg.saved"]; return RedirectToAction(nameof(Settings)); }

    // ---- roles and page permissions
    [RequirePerm(Perm.AdmRoles)]
    public async Task<IActionResult> Roles()
    {
        ViewBag.Perms = await admin.PermissionsAsync();
        return View(await admin.RolesAsync());
    }

    [RequirePerm(Perm.AdmRoles), HttpPost]
    public Task<IActionResult> SavePermissions(string[]? grant) => Do(() => admin.SavePermissionsAsync(grant ?? []), "msg.saved", () => RedirectToAction(nameof(Roles)));

    [RequirePerm(Perm.AdmRoles), HttpPost]
    public Task<IActionResult> AddRole(string code, string name, string? nameAr) => Do(() => admin.AddRoleAsync(code, name, nameAr), "msg.saved", () => RedirectToAction(nameof(Roles)));

    [RequirePerm(Perm.AdmRoles), HttpPost]
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
    public Task<IActionResult> SaveUser(int id, string[]? roles, bool active) => Do(() => admin.SetUserRolesAsync(id, roles, active), "msg.saved", () => RedirectToAction(nameof(Users)));

    [RequirePerm(Perm.AdmUsers), HttpPost]
    public Task<IActionResult> CreateUser(string email, string password, string[]? roles, int? employeeId) =>
        Do(() => admin.CreateUserAsync(email, password, roles, employeeId), "msg.saved", () => RedirectToAction(nameof(Users)));

    // ---- approval cycles
    [RequirePerm(Perm.AdmFlows)]
    public async Task<IActionResult> Workflows()
    {
        ViewBag.Roles = await admin.RolesAsync();
        return View(await flows.ListAsync());
    }

    [RequirePerm(Perm.AdmFlows), HttpPost]
    public Task<IActionResult> AddStep(string kind, ApproverType type, string[]? roles, decimal? threshold) =>
        Do(() => flows.AddStepAsync(kind, type, roles, threshold), "msg.saved", () => RedirectToAction(nameof(Workflows)));

    [RequirePerm(Perm.AdmFlows), HttpPost]
    public Task<IActionResult> RemoveStep(int id) => Do(() => flows.RemoveStepAsync(id), "msg.saved", () => RedirectToAction(nameof(Workflows)));

    [RequirePerm(Perm.AdmFlows), HttpPost]
    public Task<IActionResult> MoveStep(int id, int dir) => Do(() => flows.MoveStepAsync(id, dir), "msg.saved", () => RedirectToAction(nameof(Workflows)));
}
