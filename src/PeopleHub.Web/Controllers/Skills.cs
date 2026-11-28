using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Organization;
using PeopleHub.Application.Modules.Talent;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
/// <summary>US-PH3-012 skills inventory and gap view. Information only.</summary>
public class SkillsController(Loc loc, SkillsInventory svc, OrgService org, ICurrentUser me) : BaseController(loc)
{
 public async Task<IActionResult> Index()
 {
  Response.Headers.CacheControl = "no-store, private";
  ViewBag.Skills = await svc.SkillsAsync();
  ViewBag.Mine = me.EmployeeId is { } id ? await svc.ForEmployeeAsync(id) : new List<SkillView>();
  ViewBag.Pending = await svc.PendingAsync();
  ViewBag.Depts = await org.DepartmentsAsync();
  ViewBag.Reqs = await svc.RequirementsAsync();
  try { ViewBag.Gaps = await svc.GapsAsync(); } catch (DomainException) { ViewBag.Gaps = null; }
  return View();
 }
 [HttpPost] public Task<IActionResult> AddSkill(string name, string? nameAr, string category) => Do(() => svc.AddSkillAsync(name, nameAr, category), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Retire(int id, bool active) => Do(() => svc.SetActiveAsync(id, active), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Require(int skillId, int departmentId, int minLevel, int minPeople) => Do(() => svc.AddRequirementAsync(skillId, departmentId, minLevel, minPeople), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Unrequire(int id) => Do(() => svc.RemoveRequirementAsync(id), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Record(int? employeeId, int skillId, int level, string source, string? evidence) => Do(() => svc.RecordAsync(employeeId ?? me.EmployeeId ?? 0, skillId, level, source, evidence), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Confirm(int id) => Do(() => svc.ConfirmAsync(id), "msg.saved", () => RedirectToAction(nameof(Index)));
}
