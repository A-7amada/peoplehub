using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

namespace PeopleHub.Infrastructure.Persistence;

/// <summary>EF Core implementation of the repository port. Navigations are auto-included.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IRepository
{
    public IQueryable<T> Query<T>() where T : class => Set<T>();
    public void Add<T>(T entity) where T : class => Set<T>().Add(entity);
    public void AddRange<T>(IEnumerable<T> entities) where T : class => Set<T>().AddRange(entities);
    public void Remove<T>(T entity) where T : class => Set<T>().Remove(entity);
    public Task<List<T>> ToListAsync<T>(IQueryable<T> q) => EntityFrameworkQueryableExtensions.ToListAsync(q);
    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> q) => EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(q);
    public Task<bool> AnyAsync<T>(IQueryable<T> q) => EntityFrameworkQueryableExtensions.AnyAsync(q);
    public Task<int> CountAsync<T>(IQueryable<T> q) => EntityFrameworkQueryableExtensions.CountAsync(q);
    public Task SaveAsync() => SaveChangesAsync();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        // SQLite has no native decimal; store as REAL so comparisons and sums work.
        c.Properties<decimal>().HaveConversion<double>();
        c.Properties<decimal?>().HaveConversion<double>();
        c.Properties<TimeSpan>().HaveConversion<long>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.JobTitle).WithMany().HasForeignKey(x => x.JobTitleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            e.Navigation(x => x.JobTitle).AutoInclude();
            e.Navigation(x => x.Department).AutoInclude();
            e.Navigation(x => x.Branch).AutoInclude();
        });
        // Approvable is an unmapped base: each request type gets its own table.
        b.Entity<LeaveRequest>(e => { e.Navigation(x => x.Employee).AutoInclude(); e.Navigation(x => x.LeaveType).AutoInclude(); });
        b.Entity<OvertimeRequest>().Navigation(x => x.Employee).AutoInclude();
        b.Entity<AttendanceCorrection>().Navigation(x => x.Employee).AutoInclude();
        b.Entity<AdvanceRequest>().Navigation(x => x.Employee).AutoInclude();
        b.Entity<AttendanceRecord>().HasIndex(x => new { x.EmployeeId, x.Date }).IsUnique();
        b.Entity<AttendanceRecord>().Navigation(x => x.Employee).AutoInclude();
        b.Entity<LeaveBalance>().Navigation(x => x.LeaveType).AutoInclude();
        b.Entity<EmployeeSalaryComponent>().Navigation(x => x.Component).AutoInclude();
        b.Entity<PayrollLine>().Navigation(x => x.Employee).AutoInclude();
        b.Entity<PayrollRun>().HasIndex(x => new { x.Year, x.Month }).IsUnique();
        b.Entity<Branch>(); b.Entity<Department>(); b.Entity<JobTitle>(); b.Entity<Shift>(); b.Entity<Holiday>(); b.Entity<LeaveType>();
        b.Entity<SalaryComponent>(); b.Entity<Contract>(); b.Entity<StatusHistory>(); b.Entity<CareerEvent>();
        b.Entity<ApprovalAction>(); b.Entity<Role>().HasIndex(x => x.Code).IsUnique(); b.Entity<RolePermission>(); b.Entity<Delegation>(); b.Entity<Attachment>().HasIndex(x => x.Token).IsUnique();
        b.Entity<WorkflowDefinition>().Ignore(x => x.Steps); b.Entity<WorkflowStep>().Ignore(x => x.Token); b.Entity<Notification>(); b.Entity<AuditLog>();
        b.Ignore<Approvable>();
    }
}
