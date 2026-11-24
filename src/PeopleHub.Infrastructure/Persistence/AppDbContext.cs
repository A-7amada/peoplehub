using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

namespace PeopleHub.Infrastructure.Persistence;

/// <summary>EF Core implementation of the repository port. Navigations are auto-included.</summary>
public class AppDbContext(DbContextOptions options) : DbContext(options), IRepository
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

    public async Task AtomicAsync(Func<Task> work)
    {
        if (Database.CurrentTransaction != null) { await work(); return; }
        await using var tx = await Database.BeginTransactionAsync();
        try { await work(); await tx.CommitAsync(); }
        catch { await tx.RollbackAsync(); ChangeTracker.Clear(); throw; }
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        if (ChangeTracker.Entries().Any(e => (e.Entity is PayrollCalculationSnapshot or LeaveTransaction or AuditLog or DevicePunch or AttendanceEvidence or AttendancePeriodLock or AssetMovement or PerformanceResponse) && e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Append-only business evidence cannot be edited or deleted.");
        foreach (var e in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
            e.Property("Revision").CurrentValue = Guid.NewGuid().ToString("N");
        return base.SaveChangesAsync(ct);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        // SQLite has no native decimal; store as REAL so comparisons and sums work.
        c.Properties<decimal>().HaveConversion<double>();
        c.Properties<decimal?>().HaveConversion<double>();
        c.Properties<TimeSpan>().HaveConversion<long>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<LoginSession>().HasIndex(x=>x.TokenHash).IsUnique();
        b.Entity<PerformanceResponse>().HasIndex(x=>new{x.AppraisalId,x.Kind});
        b.Entity<CompanyAsset>().HasIndex(x=>x.Tag).IsUnique();b.Entity<AssetMovement>().HasIndex(x=>x.AssetId);
        b.Entity<EmployeeShiftDay>().HasIndex(x=>new{x.EmployeeId,x.Date}).IsUnique();
        b.Entity<EmployeeDocument>().HasIndex(x=>x.AttachmentToken).IsUnique();
        b.Entity<EmployeeDependent>();b.Entity<LifecycleTask>();b.Entity<EmployeeProbation>().HasIndex(x=>x.EmployeeId).IsUnique();b.Entity<HrFieldDefinition>().HasIndex(x=>x.Label).IsUnique();b.Entity<EmployeeFieldValue>().HasIndex(x=>new{x.EmployeeId,x.FieldId}).IsUnique();
        b.Entity<AttendancePeriodLock>().HasIndex(x=>new{x.EmployeeId,x.Year,x.Month}).IsUnique();
        b.Entity<EmploymentChange>().HasIndex(x=>new{x.EmployeeId,x.EffectiveOn,x.Kind}).IsUnique();
        b.Entity<AttendanceEvidence>().HasIndex(x=>new{x.EmployeeId,x.Date});
        b.Entity<DevicePunch>().HasIndex(x=>x.IdempotencyKey).IsUnique();
        b.Entity<PayrollCalculationSnapshot>().HasIndex(x => new { x.RunId, x.Version }).IsUnique();
        b.Entity<LeaveTransaction>();
        b.Entity<EmployeeAssignment>();
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
        b.Entity<ApprovalAction>(); b.Entity<Role>().HasIndex(x => x.Code).IsUnique(); b.Entity<RolePermission>(); b.Entity<Delegation>(); b.Entity<AppSetting>().HasIndex(x => x.Key).IsUnique(); b.Entity<DevicePunch>().HasIndex(x => new { x.DeviceUserId, x.Time }); b.Entity<EmailOutbox>(); b.Entity<PasswordReset>(); b.Entity<JobOpening>().Navigation(x => x.Department).AutoInclude(); b.Entity<Candidate>().Navigation(x => x.Job).AutoInclude(); b.Entity<PerformanceReview>().Navigation(x => x.Employee).AutoInclude(); b.Entity<TrainingCourse>(); b.Entity<PerfCycle>(); b.Entity<Feedback360>().HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerEmployeeId).OnDelete(DeleteBehavior.Restrict); b.Entity<Feedback360>().Navigation(x => x.Reviewer).AutoInclude(); b.Entity<CompetencyTemplate>(); b.Entity<CompetencyItem>(); b.Entity<PerfReminder>(); b.Entity<PerfBonus>(); b.Entity<PerfGoal>(); b.Entity<DevItem>(); b.Entity<PerfAppraisal>().Navigation(x => x.Employee).AutoInclude(); b.Entity<PerfAppraisal>().Navigation(x => x.Cycle).AutoInclude(); b.Entity<TrainingEnrollment>().Navigation(x => x.Course).AutoInclude(); b.Entity<TrainingEnrollment>().Navigation(x => x.Employee).AutoInclude(); b.Entity<Attachment>().HasIndex(x => x.Token).IsUnique();
        b.Entity<WorkflowDefinition>().Ignore(x => x.Steps); b.Entity<WorkflowStep>().Ignore(x => x.Token); b.Entity<Notification>(); b.Entity<AuditLog>();
        b.Ignore<Approvable>();
        b.Entity<PerfBonus>().HasIndex(x => x.AppraisalId).IsUnique();
        b.Entity<PerfAppraisal>().HasIndex(x => x.LegacyReviewId).IsUnique();
        b.Entity<Feedback360>().HasIndex(x => new { x.AppraisalId, x.ReviewerEmployeeId }).IsUnique();
        b.Entity<PayrollLine>().HasIndex(x => new { x.RunId, x.EmployeeId }).IsUnique();
        b.Entity<PasswordReset>().HasIndex(x => x.Token).IsUnique();
        foreach (var entity in b.Model.GetEntityTypes())
            b.Entity(entity.ClrType).Property<string>("Revision").HasDefaultValue("").IsConcurrencyToken();
        if (Database.IsNpgsql())
            foreach (var prop in b.Model.GetEntityTypes().SelectMany(t => t.GetProperties()).Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                prop.SetColumnType("timestamp without time zone");
                prop.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified), v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified)));
            }
    }
}
