using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;
namespace PeopleHub.Web.Controllers;
public class BenefitsController(Loc loc,BenefitService svc,ICurrentUser me,EmployeeService employees):BaseController(loc)
{
 public async Task<IActionResult> Index(){ViewBag.Employees=me.Can(Perm.BenefitsManage)?await employees.ManagerCandidatesAsync():new List<Employee>();return View(await svc.GetAsync());}
 [HttpPost]public Task<IActionResult> AddPlan(string name,string provider,string eligibility,string coverage,bool dependentsAllowed,decimal employerMonthly,decimal employeeMonthly,string currency)=>Do(()=>svc.AddPlanAsync(name,provider,eligibility,coverage,dependentsAllowed,employerMonthly,employeeMonthly,currency),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost]public Task<IActionResult> Enroll(int planId,int employeeId,int? dependentId,DateTime startsOn,DateTime endsOn,string eligibilityEvidence,string costCenter)=>Do(()=>svc.EnrollAsync(planId,employeeId,dependentId,startsOn,endsOn,eligibilityEvidence,costCenter),"msg.saved",()=>RedirectToAction(nameof(Index)));
 [HttpPost]public Task<IActionResult> Cancel(int enrollmentId,DateTime effectiveOn,string reason)=>Do(()=>svc.CancelAsync(enrollmentId,effectiveOn,reason),"msg.saved",()=>RedirectToAction(nameof(Index)));
}
