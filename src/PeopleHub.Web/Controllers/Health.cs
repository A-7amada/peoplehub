using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
[AllowAnonymous]
public class HealthController(AppDbContext db):Controller
{
 [HttpGet("/health/live")]public IActionResult Live()=>Content("alive","text/plain");
 [HttpGet("/health/ready")]public async Task<IActionResult> Ready(){using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(3));try{return await db.Database.CanConnectAsync(timeout.Token)?Content("ready","text/plain"):StatusCode(503,"not ready");}catch{return StatusCode(503,"not ready");}}
}
public class DiagnosticsController(Loc loc,RequestMetrics metrics,OperationsProjection ops):BaseController(loc)
{
 [RequirePerm(Perm.AdmSettings)]public async Task<IActionResult> Index(){ViewBag.Ops=await ops.SnapshotAsync();return View(metrics.Snapshot());}
}
