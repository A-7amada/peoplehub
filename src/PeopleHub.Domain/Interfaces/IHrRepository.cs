using PeopleHub.Domain.Entities;

namespace PeopleHub.Domain.Interfaces;

public interface IHrRepository
{
    IQueryable<Employee> Employees { get; }
    IQueryable<Department> Departments { get; }
    IQueryable<LeaveRequest> Leaves { get; }
    IQueryable<AppUser> Users { get; }
    void Add(Employee e);
    void Add(LeaveRequest l);
    Task<List<T>> ToListAsync<T>(IQueryable<T> q);
    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> q);
    Task<int> CountAsync<T>(IQueryable<T> q);
    Task SaveAsync();
}
