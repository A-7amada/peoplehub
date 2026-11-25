using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
public class RelationsController(Loc loc,EmployeeRelations svc,ICurrentUser me,EmployeeService employees):BaseController(loc)
{
 public async Task<IActionResult> Index(){if(!await svc.CanEnterAsync())return Forbidden();Response.Headers.CacheControl="no-store";ViewBag.Employees=me.Can(Perm.RelationsManage)?await employees.ManagerCandidatesAsync():new List<Employee>();ViewBag.Investigators=me.Can(Perm.RelationsManage)?await svc.CandidatesAsync():new List<CaseInvestigator>();return View(await svc.GetAsync());}
 [RequireStepUp,HttpPost]public Task<IActionResult> Access(int caseId,int userId,bool granted,string reason)=>Do(()=>svc.SetAccessAsync(caseId,userId,granted,reason),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost]public Task<IActionResult> Create(int employeeId,string kind,string title,string statement)=>Do(()=>svc.CreateAsync(employeeId,kind,title,statement),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost]public Task<IActionResult> Record(int caseId,string kind,string note)=>Do(()=>svc.RecordAsync(caseId,kind,note),"msg.saved",()=>RedirectToAction(nameof(Index)));
}
