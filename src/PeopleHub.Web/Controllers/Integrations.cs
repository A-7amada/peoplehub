using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Common;
using PeopleHub.Domain.Entities;
using PeopleHub.Application.Modules.Operations;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
/// <summary>US-PH3-001 integration catalog and administration. Metadata only, no secrets.</summary>
public class IntegrationsController(Loc loc, IntegrationRegistry svc) : BaseController(loc)
{
 public async Task<IActionResult> Index()
 {
  Response.Headers.CacheControl = "no-store, private";
  try { ViewBag.Rows = await svc.ListAsync(); } catch (DomainException) { return Forbid(); }
  return View();
 }
 [HttpPost] public Task<IActionResult> Register(string name, string purpose, string owner, string version, string authMethod, string? credentialRef) => Do(() => svc.RegisterAsync(name, purpose, owner, version, authMethod, credentialRef), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Status(int id, bool enabled) => Do(() => svc.SetStatusAsync(id, enabled), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Rotated(int id, string credentialRef) => Do(() => svc.RecordRotationAsync(id, credentialRef), "msg.saved", () => RedirectToAction(nameof(Index)));
}

/// <summary>US-PH1-005 retention schedule and legal hold. Review list only: nothing is deleted.</summary>
public class RetentionController(Loc loc, RetentionReview svc) : BaseController(loc)
{
 public async Task<IActionResult> Index()
 {
  Response.Headers.CacheControl = "no-store, private";
  try { ViewBag.Rules = await svc.RulesAsync(); ViewBag.Holds = await svc.HoldsAsync(); ViewBag.Review = await svc.ReviewListAsync(); } catch (DomainException) { return Forbid(); }
  return View();
 }
 [HttpPost] public Task<IActionResult> Rule(string category, int months) => Do(() => svc.SetRuleAsync(category, months), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Hold(int employeeId, string reason) => Do(() => svc.PlaceHoldAsync(employeeId, reason), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Release(int id, string reason) => Do(() => svc.ReleaseHoldAsync(id, reason), "msg.saved", () => RedirectToAction(nameof(Index)));
}
