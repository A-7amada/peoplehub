using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Domain.Security;

namespace PeopleHub.Application.Services;

public record EmployeeFilter(string? Search, int? DepartmentId, EmployeeStatus? Status);
public record DashboardStats(int Headcount, int OnLeave, int PendingLeaves, decimal MonthlyPayroll,
    List<(string Dept, string DeptAr, int Count)> ByDepartment);

public class AuthService(IHrRepository repo)
{
    public async Task<AppUser?> LoginAsync(string email, string password)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var u = await repo.FirstOrDefaultAsync(repo.Users.Where(x => x.Email == email && x.IsActive));
        return u != null && PasswordHasher.Verify(password ?? "", u.PasswordHash) ? u : null;
    }
}

public class EmployeeService(IHrRepository repo)
{
    public Task<List<Employee>> SearchAsync(EmployeeFilter f)
    {
        var q = repo.Employees.Where(e => true);
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToLower();
            q = q.Where(e => e.FullName.ToLower().Contains(s) || e.FullNameAr.Contains(s) ||
                             e.Email.Contains(s) || e.JobTitle.ToLower().Contains(s));
        }
        if (f.DepartmentId is { } d) q = q.Where(e => e.DepartmentId == d);
        if (f.Status is { } st) q = q.Where(e => e.Status == st);
        return repo.ToListAsync(q.OrderBy(e => e.FullName));
    }

    public Task<List<Department>> DepartmentsAsync() => repo.ToListAsync(repo.Departments.OrderBy(d => d.Name));
    public Task<Employee?> GetAsync(int id) => repo.FirstOrDefaultAsync(repo.Employees.Where(e => e.Id == id));

    public async Task SaveAsync(Employee e)
    {
        e.Validate();
        if (await repo.CountAsync(repo.Employees.Where(x => x.Email == e.Email && x.Id != e.Id)) > 0)
            throw new DomainException("err.emailTaken");
        if (e.Id == 0) repo.Add(e);
        await repo.SaveAsync();
    }

    public async Task TerminateAsync(int id)
    {
        var e = await GetAsync(id) ?? throw new DomainException("err.notFound");
        e.Terminate();
        await repo.SaveAsync();
    }
}

public class LeaveService(IHrRepository repo, TimeProvider clock)
{
    public Task<List<LeaveRequest>> ListAsync(int? employeeId, LeaveStatus? status)
    {
        var q = repo.Leaves.Where(l => true);
        if (employeeId is { } id) q = q.Where(l => l.EmployeeId == id);
        if (status is { } s) q = q.Where(l => l.Status == s);
        return repo.ToListAsync(q.OrderByDescending(l => l.CreatedAt));
    }

    public async Task RequestAsync(int employeeId, LeaveType type, DateTime start, DateTime end, string? reason)
    {
        var emp = await repo.FirstOrDefaultAsync(repo.Employees.Where(e => e.Id == employeeId))
                  ?? throw new DomainException("err.notFound");
        var req = LeaveRequest.Create(emp, type, start, end, reason, clock.GetUtcNow().UtcDateTime);
        var existing = await repo.ToListAsync(repo.Leaves.Where(l => l.EmployeeId == employeeId));
        if (existing.Any(req.Overlaps)) throw new DomainException("err.overlap");
        repo.Add(req);
        await repo.SaveAsync();
    }

    public Task ApproveAsync(int id) => DecideAsync(id, (l, e) => l.Approve(e));
    public Task RejectAsync(int id) => DecideAsync(id, (l, e) => l.Reject());

    public async Task CancelAsync(int id, int? onlyForEmployeeId)
    {
        await DecideAsync(id, (l, e) =>
        {
            if (onlyForEmployeeId is { } own && l.EmployeeId != own) throw new DomainException("err.forbidden");
            l.Cancel(e);
        });
    }

    private async Task DecideAsync(int id, Action<LeaveRequest, Employee> action)
    {
        var l = await repo.FirstOrDefaultAsync(repo.Leaves.Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
        var e = await repo.FirstOrDefaultAsync(repo.Employees.Where(x => x.Id == l.EmployeeId)) ?? throw new DomainException("err.notFound");
        action(l, e);
        await repo.SaveAsync();
    }
}

public class DashboardService(IHrRepository repo)
{
    public async Task<DashboardStats> GetAsync()
    {
        var active = await repo.ToListAsync(repo.Employees.Where(e => e.Status != EmployeeStatus.Terminated));
        var depts = await repo.ToListAsync(repo.Departments);
        var pending = await repo.CountAsync(repo.Leaves.Where(l => l.Status == LeaveStatus.Pending));
        return new DashboardStats(active.Count, active.Count(e => e.Status == EmployeeStatus.OnLeave), pending,
            active.Sum(e => e.MonthlySalary),
            depts.Select(d => (d.Name, d.NameAr, active.Count(e => e.DepartmentId == d.Id))).ToList());
    }
}
