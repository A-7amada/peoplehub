using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common.Tenancy;
using PeopleHub.Application.Common.Events;

namespace PeopleHub.Infrastructure.Persistence;

/// <summary>EF Core implementation of the repository port. Navigations are auto-included.</summary>
public class AppDbContext : DbContext, IRepository
{
    readonly ITenantContext? tenant;
    readonly IDomainEventDispatcher? events;
    public AppDbContext(DbContextOptions options, ITenantContext? tenant = null, IDomainEventDispatcher? events = null) : base(options) { this.tenant = tenant; this.events = events; }
    /// <summary>Read by the global query filter on every query (Ambient Context, ADR-0003). No context means the default tenant.</summary>
    protected int CurrentTenantId => tenant?.TenantId ?? TenantIds.Default;
    static readonly string[] NotTenantOwned = [nameof(Tenant), nameof(ScheduledJobState), nameof(PlatformUser)];
    void ApplyTenantFilter<T>(ModelBuilder b) where T : class =>
        b.Entity<T>().HasQueryFilter(e => EF.Property<int>(e, "TenantId") == CurrentTenantId);

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

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var raised = CollectDomainEvents();
        var n = await SaveGuardedAsync(ct);
        if (events != null && raised.Count > 0) await events.DispatchAsync(raised, ct);
        return n;
    }

    /// <summary>Events are derived from tracked state changes before the save and published only after it succeeded.</summary>
    List<IDomainEvent> CollectDomainEvents()
    {
        var list = new List<IDomainEvent>();
        foreach (var e in ChangeTracker.Entries<Employee>().Where(x => x.State == EntityState.Modified))
        {
            var p = e.Property(x => x.Status);
            if (p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)) list.Add(new EmployeeStatusChanged(CurrentTenantId, e.Entity.Id, p.OriginalValue, p.CurrentValue));
        }
        return list;
    }

    Task<int> SaveGuardedAsync(CancellationToken ct)
    {
        if (ChangeTracker.Entries().Any(e => (e.Entity is EmployeeGrievanceAppeal or EmployeeGrievanceAppealDecision or EmployeeGrievanceResponse or EmployeeCaseFile or EmployeeCaseAccess or ApprovalVote or PayrollCalculationSnapshot or LeaveTransaction or AuditLog or DevicePunch or AttendanceEvidence or AttendancePeriodLock or AssetMovement or PerformanceResponse or WorkflowRevision or PipProgress or ApprovalAssignment or ApprovalAction or TrainingCertificate or TrainingOutcome or RecruitmentEvent or CandidateOffer or CandidateOfferEvent or TrainingCertificateAlert or CompetencyApplication or BenefitPlan or BenefitEnrollment or BenefitCancellation or EmployeeCaseEvent or PayrollBankResult or ApprovalReminder or SalaryStructure or SalaryStructureApplication or PayrollAdjustment or PayrollAdjustmentEvent) && e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Append-only business evidence cannot be edited or deleted.");
        if(ChangeTracker.Entries<EmployeeCase>().Any(e=>e.State==EntityState.Deleted||e.State==EntityState.Modified&&e.Properties.Any(p=>p.IsModified&&p.Metadata.Name is not("ChangeStamp" or "Revision"))))throw new InvalidOperationException("Case source records cannot be edited or deleted.");
        if(ChangeTracker.Entries<EmployeeGrievance>().Any(e=>e.State==EntityState.Deleted||e.State==EntityState.Modified&&e.Properties.Any(p=>p.IsModified&&p.Metadata.Name is not("ChangeStamp" or "Revision"))))throw new InvalidOperationException("Grievance source records cannot be edited or deleted.");
        StampAndGuardTenant();
        foreach (var e in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
            e.Property("Revision").CurrentValue = Guid.NewGuid().ToString("N");
        return base.SaveChangesAsync(ct);
    }

    /// <summary>Tenant stamping (ADR-0002): new rows take the current tenant; an existing row can never change tenant
    /// or be written from another tenant's context.</summary>
    void StampAndGuardTenant()
    {
        foreach (var e in ChangeTracker.Entries().Where(e => e.Metadata.FindProperty("TenantId") != null))
        {
            var p = e.Property("TenantId");
            if (e.State == EntityState.Added) p.CurrentValue = CurrentTenantId;
            else if (e.State is EntityState.Modified or EntityState.Deleted)
            {
                if (e.State == EntityState.Modified && p.IsModified) throw new InvalidOperationException("TenantId cannot be changed.");
                if ((int)p.CurrentValue! != CurrentTenantId) throw new InvalidOperationException("Cross-tenant write refused.");
            }
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        ConfigureDecimalStorage(c); // Strategy: each provider decides how decimals are stored (ADR-0015).
        c.Properties<TimeSpan>().HaveConversion<long>();
    }

    /// <summary>Strategy hook. SQLite has no native decimal, so the local/test provider stores REAL.
    /// PgDbContext overrides this with exact numeric(18,2).</summary>
    protected virtual void ConfigureDecimalStorage(ModelConfigurationBuilder c)
    {
        c.Properties<decimal>().HaveConversion<double>();
        c.Properties<decimal?>().HaveConversion<double>();
    }

    /// <summary>Cipher for sensitive columns (ADR-0021). Tests override it with their own key.</summary>
    protected virtual PeopleHub.Infrastructure.Security.FieldCipher Cipher => PeopleHub.Infrastructure.Security.FieldCipher.Default;

    /// <summary>The one list of encrypted columns. Encrypted columns cannot be filtered or sorted in SQL.</summary>
    void ApplyFieldEncryption(ModelBuilder b)
    {
        var cipher = Cipher;
        void Encrypt<T>(System.Linq.Expressions.Expression<Func<T, string?>> property, string column) where T : class =>
            b.Entity<T>().Property(property).HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<string?, string?>(v => cipher.Encrypt(v, column), v => cipher.Decrypt(v, column)));
        Encrypt<AppUser>(x => x.TotpSecret, "AppUser.TotpSecret");
        Encrypt<Employee>(x => x.NationalId, "Employee.NationalId");
        Encrypt<Employee>(x => x.Iban, "Employee.Iban");
        Encrypt<PayrollLine>(x => x.IbanSnapshot, "PayrollLine.IbanSnapshot");
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        ApplyFieldEncryption(b);
        b.Entity<ApprovalVote>().HasIndex(x=>new{x.Kind,x.RequestId,x.StageIndex,x.ParticipantEmployeeId}).IsUnique();
        b.Entity<ApprovalVote>().HasIndex(x=>new{x.Kind,x.RequestId,x.StageIndex,x.ActorEmployeeId}).IsUnique();
        b.Entity<PayrollAdjustment>().HasIndex(x=>x.TargetRunId);b.Entity<PayrollAdjustmentEvent>().HasIndex(x=>new{x.AdjustmentId,x.Sequence}).IsUnique();
        b.Entity<SalaryStructure>();b.Entity<SalaryStructureApplication>().HasIndex(x=>x.EmployeeId);
        b.Entity<ApprovalReminder>().HasIndex(x=>new{x.Kind,x.RequestId,x.StageIndex,x.UserId,x.OnDate}).IsUnique();
        b.Entity<PayrollBankResult>().HasIndex(x=>new{x.LineId,x.Version}).IsUnique();
        b.Entity<EmployeeCaseFile>().HasIndex(x=>x.Token).IsUnique();
        b.Entity<IntegrationEvent>().HasIndex(x=>x.IdempotencyKey);b.Entity<IntegrationEvent>().HasIndex(x=>new{x.Status,x.NextAttemptAt});
        b.Entity<EmployeeGrievanceAppeal>().HasIndex(x=>x.ResponseId).IsUnique();
        b.Entity<EmployeeGrievanceAppealDecision>().HasIndex(x=>x.AppealId).IsUnique();
        b.Entity<EmployeeGrievance>().HasIndex(x=>x.EmployeeId);
        b.Entity<EmployeeGrievanceResponse>().HasIndex(x=>x.GrievanceId);
        b.Entity<EmployeeCaseAccess>().HasIndex(x=>new{x.CaseId,x.UserId});
        b.Entity<EmployeeCase>();b.Entity<EmployeeCaseEvent>().HasIndex(x=>x.CaseId);
        b.Entity<BenefitCancellation>().HasIndex(x=>x.EnrollmentId).IsUnique();b.Entity<BenefitEnrollment>().HasIndex(x=>new{x.EmployeeId,x.PlanId});b.Entity<BenefitPlan>();
        b.Entity<CompetencyApplication>().HasIndex(x=>x.AppraisalId).IsUnique();
        b.Entity<TrainingCourse>().Property(x=>x.ApprovalStatus).HasDefaultValue("Legacy");b.Entity<JobOpening>().Property(x=>x.ApprovalStatus).HasDefaultValue("Legacy");
        b.Entity<CandidateOffer>().HasIndex(x=>x.CandidateId);b.Entity<CandidateOfferEvent>().HasIndex(x=>x.OfferId);
        b.Entity<CandidateInterview>().HasIndex(x=>new{x.CandidateId,x.StartsAt});b.Entity<RecruitmentEvent>().HasIndex(x=>x.CandidateId);
        b.Entity<TrainingCertificateAlert>().HasIndex(x=>new{x.CertificateId,x.OnDate}).IsUnique();
        b.Entity<TrainingCertificate>().HasIndex(x=>x.AttachmentToken).IsUnique();b.Entity<TrainingOutcome>().HasIndex(x=>x.EnrollmentId);
        b.Entity<ApprovalAssignment>().HasIndex(x=>new{x.Kind,x.RequestId,x.StageIndex});
        b.Entity<PipMilestone>().HasIndex(x=>x.AppraisalId);b.Entity<PipProgress>().HasIndex(x=>x.MilestoneId);
        b.Entity<WorkflowRevision>().HasIndex(x=>new{x.Kind,x.Hash});
        b.Entity<LoginSession>().HasIndex(x=>x.TokenHash).IsUnique();
        b.Entity<PerformanceResponse>().HasIndex(x=>new{x.AppraisalId,x.Kind});
        b.Entity<AssetMovement>().HasIndex(x=>x.AssetId);
        b.Entity<EmployeeShiftDay>().HasIndex(x=>new{x.EmployeeId,x.Date}).IsUnique();
        b.Entity<EmployeeDocument>().HasIndex(x=>x.AttachmentToken).IsUnique();
        b.Entity<EmployeeDependent>();b.Entity<LifecycleTask>();b.Entity<EmployeeProbation>().HasIndex(x=>x.EmployeeId).IsUnique();b.Entity<HrFieldDefinition>().HasIndex(x=>x.Label);b.Entity<EmployeeFieldValue>().HasIndex(x=>new{x.EmployeeId,x.FieldId}).IsUnique();
        b.Entity<AttendancePeriodLock>().HasIndex(x=>new{x.EmployeeId,x.Year,x.Month}).IsUnique();
        b.Entity<EmploymentChange>().HasIndex(x=>new{x.EmployeeId,x.EffectiveOn,x.Kind}).IsUnique();
        b.Entity<AttendanceEvidence>().HasIndex(x=>new{x.EmployeeId,x.Date});
        b.Entity<DevicePunch>().HasIndex(x=>x.IdempotencyKey);
        b.Entity<PayrollCalculationSnapshot>().HasIndex(x => new { x.RunId, x.Version }).IsUnique();
        b.Entity<LeaveTransaction>();
        b.Entity<EmployeeAssignment>();
        b.Entity<AppUser>().HasIndex(x => x.Email);
        b.Entity<Employee>(e =>
        {
            
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
        
        b.Entity<Branch>(); b.Entity<Department>(); b.Entity<JobTitle>(); b.Entity<Shift>(); b.Entity<Holiday>(); b.Entity<LeaveType>();
        b.Entity<SalaryComponent>(); b.Entity<Contract>(); b.Entity<StatusHistory>(); b.Entity<CareerEvent>();
        b.Entity<ApprovalAction>();  b.Entity<RolePermission>(); b.Entity<Delegation>(); b.Entity<AppSetting>().HasIndex(x => x.Key); b.Entity<DevicePunch>().HasIndex(x => new { x.DeviceUserId, x.Time }); b.Entity<EmailOutbox>(); b.Entity<PasswordReset>(); b.Entity<JobOpening>().Navigation(x => x.Department).AutoInclude(); b.Entity<Candidate>().Navigation(x => x.Job).AutoInclude(); b.Entity<PerformanceReview>().Navigation(x => x.Employee).AutoInclude(); b.Entity<TrainingCourse>(); b.Entity<PerfCycle>(); b.Entity<Feedback360>().HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerEmployeeId).OnDelete(DeleteBehavior.Restrict); b.Entity<Feedback360>().Navigation(x => x.Reviewer).AutoInclude(); b.Entity<CompetencyTemplate>(); b.Entity<CompetencyItem>(); b.Entity<PerfReminder>(); b.Entity<PerfBonus>(); b.Entity<PerfGoal>(); b.Entity<DevItem>(); b.Entity<PerfAppraisal>().Navigation(x => x.Employee).AutoInclude(); b.Entity<PerfAppraisal>().Navigation(x => x.Cycle).AutoInclude(); b.Entity<TrainingEnrollment>().Navigation(x => x.Course).AutoInclude(); b.Entity<TrainingEnrollment>().Navigation(x => x.Employee).AutoInclude(); b.Entity<Attachment>().HasIndex(x => x.Token).IsUnique();
        b.Entity<WorkflowDefinition>().Ignore(x => x.Steps); b.Entity<WorkflowStep>().Ignore(x => x.Token); b.Entity<Notification>(); b.Entity<AuditLog>();
        b.Ignore<Approvable>();
        b.Entity<PerfBonus>().HasIndex(x => x.AppraisalId).IsUnique();
        b.Entity<PerfAppraisal>().HasIndex(x => x.LegacyReviewId).IsUnique();
        b.Entity<Feedback360>().HasIndex(x => new { x.AppraisalId, x.ReviewerEmployeeId }).IsUnique();
        b.Entity<PayrollLine>().HasIndex(x => new { x.RunId, x.EmployeeId }).IsUnique();
        b.Entity<PasswordReset>().HasIndex(x => x.Token).IsUnique();
        b.Entity<ScheduledJobState>().HasIndex(x => x.Name).IsUnique();
        
        b.Entity<StagedEmployeeRow>().HasIndex(x => new { x.BatchId, x.RowNumber }).IsUnique();
        b.Entity<StagedEmployeeRow>().HasOne<EmployeeImportBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CompanyAsset>(); b.Entity<Employee>(); b.Entity<PayrollRun>(); b.Entity<EmployeeImportBatch>(); b.Entity<Role>(); b.Entity<RehireRequest>(); b.Entity<TotpRecoveryCode>(); b.Entity<EmployeeLetter>(); b.Entity<LeavePolicy>(); b.Entity<LeaveEntitlementTier>(); // registered before tenant columns are added
        b.Entity<Tenant>().HasData(new Tenant { Id = TenantIds.Default, Name = "Demo company", Slug = "demo" });
        b.Entity<Tenant>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<PlatformUser>().HasIndex(x => x.Email).IsUnique();
        // Shared database, TenantId on every customer table with a global query filter (ADR-0001, ADR-0002).
        foreach (var entity in b.Model.GetEntityTypes().Where(t => t.BaseType == null && !t.IsOwned() && !NotTenantOwned.Contains(t.ClrType.Name)).ToList())
        {
            b.Entity(entity.ClrType).Property<int>("TenantId").HasDefaultValue(TenantIds.Default);
            b.Entity(entity.ClrType).HasIndex("TenantId");
            typeof(AppDbContext).GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(entity.ClrType).Invoke(this, [b]);
        }
        // Natural keys are unique per tenant, not globally (ADR-0002). Logins stay globally unique by email.
        b.Entity<CompanyAsset>().HasIndex("TenantId", nameof(CompanyAsset.Tag)).IsUnique();
        b.Entity<Employee>().HasIndex("TenantId", nameof(Employee.Code)).IsUnique();
        b.Entity<Employee>().HasIndex("TenantId", nameof(Employee.Email)).IsUnique();
        b.Entity<PayrollRun>().HasIndex("TenantId", nameof(PayrollRun.Year), nameof(PayrollRun.Month)).IsUnique();
        b.Entity<EmployeeImportBatch>().HasIndex("TenantId", nameof(EmployeeImportBatch.FileHash)).IsUnique();
        b.Entity<TotpRecoveryCode>().HasIndex("TenantId", nameof(TotpRecoveryCode.UserId), nameof(TotpRecoveryCode.CodeHash));
        b.Entity<RehireRequest>().HasIndex("TenantId", nameof(RehireRequest.EmployeeId), nameof(RehireRequest.Status));
        b.Entity<EmployeeLetter>().HasIndex("TenantId", nameof(EmployeeLetter.EmployeeId), nameof(EmployeeLetter.Status));
        // ADR-0023 stage 1: values people or devices choose are unique per tenant, not across the whole database.
        b.Entity<IntegrationEvent>().HasIndex("TenantId", nameof(IntegrationEvent.IdempotencyKey)).IsUnique();
        b.Entity<WorkflowRevision>().HasIndex("TenantId", nameof(WorkflowRevision.Kind), nameof(WorkflowRevision.Hash)).IsUnique();
        b.Entity<HrFieldDefinition>().HasIndex("TenantId", nameof(HrFieldDefinition.Label)).IsUnique();
        b.Entity<DevicePunch>().HasIndex("TenantId", nameof(DevicePunch.IdempotencyKey)).IsUnique();
        b.Entity<AppSetting>().HasIndex("TenantId", nameof(AppSetting.Key)).IsUnique();
        b.Entity<AppUser>().HasIndex("TenantId", nameof(AppUser.Email)).IsUnique();
        b.Entity<LeavePolicy>().HasIndex("TenantId", nameof(LeavePolicy.LeaveTypeId)).IsUnique();
        b.Entity<LeaveEntitlementTier>().HasIndex("TenantId", nameof(LeaveEntitlementTier.LeaveTypeId));
        b.Entity<Role>().HasIndex("TenantId", nameof(Role.Code)).IsUnique();
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
