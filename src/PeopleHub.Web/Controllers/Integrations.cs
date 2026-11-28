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
