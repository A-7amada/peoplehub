using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

namespace PeopleHub.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IHrRepository
{
    public DbSet<Employee> EmployeeSet => Set<Employee>();
    public DbSet<Department> DepartmentSet => Set<Department>();
    public DbSet<LeaveRequest> LeaveSet => Set<LeaveRequest>();
    public DbSet<AppUser> UserSet => Set<AppUser>();

    public IQueryable<Employee> Employees => EmployeeSet.Include(e => e.Department);
    public IQueryable<Department> Departments => DepartmentSet;
    public IQueryable<LeaveRequest> Leaves => LeaveSet.Include(l => l.Employee).ThenInclude(e => e!.Department);
    public IQueryable<AppUser> Users => UserSet;
    public void Add(Employee e) => EmployeeSet.Add(e);
    public void Add(LeaveRequest l) => LeaveSet.Add(l);
    public Task<List<T>> ToListAsync<T>(IQueryable<T> q) => EntityFrameworkQueryableExtensions.ToListAsync(q);
    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> q) => EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(q)!;
    public Task<int> CountAsync<T>(IQueryable<T> q) => EntityFrameworkQueryableExtensions.CountAsync(q);
    public Task SaveAsync() => SaveChangesAsync();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Employee>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(200);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.MonthlySalary).HasPrecision(12, 2);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<LeaveRequest>(e =>
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade));
        b.Entity<AppUser>(e => e.HasIndex(x => x.Email).IsUnique());
    }
}
