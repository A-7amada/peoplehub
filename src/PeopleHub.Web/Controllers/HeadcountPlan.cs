using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Organization;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
/// <summary>US-PH3-011 read-only headcount and cost planning view.</summary>
public class HeadcountPlanController(Loc loc, HeadcountPlan svc) : BaseController(loc)
{
 public async Task<IActionResult> Index()
 {
  Response.Headers.CacheControl = "no-store, private";
  try { return View(await svc.BuildAsync()); } catch (DomainException) { return Forbid(); }
 }
}
