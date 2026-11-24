using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

public class EmployeesController(Loc loc, EmployeeService svc, OrgService org, LeaveService leave, AttendanceService att, AppClock clock, ICurrentUser me, EmploymentTimeline timeline, AccessScope access) : BaseController(loc)
{
    private async Task Lists()
    {
        ViewBag.Depts = await org.DepartmentsAsync(); ViewBag.Branches = await org.BranchesAsync();
        ViewBag.Titles = await org.TitlesAsync(); ViewBag.Shifts = await org.ShiftsAsync();
        ViewBag.Managers = await svc.ManagerCandidatesAsync();
    }

    public async Task<IActionResult> Index(string? q, int? dept, int? branch, EmployeeStatus? status)
    {
        await Lists();
        ViewBag.Filter = new EmployeeFilter(q, dept, branch, status);
        return View(await svc.SearchAsync(new EmployeeFilter(q, dept, branch, status)));
    }

    public async Task<IActionResult> Profile(int? id, string tab = "overview")
    {
        try
        {
            var eid = id ?? me.EmployeeId ?? throw new DomainException("err.notFound");
            await timeline.ApplyDueAsync();
            var p = await svc.ProfileAsync(eid);
            ViewBag.Scheduled=await timeline.ListAsync(eid,access);
            await Lists();
            ViewBag.Assignments=await svc.AssignmentsAsync(eid);
            ViewBag.Tab = tab; ViewBag.Components = await svc.ComponentsAsync();
            ViewBag.Balances = tab == "leave" ? await leave.BalancesAsync(eid, clock.Today.Year) : [];
            ViewBag.Ledger=tab=="leave" ? await leave.TransactionsAsync(eid,clock.Today.Year) : [];
            ViewBag.Types = await leave.TypesAsync();
            ViewBag.Year = clock.Today.Year;
            ViewBag.Today = clock.Today;
            return View(p);
        }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; return RedirectToAction(nameof(Index)); }
    }

    [RequirePerm(Perm.EmpManage)]
    public async Task<IActionResult> Edit(int? id)
    {
        await Lists();
        var e = id == null ? new Employee { HireDate = clock.Today, Email = "" } : await svc.GetAsync(id.Value);
        if (e != null && !me.Can(Perm.EmpPay)) { e=System.Text.Json.JsonSerializer.Deserialize<Employee>(System.Text.Json.JsonSerializer.Serialize(e))!;e.BasicSalary=0;e.BankName="";e.Iban=""; }
        return e == null ? NotFound() : View(e);
    }

    [RequirePerm(Perm.EmpManage), HttpPost]
    public async Task<IActionResult> Edit(Employee model, string? reason)
    {
        try { await svc.SaveAsync(model, reason); TempData["ok"] = L["msg.saved"]; return RedirectToAction(nameof(Profile), new { id = model.Id == 0 ? (await svc.SearchAsync(new(model.Email, null, null, null))).FirstOrDefault()?.Id : model.Id }); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; if(!me.Can(Perm.EmpPay)){model.BasicSalary=0;model.Iban="";model.BankName="";} await Lists(); return View(model); }
    }

    [RequirePerm(Perm.EmpManage),HttpPost]
    public Task<IActionResult> Schedule(EmploymentChange change)=>Do(()=>timeline.ScheduleAsync(change),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=change.EmployeeId,tab="history"}));
    [RequirePerm(Perm.EmpManage),HttpPost]
    public Task<IActionResult> CancelSchedule(int id,int employeeId,string reason)=>Do(()=>timeline.CancelAsync(id,reason),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="history"}));

    [RequirePerm(Perm.EmpManage), HttpPost]
    public Task<IActionResult> Status(int id, EmployeeStatus to, string? reason) => Do(() => svc.ChangeStatusAsync(id, to, reason), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "history" }));

    [RequirePerm(Perm.EmpManage), HttpPost]
    public Task<IActionResult> AddContract(int id, ContractType type, DateTime start, DateTime? end, string? notes) =>
        Do(() => svc.AddContractAsync(id, type, start, end, notes), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "contracts" }));

    [RequirePerm(Perm.EmpManage), HttpPost]
    public Task<IActionResult> RenewContract(int id, int contractId, DateTime newEnd) =>
        Do(() => svc.RenewContractAsync(contractId, newEnd), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "contracts" }));

    [RequirePerm(Perm.EmpManage), HttpPost]
    public Task<IActionResult> AddEvent(int id, CareerEventType type, string? details) =>
        Do(() => svc.AddCareerEventAsync(id, type, details), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "history" }));

    [HttpPost]
    public Task<IActionResult> SetComponent(int id, int componentId, decimal amount) =>
        Do(() => svc.SetComponentAsync(id, componentId, amount), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "pay" }));

    [HttpPost]
    public Task<IActionResult> MyContact(string? phone, string? address) =>
        Do(() => svc.UpdateMyContactAsync(phone, address), "msg.saved", () => RedirectToAction(nameof(Profile), new { tab = "overview" }));

    [RequirePerm(Perm.EmpManage), HttpPost]
    public Task<IActionResult> AdjustBalance(int id, int typeId, int entitled, string? reason) =>
        Do(() => leave.AdjustBalanceAsync(id, typeId, clock.Today.Year, entitled, reason), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "leave" }));
}

public class OrganizationController(Loc loc, OrgService org, EmployeeService emps) : BaseController(loc)
{
    public async Task<IActionResult> Index() { ViewBag.Managers = await emps.ManagerCandidatesAsync(); return View(await org.GetAsync()); }

    [RequirePerm(Perm.OrgManage), HttpPost]
    public Task<IActionResult> AddDepartment(string name, string? nameAr, int? parentId, int? managerId, string? costCenter, int target) =>
        Do(() => org.AddDepartmentAsync(name, nameAr, parentId, managerId, costCenter, target), "msg.saved", () => RedirectToAction(nameof(Index)));

    [RequirePerm(Perm.OrgManage), HttpPost]
    public Task<IActionResult> ToggleDepartment(int id) => Do(() => org.ToggleDepartmentAsync(id), "msg.saved", () => RedirectToAction(nameof(Index)));

    [RequirePerm(Perm.OrgManage), HttpPost]
    public Task<IActionResult> AddTitle(string name, string? nameAr, int grade, decimal min, decimal max, string? description) =>
        Do(() => org.AddJobTitleAsync(name, nameAr, grade, min, max, description), "msg.saved", () => RedirectToAction(nameof(Index)));
}
