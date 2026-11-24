using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

namespace PeopleHub.Application.Services;

public record EmployeeFilter(string? Search, int? DepartmentId, int? BranchId, EmployeeStatus? Status);
public record EmployeeProfile(Employee Employee, string? ManagerName, string? ManagerNameAr, List<Contract> Contracts,
    List<StatusHistory> StatusHistory, List<CareerEvent> Events, List<EmployeeSalaryComponent> Components, bool CanSeeSalary, Shift? Shift);

public class EmployeeService(IRepository repo, AccessScope scope, ICurrentUser me, AuditService audit, AppClock clock)
{
    public async Task<List<Employee>> SearchAsync(EmployeeFilter f)
    {
        var ids = await scope.VisibleEmployeeIdsAsync();
        var q = repo.Query<Employee>().Where(e => true);
        if (ids != null) q = q.Where(e => ids.Contains(e.Id));
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToLower();
            q = q.Where(e => e.FullName.ToLower().Contains(s) || e.FullNameAr.Contains(s) || e.Email.Contains(s) || e.Code.ToLower().Contains(s));
        }
        if (f.DepartmentId is { } d) q = q.Where(e => e.DepartmentId == d);
        if (f.BranchId is { } b) q = q.Where(e => e.BranchId == b);
        if (f.Status is { } st) q = q.Where(e => e.Status == st);
        return await repo.ToListAsync(q.OrderBy(e => e.Code));
    }

    public async Task<Dictionary<int, Employee>> LookupAsync() =>
        (await repo.ToListAsync(repo.Query<Employee>())).ToDictionary(e => e.Id);

    public async Task<List<Employee>> ManagerCandidatesAsync() =>
        await repo.ToListAsync(repo.Query<Employee>().Where(e => e.Status == EmployeeStatus.Active).OrderBy(e => e.FullName));

    public Task<Employee?> GetAsync(int id) => repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e => e.Id == id));

    public async Task<EmployeeProfile> ProfileAsync(int id)
    {
        if (!await scope.CanSeeAsync(id)) throw new DomainException("err.forbidden");
        var e = await GetAsync(id) ?? throw new DomainException("err.notFound");
        var mgr = e.ManagerId is { } m ? await GetAsync(m) : null;
        return new EmployeeProfile(e, mgr?.FullName, mgr?.FullNameAr,
            await repo.ToListAsync(repo.Query<Contract>().Where(c => c.EmployeeId == id).OrderByDescending(c => c.StartDate)),
            await repo.ToListAsync(repo.Query<StatusHistory>().Where(c => c.EmployeeId == id).OrderByDescending(c => c.At)),
            await repo.ToListAsync(repo.Query<CareerEvent>().Where(c => c.EmployeeId == id).OrderByDescending(c => c.Date)),
            scope.CanSeeSalary(id) ? await repo.ToListAsync(repo.Query<EmployeeSalaryComponent>().Where(c => c.EmployeeId == id)) : [],
            scope.CanSeeSalary(id),
            e.ShiftId is { } s ? await repo.FirstOrDefaultAsync(repo.Query<Shift>().Where(x => x.Id == s)) : null);
    }

    private void RequireHr()
    {
        if (!me.Can(Perm.EmpManage)) throw new DomainException("err.forbidden");
    }

    public async Task SaveAsync(Employee model, string? reason)
    {
        RequireHr();
        model.Validate();
        if (await repo.AnyAsync(repo.Query<Employee>().Where(x => x.Email == model.Email && x.Id != model.Id))) throw new DomainException("err.emailTaken");
        var today = clock.Today;

        if (model.Id == 0)
        {
            var n = await repo.CountAsync(repo.Query<Employee>()) + 1;
            model.Code = $"EMP-{n:0000}";
            while (await repo.AnyAsync(repo.Query<Employee>().Where(x => x.Code == model.Code))) model.Code = $"EMP-{++n:0000}";
            model.Status = EmployeeStatus.Active;
            repo.Add(model);
            audit.Log("Create", "Employee", model.Code, $"{model.FullName}, salary {model.BasicSalary}");
            await repo.SaveAsync();
            repo.Add(new StatusHistory { EmployeeId = model.Id, From = EmployeeStatus.Active, To = EmployeeStatus.Active, At = clock.UtcNow, ChangedBy = me.Email, Reason = "Hired" });
            await repo.SaveAsync();
            return;
        }

        var e = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x => x.Id == model.Id)) ?? throw new DomainException("err.notFound");
        void Event(CareerEventType t, string details) =>
            repo.Add(new CareerEvent { EmployeeId = e.Id, Type = t, Date = today, Details = details, RecordedBy = me.Email });
        var changes = new List<string>();
        if (e.BasicSalary != model.BasicSalary) { Event(CareerEventType.SalaryChange, $"{e.BasicSalary:N0} -> {model.BasicSalary:N0}"); changes.Add($"salary {e.BasicSalary} -> {model.BasicSalary}"); }
        if (e.JobTitleId != model.JobTitleId) { Event(CareerEventType.TitleChange, $"title #{e.JobTitleId} -> #{model.JobTitleId}"); changes.Add($"jobTitle {e.JobTitleId} -> {model.JobTitleId}"); }
        if (e.DepartmentId != model.DepartmentId) { Event(CareerEventType.Transfer, $"department #{e.DepartmentId} -> #{model.DepartmentId}"); changes.Add($"department {e.DepartmentId} -> {model.DepartmentId}"); }
        if (e.ManagerId != model.ManagerId) { Event(CareerEventType.ManagerChange, $"manager #{e.ManagerId} -> #{model.ManagerId}"); changes.Add($"manager {e.ManagerId} -> {model.ManagerId}"); }
        if (model.ManagerId == e.Id) throw new DomainException("err.selfManager");
        if (e.BranchId != model.BranchId) changes.Add($"branch {e.BranchId} -> {model.BranchId}");

        e.FullName = model.FullName; e.FullNameAr = model.FullNameAr ?? ""; e.NationalId = model.NationalId ?? ""; e.BirthDate = model.BirthDate;
        e.Gender = model.Gender; e.MaritalStatus = model.MaritalStatus ?? ""; e.Nationality = model.Nationality ?? ""; e.Address = model.Address ?? "";
        e.Phone = model.Phone ?? ""; e.Email = model.Email; e.HireDate = model.HireDate; e.JobTitleId = model.JobTitleId; e.DepartmentId = model.DepartmentId;
        e.BranchId = model.BranchId; e.ManagerId = model.ManagerId; e.EmploymentType = model.EmploymentType; e.ShiftId = model.ShiftId;
        e.BankName = model.BankName ?? ""; e.Iban = model.Iban ?? ""; e.InsuranceNumber = model.InsuranceNumber ?? ""; e.BasicSalary = model.BasicSalary;
        audit.Log("Update", "Employee", e.Code, changes.Count == 0 ? "profile fields" : string.Join("; ", changes) + (string.IsNullOrWhiteSpace(reason) ? "" : $" (reason: {reason})"));
        await repo.SaveAsync();
    }

    /// <summary>Employees may update only their own contact details.</summary>
    public async Task UpdateMyContactAsync(string? phone, string? address)
    {
        var e = await GetAsync(me.EmployeeId ?? throw new DomainException("err.forbidden")) ?? throw new DomainException("err.notFound");
        audit.Log("Update", "Employee", e.Code, $"contact: phone '{e.Phone}' -> '{phone}'");
        e.Phone = phone?.Trim() ?? ""; e.Address = address?.Trim() ?? "";
        await repo.SaveAsync();
    }

    public async Task ChangeStatusAsync(int id, EmployeeStatus to, string? reason)
    {
        RequireHr();
        if (to is EmployeeStatus.Terminated or EmployeeStatus.Resigned or EmployeeStatus.Suspended && string.IsNullOrWhiteSpace(reason)) throw new DomainException("err.reasonRequired");
        var e = await GetAsync(id) ?? throw new DomainException("err.notFound");
        var from = e.Status;
        repo.Add(e.ChangeStatus(to, me.Email, reason, clock.UtcNow));
        audit.Log("StatusChange", "Employee", e.Code, $"{from} -> {to}; {reason}");
        await repo.SaveAsync();
    }

    public async Task AddContractAsync(int employeeId, ContractType type, DateTime start, DateTime? end, string? notes)
    {
        RequireHr();
        var old = await repo.ToListAsync(repo.Query<Contract>().Where(c => c.EmployeeId == employeeId && c.IsCurrent));
        var c = Contract.Create(employeeId, type, start, end, notes);
        foreach (var o in old) o.IsCurrent = false;
        repo.Add(c);
        audit.Log("Create", "Contract", employeeId, $"{type} {start:yyyy-MM-dd} -> {end:yyyy-MM-dd}");
        await repo.SaveAsync();
    }

    public async Task RenewContractAsync(int contractId, DateTime newEnd)
    {
        RequireHr();
        var c = await repo.FirstOrDefaultAsync(repo.Query<Contract>().Where(x => x.Id == contractId)) ?? throw new DomainException("err.notFound");
        var nc = c.Renew(newEnd);
        repo.Add(nc);
        repo.Add(new CareerEvent { EmployeeId = c.EmployeeId, Type = CareerEventType.ContractRenewal, Date = clock.Today, Details = $"until {newEnd:yyyy-MM-dd}", RecordedBy = me.Email });
        audit.Log("Renew", "Contract", contractId, $"until {newEnd:yyyy-MM-dd}");
        await repo.SaveAsync();
    }

    public async Task AddCareerEventAsync(int employeeId, CareerEventType type, string? details)
    {
        RequireHr();
        if (string.IsNullOrWhiteSpace(details)) throw new DomainException("err.reasonRequired");
        repo.Add(new CareerEvent { EmployeeId = employeeId, Type = type, Date = clock.Today, Details = details.Trim(), RecordedBy = me.Email });
        audit.Log("Create", "CareerEvent", employeeId, $"{type}: {details}");
        await repo.SaveAsync();
    }

    public async Task SetComponentAsync(int employeeId, int componentId, decimal amount)
    {
        if (!me.Can(Perm.EmpPay)) throw new DomainException("err.forbidden");
        if (amount < 0) throw new DomainException("err.salary");
        var row = await repo.FirstOrDefaultAsync(repo.Query<EmployeeSalaryComponent>().Where(c => c.EmployeeId == employeeId && c.ComponentId == componentId));
        var before = row?.Amount ?? 0;
        if (row == null) repo.Add(new EmployeeSalaryComponent { EmployeeId = employeeId, ComponentId = componentId, Amount = amount });
        else row.Amount = amount;
        audit.Log("Update", "SalaryComponent", employeeId, $"component #{componentId}: {before} -> {amount}");
        await repo.SaveAsync();
    }

    public Task<List<SalaryComponent>> ComponentsAsync() => repo.ToListAsync(repo.Query<SalaryComponent>().OrderBy(c => c.Type).ThenBy(c => c.Name));

    public async Task<List<(Contract Contract, Employee Employee)>> ExpiringContractsAsync(int days)
    {
        var today = clock.Today;
        var ids = await scope.VisibleEmployeeIdsAsync();
        var cs = (await repo.ToListAsync(repo.Query<Contract>().Where(c => c.IsCurrent && c.EndDate != null))).Where(c => c.ExpiresWithin(today, days));
        var emps = await LookupAsync();
        return cs.Where(c => ids == null || ids.Contains(c.EmployeeId)).Select(c => (c, emps[c.EmployeeId])).OrderBy(x => x.c.EndDate).ToList();
    }
}
