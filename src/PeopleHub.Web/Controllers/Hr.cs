using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Application.Modules.Leave;
using PeopleHub.Application.Modules.Organization;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Services;

namespace PeopleHub.Web.Controllers;

public class EmployeesController(Loc loc, EmployeeService svc, OrgService org, LeaveService leave, AttendanceService att, AppClock clock, ICurrentUser me, EmploymentTimeline timeline, AccessScope access, EmployeeLifecycle lifecycle, EmployeeDocuments documents, ShiftSchedule shifts, AssetService assets) : BaseController(loc)
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
            var p = await svc.ProfileAsync(eid);
            ViewBag.Scheduled=await timeline.ListAsync(eid,access);
            ViewBag.Rehires=me.Can(Perm.EmpManage)?await HttpContext.RequestServices.GetRequiredService<RehireService>().ListAsync(eid,access):new List<RehireRequest>();
            if(tab=="letters"&&(me.Can(Perm.EmpManage)||me.EmployeeId==eid))ViewBag.Letters=await HttpContext.RequestServices.GetRequiredService<LetterService>().ListAsync(eid,access);
            if(tab=="assets"){ViewBag.Assets=await assets.GetAsync(eid);if(me.Can(Perm.EmpManage))ViewBag.Inventory=await assets.InventoryAsync();}
            if(tab=="shifts")ViewBag.ShiftDays=await shifts.ListAsync(eid,access);
            if(tab=="documents") ViewBag.Documents=await documents.ListAsync(eid);
            if(tab=="lifecycle") ViewBag.Lifecycle=await lifecycle.GetAsync(eid);
            await Lists();
            ViewBag.Assignments=await svc.AssignmentsAsync(eid);
            ViewBag.Tab = tab; ViewBag.Components = await svc.ComponentsAsync();
            ViewBag.Balances = tab == "leave" && (me.EmployeeId==eid||me.Can(Perm.LeaveEvidence)) ? await leave.BalancesAsync(eid, clock.Today.Year) : [];
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
        var e = id == null ? new Employee { HireDate = clock.Today, Email = "" } : await svc.EditModelAsync(id.Value);

        return e == null ? NotFound() : View(e);
    }

    [RequirePerm(Perm.EmpManage), HttpPost]
    public async Task<IActionResult> Edit(Employee model, string? reason)
    {
        try { if (!ModelState.IsValid) throw new DomainException("err.required"); await svc.SaveAsync(model, reason); TempData["ok"] = L["msg.saved"]; return RedirectToAction(nameof(Profile), new { id = model.Id == 0 ? (await svc.SearchAsync(new(model.Email, null, null, null))).FirstOrDefault()?.Id : model.Id }); }
        catch (DomainException ex) { TempData["err"] = L[ex.Message]; if(!me.Can(Perm.EmpPay))model.BasicSalary=0;if(!me.Can(Perm.EmpBankEdit)){model.Iban="";model.BankName="";}if(!me.Can(Perm.EmpIdentity)){model.NationalId="";model.BirthDate=null;model.Address="";model.MaritalStatus="";model.InsuranceNumber="";} await Lists(); return View(model); }
    }

    [HttpPost]public Task<IActionResult> RegisterAsset(int employeeId,string tag,string name,string serial,string condition)=>Do(()=>assets.RegisterAsync(tag,name,serial??"",condition),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="assets"}));
    [HttpPost]public Task<IActionResult> MoveAsset(int employeeId,int assetId,string movement,DateTime? dueOn,string condition,string evidence)=>Do(()=>assets.MoveAsync(assetId,movement,employeeId,dueOn,condition,evidence),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="assets"}));
    [HttpPost]public Task<IActionResult> SetShift(int employeeId,DateTime date,int shiftId,string reason)=>Do(()=>shifts.SetAsync(employeeId,date,shiftId,reason),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="shifts"}));
    [HttpPost]public Task<IActionResult> RotateShifts(int employeeId,DateTime from,DateTime to,string pattern,string reason)=>Do(()=>shifts.RotateAsync(employeeId,from,to,pattern,reason),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="shifts"}));
    [HttpPost]public Task<IActionResult> AddShift(int employeeId,string name,string? nameAr,TimeSpan start,TimeSpan end,int grace,int maxLate,bool flexible,TimeSpan? latestStart,int? requiredMinutes)=>Do(()=>shifts.AddShiftAsync(name,nameAr,start,end,grace,maxLate,flexible,latestStart,requiredMinutes),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="shifts"}));
    [HttpPost]public async Task<IActionResult> AddDocument(int employeeId,string title,DateTime? expiresOn,IFormFile? file)
    {var data=await FileBytes(file);return await Do(()=>documents.AddAsync(employeeId,title,expiresOn,file?.FileName??"",data??[]),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="documents"}));}
    [HttpPost]public Task<IActionResult> AddDependent(int employeeId,string name,string relationship,DateTime? birthDate)=>Do(()=>lifecycle.AddDependentAsync(employeeId,name,relationship,birthDate),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="lifecycle"}));
    [HttpPost]public Task<IActionResult> RemoveDependent(int id,int employeeId,string reason)=>Do(()=>lifecycle.RemoveDependentAsync(id,reason),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="lifecycle"}));
    [HttpPost]public Task<IActionResult> AddLifecycleTask(int employeeId,string process,string title,int? assignedEmployeeId,DateTime dueOn)=>Do(()=>lifecycle.AddTaskAsync(employeeId,process,title,assignedEmployeeId,dueOn),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="lifecycle"}));
    [HttpPost]public Task<IActionResult> CompleteLifecycleTask(int id,int employeeId,string note)=>Do(()=>lifecycle.CompleteTaskAsync(id,note),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="lifecycle"}));
    [HttpPost]public Task<IActionResult> SetProbation(int employeeId,DateTime endsOn,string reason)=>Do(()=>lifecycle.SetProbationAsync(employeeId,endsOn,reason),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="lifecycle"}));
    [HttpPost]public Task<IActionResult> DecideProbation(int employeeId,string decision,string reason)=>Do(()=>lifecycle.DecideProbationAsync(employeeId,decision,reason),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="lifecycle"}));
    [HttpPost]public Task<IActionResult> AddField(int employeeId,string label,string? labelAr,string kind)=>Do(()=>lifecycle.AddFieldAsync(label,labelAr,kind),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="lifecycle"}));
    [HttpPost]public Task<IActionResult> SetField(int employeeId,int fieldId,string value)=>Do(()=>lifecycle.SetFieldAsync(employeeId,fieldId,value),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="lifecycle"}));

    [HttpPost]
    public Task<IActionResult> RequestLetter(string? addressedTo, bool includeSalary) => Do(() => HttpContext.RequestServices.GetRequiredService<LetterService>().RequestAsync(addressedTo, includeSalary), "msg.saved", () => RedirectToAction(nameof(Profile), new { id = me.EmployeeId, tab = "letters" }));
    [RequirePerm(Perm.EmpManage), HttpPost]
    public Task<IActionResult> IssueLetter(int letterId, int id) => Do(() => HttpContext.RequestServices.GetRequiredService<LetterService>().IssueAsync(letterId), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "letters" }));
    [RequirePerm(Perm.EmpManage), HttpPost]
    public Task<IActionResult> DeclineLetter(int letterId, int id, string? note) => Do(() => HttpContext.RequestServices.GetRequiredService<LetterService>().DeclineAsync(letterId, note), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "letters" }));
    public async Task<IActionResult> Letter(int id)
    {
        try { var (l, e) = await HttpContext.RequestServices.GetRequiredService<LetterService>().OpenAsync(id, access); ViewBag.Employee = e; return View(l); }
        catch (DomainException) { return NotFound(); }
    }

    [RequirePerm(Perm.EmpManage), HttpPost]
    public Task<IActionResult> RequestRehire(int id, string? reason) => Do(() => HttpContext.RequestServices.GetRequiredService<RehireService>().RequestAsync(id, reason), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "history" }));
    [RequirePerm(Perm.EmpManage), HttpPost, RequireStepUp]
    public Task<IActionResult> DecideRehire(int requestId, int id, bool approve, string? note) => Do(() => HttpContext.RequestServices.GetRequiredService<RehireService>().DecideAsync(requestId, approve, note), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "history" }));

    [RequirePerm(Perm.EmpManage),HttpPost]
    public Task<IActionResult> Schedule(EmploymentChange change)=>Do(()=>timeline.ScheduleAsync(change),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=change.EmployeeId,tab="history"}));
    [RequirePerm(Perm.EmpManage),HttpPost]
    public Task<IActionResult> CancelSchedule(int id,int employeeId,string reason)=>Do(()=>timeline.CancelAsync(id,reason),"msg.saved",()=>RedirectToAction(nameof(Profile),new{id=employeeId,tab="history"}));

    [RequirePerm(Perm.EmpManage), HttpPost, RequireStepUp]
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

    [HttpPost, RequireStepUp]
    public Task<IActionResult> SetComponent(int id, int componentId, decimal amount) =>
        Do(() => svc.SetComponentAsync(id, componentId, amount), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "pay" }));

    [HttpPost]
    public Task<IActionResult> MyContact(string? phone, string? address) =>
        Do(() => svc.UpdateMyContactAsync(phone, address), "msg.saved", () => RedirectToAction(nameof(Profile), new { tab = "overview" }));

    [RequirePerm(Perm.EmpManage), HttpPost, RequireStepUp]
    public Task<IActionResult> AdjustBalance(int id, int typeId, decimal entitled, string? reason) =>
        Do(() => leave.AdjustBalanceAsync(id, typeId, clock.Today.Year, entitled, reason), "msg.saved", () => RedirectToAction(nameof(Profile), new { id, tab = "leave" }));
}

