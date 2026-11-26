using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Modules.Operations;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
[AllowAnonymous]
public class HealthController(AppDbContext db):Controller
{
 [HttpGet("/health/live")]public IActionResult Live()=>Content("alive","text/plain");
 [HttpGet("/health/ready")]public async Task<IActionResult> Ready(){using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(3));try{return await db.Database.CanConnectAsync(timeout.Token)?Content("ready","text/plain"):StatusCode(503,"not ready");}catch{return StatusCode(503,"not ready");}}
}
public class DiagnosticsController(Loc loc,RequestMetrics metrics,OperationsProjection ops,ScheduledJobs jobs):BaseController(loc)
{
 [RequirePerm(Perm.AdmSettings)]public async Task<IActionResult> Index(){ViewBag.Ops=await ops.SnapshotAsync();ViewBag.Jobs=await jobs.ListAsync();ViewBag.Files=await ops.StoredFilesAsync();return View(metrics.Snapshot());}
}

/// <summary>OpenTelemetry-compatible metrics scrape endpoint. Off unless METRICS_TOKEN is configured (then 404); with a token it needs "Authorization: Bearer &lt;token&gt;".</summary>
[AllowAnonymous]
public class MetricsController(RequestMetrics metrics,IConfiguration config):Controller
{
 [HttpGet("/metrics")]public IActionResult Scrape()
 {
  var token=config["METRICS_TOKEN"];
  if(string.IsNullOrEmpty(token))return NotFound();
  var header=Request.Headers.Authorization.ToString();
  var given=header.StartsWith("Bearer ",StringComparison.Ordinal)?header[7..]:"";
  var ok=System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(given),System.Text.Encoding.UTF8.GetBytes(token));
  if(!ok)return Unauthorized();
  return Content(OpenMetricsFormatter.Render(metrics.Snapshot()),"application/openmetrics-text; version=1.0.0; charset=utf-8");
 }
}
