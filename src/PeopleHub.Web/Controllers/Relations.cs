using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
public class RelationsController(Loc loc,EmployeeRelations svc,ICurrentUser me,EmployeeService employees):BaseController(loc)
{
 public async Task<IActionResult> Index(){if(!me.Can(Perm.RelationsManage))return Forbidden();Response.Headers.CacheControl="no-store";ViewBag.Employees=await employees.ManagerCandidatesAsync();return View(await svc.GetAsync());}
 [HttpPost]public Task<IActionResult> Create(int employeeId,string kind,string title,string statement)=>Do(()=>svc.CreateAsync(employeeId,kind,title,statement),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost]public Task<IActionResult> Record(int caseId,string kind,string note)=>Do(()=>svc.RecordAsync(caseId,kind,note),"msg.saved",()=>RedirectToAction(nameof(Index)));
}
