using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
public class GrievancesController(Loc loc,GrievanceIntake svc):BaseController(loc)
{
 public async Task<IActionResult> Index(){Response.Headers.CacheControl="no-store, private";try{return View(await svc.GetAsync());}catch(DomainException){return Forbidden();}}
 [HttpPost]public Task<IActionResult> Submit(string title,string statement)=>Do(()=>svc.SubmitAsync(title,statement),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost]public Task<IActionResult> Respond(int id,string note)=>Do(()=>svc.RespondAsync(id,note),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost]public Task<IActionResult> Appeal(int responseId,string note)=>Do(()=>svc.AppealAsync(responseId,note),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost]public Task<IActionResult> DecideAppeal(int appealId,string decision,string note)=>Do(()=>svc.DecideAppealAsync(appealId,decision,note),"msg.saved",()=>RedirectToAction(nameof(Index)));
}
