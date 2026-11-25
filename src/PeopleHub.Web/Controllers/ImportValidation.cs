using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
[RequirePerm(Perm.EmpManage)]
public class ImportValidationController(Loc loc,EmployeeImportValidation service):BaseController(loc)
{
 public IActionResult Index(){Response.Headers.CacheControl="no-store, private";return View();}
 [HttpPost,RequestSizeLimit(300000),RequestFormLimits(MemoryBufferThreshold=300000,MultipartBodyLengthLimit=300000)]public async Task<IActionResult> Preview(IFormFile? file)
 {
  Response.Headers.CacheControl="no-store, private";
  if(file==null||file.Length==0||file.Length>EmployeeImportValidation.MaxBytes||!ModelState.IsValid){TempData["err"]=L["err.required"];return RedirectToAction(nameof(Index));}
  using var memory=new MemoryStream();await file.CopyToAsync(memory);
  try{return View("Index",await service.PreviewAsync(memory.ToArray()));}catch(DomainException ex){TempData["err"]=L[ex.Message];return RedirectToAction(nameof(Index));}
 }
}
