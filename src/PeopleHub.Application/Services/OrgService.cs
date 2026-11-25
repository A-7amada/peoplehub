using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

namespace PeopleHub.Application.Services;

public record DeptNode(Department Dept, string? ManagerName, string? ManagerNameAr, int Headcount, List<DeptNode> Children);
public record OrgOverview(List<DeptNode> Tree, List<Branch> Branches, List<JobTitle> Titles, Dictionary<int, int> TitleCounts, Dictionary<int, int> BranchCounts);

public class OrgService(IRepository repo, AuditService audit, ICurrentUser me)
{
    private void NeedManage(){if(!me.Can(Perm.OrgManage))throw new DomainException("err.forbidden");}

    public async Task<OrgOverview> GetAsync()
    {
        var depts = await repo.ToListAsync(repo.Query<Department>().OrderBy(d => d.Name));
        var emps = await repo.ToListAsync(repo.Query<Employee>().Where(e => e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave));
        var byId = emps.ToDictionary(e => e.Id);
        DeptNode Build(Department d) => new(d,
            d.ManagerId != null && byId.TryGetValue(d.ManagerId.Value, out var m) ? m.FullName : null,
            d.ManagerId != null && byId.TryGetValue(d.ManagerId.Value, out var m2) ? m2.FullNameAr : null,
            emps.Count(e => e.DepartmentId == d.Id),
            depts.Where(c => c.ParentId == d.Id).Select(Build).ToList());
        var roots = depts.Where(d => d.ParentId == null).Select(Build).ToList();
        return new OrgOverview(roots, await repo.ToListAsync(repo.Query<Branch>().OrderBy(b => b.Name)),
            await repo.ToListAsync(repo.Query<JobTitle>().OrderBy(t => t.Grade).ThenBy(t => t.Name)),
            emps.GroupBy(e => e.JobTitleId).ToDictionary(g => g.Key, g => g.Count()),
            emps.GroupBy(e => e.BranchId).ToDictionary(g => g.Key, g => g.Count()));
    }

    public Task<List<Department>> DepartmentsAsync() => repo.ToListAsync(repo.Query<Department>().OrderBy(d => d.Name));
    public Task<List<Branch>> BranchesAsync() => repo.ToListAsync(repo.Query<Branch>().OrderBy(d => d.Name));
    public Task<List<JobTitle>> TitlesAsync() => repo.ToListAsync(repo.Query<JobTitle>().OrderBy(d => d.Name));
    public Task<List<Shift>> ShiftsAsync() => repo.ToListAsync(repo.Query<Shift>().OrderBy(d => d.Start));

    public Task AddDepartmentAsync(string name, string? nameAr, int? parentId, int? managerId, string? costCenter, int target)=>repo.AtomicAsync(async()=>
    {
        NeedManage();
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("err.name");
        if(target<0)throw new DomainException("err.required");
        if(parentId!=null&&!await repo.AnyAsync(repo.Query<Department>().Where(x=>x.Id==parentId)))throw new DomainException("err.notFound");
        if(managerId!=null&&!await repo.AnyAsync(repo.Query<Employee>().Where(x=>x.Id==managerId&&(x.Status==EmployeeStatus.Active||x.Status==EmployeeStatus.OnLeave))))throw new DomainException("err.notFound");
        var d = new Department { Name = name.Trim(), NameAr = nameAr?.Trim() ?? "", ParentId = parentId, ManagerId = managerId, CostCenter = costCenter?.Trim() ?? "", TargetHeadcount = target };
        repo.Add(d);
        audit.Log("Create", "Department", name, $"parent={parentId} manager={managerId}");
        await repo.SaveAsync();
    });

    public Task ToggleDepartmentAsync(int id)=>repo.AtomicAsync(async()=>
    {
        NeedManage();
        var d = await repo.FirstOrDefaultAsync(repo.Query<Department>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
        d.IsActive = !d.IsActive;
        audit.Log("Update", "Department", id, $"IsActive {!d.IsActive} -> {d.IsActive}");
        await repo.SaveAsync();
    });

    public Task AddJobTitleAsync(string name, string? nameAr, int grade, decimal min, decimal max, string? description)=>repo.AtomicAsync(async()=>
    {
        NeedManage();
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("err.name");
        if(grade is <1 or >12)throw new DomainException("err.required");
        if (min < 0 || max < min) throw new DomainException("err.salaryRange");
        repo.Add(new JobTitle { Name = name.Trim(), NameAr = nameAr?.Trim() ?? "", Grade = grade, MinSalary = min, MaxSalary = max, Description = description?.Trim() ?? "" });
        audit.Log("Create", "JobTitle", name, $"grade={grade} range={min}-{max}");
        await repo.SaveAsync();
    });
}
