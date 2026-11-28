using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Benefits;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
public class BenefitClaimsController(Loc loc, BenefitClaims svc, BenefitService benefits) : BaseController(loc)
{
 public async Task<IActionResult> Index()
 {
  Response.Headers.CacheControl = "no-store, private";
  ViewBag.Enrollments = (await benefits.GetAsync()).Enrollments;
  return View(await svc.ListAsync());
 }
 [HttpPost] public Task<IActionResult> Record(int enrollmentId, DateTime serviceDate, decimal amount, string description) => Do(() => svc.RecordAsync(enrollmentId, serviceDate, amount, description), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Advance(int id, string? outcome) => Do(() => svc.AdvanceAsync(id, outcome), "msg.saved", () => RedirectToAction(nameof(Index)));
}
