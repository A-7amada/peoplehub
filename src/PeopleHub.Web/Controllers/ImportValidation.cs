using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
[RequirePerm(Perm.EmpManage)]
public class ImportValidationController(Loc loc,EmployeeImportValidation service,EmployeeImportBatches batches):BaseController(loc)
{
 public IActionResult Index(){Response.Headers.CacheControl="no-store, private";return View();}
 [HttpPost,RequestSizeLimit(300000),RequestFormLimits(MemoryBufferThreshold=300000,MultipartBodyLengthLimit=300000)]public async Task<IActionResult> Preview(IFormFile? file)
 {
  Response.Headers.CacheControl="no-store, private";
  if(file==null||file.Length==0||file.Length>EmployeeImportValidation.MaxBytes||!ModelState.IsValid){TempData["err"]=L["err.required"];return RedirectToAction(nameof(Index));}
  using var memory=new MemoryStream();await file.CopyToAsync(memory);
  try{return View("Index",await service.PreviewAsync(memory.ToArray()));}catch(DomainException ex){TempData["err"]=L[ex.Message];return RedirectToAction(nameof(Index));}
 }
 [HttpPost,RequestSizeLimit(300000),RequestFormLimits(MemoryBufferThreshold=300000,MultipartBodyLengthLimit=300000)]public async Task<IActionResult> Stage(IFormFile? file)
 {
  Response.Headers.CacheControl="no-store, private";
  if(file==null||file.Length==0||file.Length>EmployeeImportValidation.MaxBytes){TempData["err"]=L["err.required"];return RedirectToAction(nameof(Index));}
  using var memory=new MemoryStream();await file.CopyToAsync(memory);
  try{var id=await batches.StageAsync(memory.ToArray());return RedirectToAction(nameof(Batch),new{id});}catch(DomainException ex){TempData["err"]=L[ex.Message];return RedirectToAction(nameof(Index));}
 }
 public async Task<IActionResult> Batch(int id)
 {
  Response.Headers.CacheControl="no-store, private";
  var b=await batches.GetAsync(id);if(b==null)return NotFound();
  ViewBag.Recon=b.Status!="Staged"?await batches.ReconcileAsync(id):null;
  return View(b);
 }
 [HttpPost,RequireStepUp]public async Task<IActionResult> Commit(int id)
 {
  try{await batches.CommitAsync(id);TempData["ok"]=L["msg.saved"];}catch(DomainException ex){TempData["err"]=L[ex.Message];}
  return RedirectToAction(nameof(Batch),new{id});
 }
 [HttpPost,RequireStepUp]public async Task<IActionResult> Rollback(int id,string? reason)
 {
  try{await batches.RollbackAsync(id,reason);TempData["ok"]=L["msg.saved"];}catch(DomainException ex){TempData["err"]=L[ex.Message];}
  return RedirectToAction(nameof(Batch),new{id});
 }
 [HttpPost]public async Task<IActionResult> Discard(int id)
 {
  try{await batches.DiscardAsync(id);TempData["ok"]=L["msg.saved"];return RedirectToAction(nameof(Index));}catch(DomainException ex){TempData["err"]=L[ex.Message];return RedirectToAction(nameof(Batch),new{id});}
 }
}
