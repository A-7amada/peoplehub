using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using PeopleHub.Domain.Entities;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
using PeopleHub.Application.Modules.Benefits;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Application.Modules.Identity;
using PeopleHub.Application.Modules.Leave;
using PeopleHub.Application.Modules.Operations;
using PeopleHub.Application.Modules.Organization;
using PeopleHub.Application.Modules.Payroll;
using PeopleHub.Application.Modules.Platform;
using PeopleHub.Application.Modules.Relations;
using PeopleHub.Application.Modules.Reporting;
using PeopleHub.Application.Modules.Requests;
using PeopleHub.Application.Modules.Talent;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var dbPath = builder.Configuration["DB_PATH"] ?? "peoplehub.db";
var pg = builder.Configuration["POSTGRES_CONNECTION"];
if (!string.IsNullOrWhiteSpace(pg))
{
    builder.Services.AddDbContext<PgDbContext>(o => o.UseNpgsql(pg));
    builder.Services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<PgDbContext>());
}
else builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath};Default Timeout=15"));
builder.Services.AddScoped<IRepository>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AppClock>();
builder.Services.AddSingleton<RequestMetrics>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, WebCurrentUser>();
builder.Services.AddScoped<HttpTenantContext>();
builder.Services.AddScoped<PeopleHub.Application.Common.Tenancy.ITenantContext>(sp => sp.GetRequiredService<HttpTenantContext>());
builder.Services.AddScoped<PeopleHub.Application.Common.Events.IDomainEventDispatcher, PeopleHub.Infrastructure.Events.DomainEventDispatcher>();
builder.Services.AddScoped<PeopleHub.Application.Common.Events.IDomainEventHandler<PeopleHub.Application.Common.Events.EmployeeStatusChanged>, PeopleHub.Infrastructure.Events.EmployeeStatusChangedLogger>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PeopleHub.Infrastructure.Caching.InProcessCache>();
builder.Services.AddScoped<PeopleHub.Application.Common.Caching.ICache>(sp => new PeopleHub.Infrastructure.Caching.TenantKeyedCache(sp.GetRequiredService<PeopleHub.Infrastructure.Caching.InProcessCache>(), sp.GetRequiredService<PeopleHub.Application.Common.Tenancy.ITenantContext>()));
builder.Services.AddScoped<AccessScope>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IntegrationOutbox>();
builder.Services.AddScoped<ScheduledJobs>();
builder.Services.AddScoped<OperationsProjection>();
builder.Services.AddScoped<PeopleHub.Application.Common.Tenancy.ITenantOverride>(sp => sp.GetRequiredService<HttpTenantContext>());
builder.Services.AddScoped<PeopleHub.Application.Common.Tenancy.ITenantProvisioner, PeopleHub.Infrastructure.Persistence.TenantProvisioner>();
builder.Services.AddScoped<PeopleHub.Infrastructure.Persistence.TenantAdminService>();
builder.Services.AddScoped<PeopleHub.Infrastructure.Persistence.PlatformAuthService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<OrgService>();
builder.Services.AddScoped<GrievanceIntake>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<EmployeeImportValidation>();
builder.Services.AddScoped<EmployeeImportBatches>();
builder.Services.AddScoped<EmployeeLifecycle>();
builder.Services.AddScoped<AssetService>();
builder.Services.AddScoped<TrainingEvidence>();
builder.Services.AddScoped<TrainingExpiry>();
builder.Services.AddScoped<ApprovalReminders>();
builder.Services.AddScoped<BenefitService>();
builder.Services.AddScoped<EmployeeRelations>();
builder.Services.AddScoped<RecruitmentInterviews>();
builder.Services.AddScoped<RecruitmentFiles>();
builder.Services.AddScoped<RecruitmentOffers>();
builder.Services.AddScoped<HolidayService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AttendanceExceptions>();
builder.Services.AddScoped<RehireService>();
builder.Services.AddScoped<LetterService>();
builder.Services.AddScoped<PeopleHub.Application.Modules.Leave.LeavePolicyService>();
builder.Services.AddScoped<EmployeeDocuments>();
builder.Services.AddScoped<ShiftSchedule>();
builder.Services.AddScoped<EmploymentTimeline>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<LeaveService>();
builder.Services.AddScoped<LeaveCalendar>();
builder.Services.AddScoped<ApprovalService>();
builder.Services.AddScoped<PayrollService>();
builder.Services.AddScoped<PayrollJournals>();
builder.Services.AddScoped<PayrollAdjustments>();
builder.Services.AddScoped<SalaryStructures>();
builder.Services.AddScoped<PeopleHub.Application.Modules.Talent.SkillsInventory>();
builder.Services.AddScoped<PeopleHub.Application.Modules.Operations.IntegrationRegistry>();
builder.Services.AddScoped<PeopleHub.Application.Modules.Requests.WorkflowDrafts>();
builder.Services.AddScoped<PeopleHub.Application.Modules.Benefits.BenefitClaims>();
builder.Services.AddScoped<BankReconciliation>();
builder.Services.AddScoped<WorkflowService>();
builder.Services.AddScoped<AttachmentService>();
builder.Services.AddScoped<DelegationService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<SettingsService>(); builder.Services.AddScoped<DeviceService>(); builder.Services.AddScoped<EmailService>(); builder.Services.AddScoped<AccountService>(); builder.Services.AddScoped<TalentService>(); builder.Services.AddScoped<PerfService>(); builder.Services.AddScoped<ReportBuilderService>();
var protection = builder.Services.AddDataProtection().SetApplicationName("PeopleHub");
if (builder.Configuration["DATA_PROTECTION_PATH"] is string keyPath)
{ Directory.CreateDirectory(keyPath); protection.PersistKeysToFileSystem(new DirectoryInfo(keyPath)); }
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = SettingsService.GetInt("auth.rateLimitPerMinute"), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.Configure<ForwardedHeadersOptions>(o => { o.ForwardedHeaders = ForwardedHeaders.XForwardedProto; });
builder.Services.AddSingleton<Loc>();
// Domain services validate required business fields. Omitted optional string fields must not fail implicit nullable validation.
builder.Services.AddControllersWithViews(o => { o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true; o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()); });
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Denied"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax; o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        o.Events.OnValidatePrincipal = async c => {
            if (c.Principal?.FindFirst("platform")?.Value == "1")
            {
                // Platform staff: valid only for /Platform pages and sign-out, and only while the account is active with the same stamp.
                var p = c.HttpContext.Request.Path;
                var pdb = c.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var pu = int.TryParse(c.Principal.FindFirst("uid")?.Value, out var pid) ? await pdb.Set<PlatformUser>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == pid && x.IsActive) : null;
                var allowed = p.StartsWithSegments("/Platform") || p.StartsWithSegments("/Account/Logout") || p.StartsWithSegments("/Account/Lang") || p.StartsWithSegments("/css") || p.StartsWithSegments("/fonts") || p.StartsWithSegments("/favicon.ico");
                if (pu == null || pu.SecurityStamp != c.Principal.FindFirst("stamp")?.Value || !allowed) c.RejectPrincipal();
                return;
            }
            var id = c.Principal?.FindFirst("uid")?.Value;
            if (int.TryParse(c.Principal?.FindFirst("tenant_id")?.Value, out var tid)) c.HttpContext.RequestServices.GetRequiredService<HttpTenantContext>().Override = tid;
            var db = c.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var u = int.TryParse(id, out var uid) ? await db.Set<AppUser>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid && u.IsActive) : null;
            var sessions=c.HttpContext.RequestServices.GetRequiredService<SessionService>();
            var tenantActive = await db.Set<Tenant>().AsNoTracking().AnyAsync(t => t.Id == (tid > 0 ? tid : 1) && t.Status == "Active");
            if (u == null || !tenantActive || c.Principal?.FindFirst("stamp")?.Value != u.SecurityStamp || !await sessions.ValidAsync(u.Id,c.Principal?.FindFirst("sid")?.Value)) c.RejectPrincipal();
        }; o.ExpireTimeSpan = TimeSpan.FromHours(8); });
builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    // UI language switches between en and ar; number and date formatting stays en-US so digits and separators look the same in both.
    o.DefaultRequestCulture = new RequestCulture("en-US", "en");
    o.SupportedCultures = new[] { new CultureInfo("en-US") };
    o.SupportedUICultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
    o.RequestCultureProviders = new List<IRequestCultureProvider> { new CustomRequestCultureProvider(ctx =>
    {
        var parsed = CookieRequestCultureProvider.ParseCookieValue(ctx.Request.Cookies[".AspNetCore.Culture"] ?? "");
        var ui = parsed?.UICultures.FirstOrDefault().Value == "ar" ? "ar" : "en";
        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult("en-US", ui));
    }) };
});

builder.Services.AddHostedService<PerfWorker>();
builder.Services.AddHostedService<MailWorker>();
builder.Services.AddHostedService<BackupWorker>();
builder.Services.AddScoped<OperationsService>();
builder.Logging.AddSimpleConsole(o => { o.IncludeScopes = true; o.SingleLine = true; });
PeopleHub.Infrastructure.Security.FieldCipher.ConfigureDefault(builder.Configuration["FIELD_ENCRYPTION_KEY"], builder.Configuration["FIELD_ENCRYPTION_KEY_PREVIOUS"]);
var app = builder.Build();
// Settings are cached per company (ADR-0023 stage 4): the ambient company comes from the request, and a company's saved settings are read once on first use.
{
    var accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
    SettingsService.AmbientTenant = () => accessor.HttpContext?.RequestServices.GetService<HttpTenantContext>()?.TenantId ?? PeopleHub.Application.Common.Tenancy.TenantIds.Default;
    SettingsService.Loader = tid =>
    {
        using var sc = app.Services.CreateScope();
        sc.ServiceProvider.GetRequiredService<PeopleHub.Application.Common.Tenancy.ITenantOverride>().Override = tid;
        var d = sc.ServiceProvider.GetRequiredService<PeopleHub.Infrastructure.Persistence.AppDbContext>();
        try { return d.Set<AppSetting>().AsNoTracking().ToList().ToDictionary(x => x.Key, x => x.Value); } catch { return new Dictionary<string, string>(); }
    };
}
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Resilience pattern (Retry): migrations are transactional on PostgreSQL, so a failed attempt (for example a lock conflict with the previous instance during a rolling deploy) leaves the schema unchanged and is safe to repeat.
    var startupLog = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    for (var attempt = 1; ; attempt++)
    {
        try { await db.Database.MigrateAsync(); break; }
        catch (Exception ex) when (attempt < 4)
        {
            startupLog.LogError(ex, "Migration attempt {Attempt} failed; retrying in {Delay}s", attempt, attempt * 5);
            await Task.Delay(TimeSpan.FromSeconds(attempt * 5));
        }
    }
    var hrPass = app.Environment.IsDevelopment() ? "Admin123!" : app.Configuration["HR_SEED_PASSWORD"];
    await SeedData.InitializeAsync(db, hrPass, "Demo123!", scope.ServiceProvider.GetRequiredService<AppClock>().Today);
    await PeopleHub.Infrastructure.Persistence.PlatformAuthService.SeedFirstAsync(db, app.Configuration["PLATFORM_ADMIN_EMAIL"], app.Configuration["PLATFORM_ADMIN_PASSWORD"]);
    await scope.ServiceProvider.GetRequiredService<SettingsService>().LoadAsync();
    await AdminService.ReloadCacheAsync(db);
    await scope.ServiceProvider.GetRequiredService<PerfService>().MigrateLegacyAsync();
    var enc = await PeopleHub.Infrastructure.Security.FieldEncryptionBackfill.RunAsync(db, PeopleHub.Infrastructure.Security.FieldCipher.Default);
    startupLog.LogInformation("Field encryption: enabled={Enabled} encryptedNow={Encrypted} plaintextRemaining={Remaining} keyId={KeyId}", enc.Enabled, enc.Encrypted, enc.Remaining, enc.KeyId);
}
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.Use(async (ctx, next) => {
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "same-origin";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    try { await next(); }
    catch (DbUpdateConcurrencyException) { ctx.Response.StatusCode = 409; await ctx.Response.WriteAsync("This record changed in another session. Reload before retrying."); }
});
app.UseStaticFiles();
app.UseRouting();
app.Use(async (ctx,next)=>{
    var metric=ctx.RequestServices.GetRequiredService<RequestMetrics>();
    var endpoint=ctx.GetEndpoint();var action=endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>();
    var route=action==null?"unmatched":$"{action.ControllerName}/{action.ActionName}";
    var started=metric.Begin(route);var status=500;
    var trace=System.Diagnostics.Activity.Current?.TraceId.ToString()??ctx.TraceIdentifier;
    ctx.Response.OnStarting(()=>{ctx.Response.Headers["X-Correlation-ID"]=trace;return Task.CompletedTask;});
    // Structured log scope: every log line written during the request carries the tenant and the trace id.
    using var logScope=app.Logger.BeginScope("tenant={TenantId} trace={Trace}",ctx.RequestServices.GetRequiredService<PeopleHub.Application.Common.Tenancy.ITenantContext>().TenantId,trace);
    try{await next();status=ctx.Response.StatusCode;}catch(DbUpdateConcurrencyException){status=409;throw;}finally{metric.End(route,started,status);app.Logger.LogInformation("HTTP {Route} status {Status} trace {Trace}",route,status,trace);}
});
app.UseRateLimiter();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
