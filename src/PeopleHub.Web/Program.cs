using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using PeopleHub.Domain.Entities;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Services;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, WebCurrentUser>();
builder.Services.AddScoped<AccessScope>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<OrgService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<EmployeeLifecycle>();
builder.Services.AddScoped<AssetService>();
builder.Services.AddScoped<EmployeeDocuments>();
builder.Services.AddScoped<ShiftSchedule>();
builder.Services.AddScoped<EmploymentTimeline>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<LeaveService>();
builder.Services.AddScoped<ApprovalService>();
builder.Services.AddScoped<PayrollService>();
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
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.Configure<ForwardedHeadersOptions>(o => { o.ForwardedHeaders = ForwardedHeaders.XForwardedProto; });
builder.Services.AddSingleton<Loc>();
builder.Services.AddControllersWithViews(o => o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Denied"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax; o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        o.Events.OnValidatePrincipal = async c => {
            var id = c.Principal?.FindFirst("uid")?.Value;
            var db = c.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var u = int.TryParse(id, out var uid) ? await db.Set<AppUser>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid && u.IsActive) : null;
            if (u == null || c.Principal?.FindFirst("stamp")?.Value != u.SecurityStamp) c.RejectPrincipal();
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
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var hrPass = app.Environment.IsDevelopment() ? "Admin123!" : app.Configuration["HR_SEED_PASSWORD"];
    await SeedData.InitializeAsync(db, hrPass, "Demo123!", scope.ServiceProvider.GetRequiredService<AppClock>().Today);
    await scope.ServiceProvider.GetRequiredService<SettingsService>().LoadAsync();
    await AdminService.ReloadCacheAsync(db);
    await scope.ServiceProvider.GetRequiredService<PerfService>().MigrateLegacyAsync();
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
app.UseRateLimiter();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