public class OrganizationController(Loc loc, OrgService org, EmployeeService emps,HolidayService holidays) : BaseController(loc)
{
    public async Task<IActionResult> Index() { ViewBag.Managers = await emps.ManagerCandidatesAsync();ViewBag.Holidays=await holidays.ListAsync(); return View(await org.GetAsync()); }

    [RequirePerm(Perm.OrgManage), HttpPost]
    public Task<IActionResult> AddHoliday(DateTime date,string name,string? nameAr,int? branchId,int? departmentId,string reason)=>Do(()=>holidays.AddAsync(date,name,nameAr,branchId,departmentId,reason),"msg.saved",()=>RedirectToAction(nameof(Index)));

    [RequirePerm(Perm.OrgManage), HttpPost]
    public Task<IActionResult> AddDepartment(string name, string? nameAr, int? parentId, int? managerId, string? costCenter, int target) =>
        Do(() => org.AddDepartmentAsync(name, nameAr, parentId, managerId, costCenter, target), "msg.saved", () => RedirectToAction(nameof(Index)));

    [RequirePerm(Perm.OrgManage), HttpPost]
    public Task<IActionResult> ToggleDepartment(int id) => Do(() => org.ToggleDepartmentAsync(id), "msg.saved", () => RedirectToAction(nameof(Index)));

    [RequirePerm(Perm.OrgManage), HttpPost]
    public Task<IActionResult> AddTitle(string name, string? nameAr, int grade, decimal min, decimal max, string? description) =>
        Do(() => org.AddJobTitleAsync(name, nameAr, grade, min, max, description), "msg.saved", () => RedirectToAction(nameof(Index)));
}
