using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Leave;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
/// <summary>US-PH2-004 leave encashment approval record. Days only, no payout amount.</summary>
public class LeaveEncashmentController(Loc loc, LeaveEncashments svc, IRepository repo) : BaseController(loc)
{
 public async Task<IActionResult> Index()
 {
  Response.Headers.CacheControl = "no-store, private";
  try
  {
   var rows = await svc.ListAsync(); var types = await repo.ToListAsync(repo.Query<LeaveType>().OrderBy(x => x.Id));
   var max = new Dictionary<int, decimal>(); foreach (var t in types) max[t.Id] = await svc.MaxDaysAsync(t.Code);
   ViewBag.Types = types; ViewBag.Max = max; ViewBag.Year = DateTime.UtcNow.Year;
   return View(rows);
  }
  catch (DomainException) { return Forbid(); }
 }
 [HttpPost] public Task<IActionResult> Max(int leaveTypeId, decimal days) => Do(() => svc.SetMaxDaysAsync(leaveTypeId, days), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Request(int employeeId, int leaveTypeId, int year, decimal days) => Do(() => svc.RequestAsync(employeeId, leaveTypeId, year, days), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Decide(int id, bool approve, string note) => Do(() => svc.DecideAsync(id, approve, note), "msg.saved", () => RedirectToAction(nameof(Index)));
 [HttpPost] public Task<IActionResult> Cancel(int id) => Do(() => svc.CancelAsync(id), "msg.saved", () => RedirectToAction(nameof(Index)));
}
