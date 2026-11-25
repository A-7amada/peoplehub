using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Application.Modules.Relations;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
public class RelationsController(Loc loc,EmployeeRelations svc,ICurrentUser me,EmployeeService employees):BaseController(loc)
{
 public async Task<IActionResult> Index(){if(!await svc.CanEnterAsync())return Forbidden();Response.Headers.CacheControl="no-store";ViewBag.Employees=me.Can(Perm.RelationsManage)?await employees.ManagerCandidatesAsync():new List<Employee>();ViewBag.Investigators=me.Can(Perm.RelationsManage)?await svc.CandidatesAsync():new List<CaseInvestigator>();return View(await svc.GetAsync());}
 [HttpPost]public async Task<IActionResult> Evidence(int caseId,string sourceReference,IFormFile? file){var data=await FileBytes(file);return await Do(()=>svc.AddFileAsync(caseId,sourceReference,file?.FileName??"",data??[]),"msg.saved",()=>RedirectToAction(nameof(Index)));}
 [HttpGet]public async Task<IActionResult> File(string id)
 {
  Response.Headers.CacheControl="no-store, private";Response.Headers.Pragma="no-cache";Response.Headers.XContentTypeOptions="nosniff";
  try{var f=await svc.DownloadAsync(id);return File(f.Data,f.ContentType,f.FileName);}catch(DomainException){return NotFound();}
 }
 [RequireStepUp,HttpPost]public Task<IActionResult> Access(int caseId,int userId,bool granted,string reason)=>Do(()=>svc.SetAccessAsync(caseId,userId,granted,reason),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost]public Task<IActionResult> Create(int employeeId,string kind,string title,string statement)=>Do(()=>svc.CreateAsync(employeeId,kind,title,statement),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost]public Task<IActionResult> Record(int caseId,string kind,string note)=>Do(()=>svc.RecordAsync(caseId,kind,note),"msg.saved",()=>RedirectToAction(nameof(Index)));
}
