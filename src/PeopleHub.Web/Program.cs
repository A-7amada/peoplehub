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
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
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
builder.Services.AddDataProtection();
builder.Services.AddSingleton<Loc>();
builder.Services.AddControllersWithViews(o => o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Denied"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax; o.ExpireTimeSpan = TimeSpan.FromHours(8); });
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
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hrPass = app.Environment.IsDevelopment() ? "Admin123!" : app.Configuration["HR_SEED_PASSWORD"];
    await SeedData.InitializeAsync(db, hrPass, "Demo123!", scope.ServiceProvider.GetRequiredService<AppClock>().Today);
    await scope.ServiceProvider.GetRequiredService<SettingsService>().LoadAsync();
    await AdminService.ReloadCacheAsync(db);
    await scope.ServiceProvider.GetRequiredService<PerfService>().MigrateLegacyAsync();
}
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
