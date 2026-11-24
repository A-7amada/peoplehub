using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
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
builder.Services.AddScoped<IHrRepository>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<LeaveService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddSingleton<Loc>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews(o => o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Login"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax; });
builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    var cultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
    o.DefaultRequestCulture = new RequestCulture("en");
    o.SupportedCultures = cultures; o.SupportedUICultures = cultures;
    o.RequestCultureProviders = new List<IRequestCultureProvider> { new CookieRequestCultureProvider() };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hrPass = app.Environment.IsDevelopment() ? "Admin123!" : app.Configuration["HR_SEED_PASSWORD"];
    await SeedData.InitializeAsync(db, hrPass, "Demo123!");
}

if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
