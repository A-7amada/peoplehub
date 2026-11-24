using System.Security.Claims;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;

namespace PeopleHub.Web.Services;

public class WebCurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private ClaimsPrincipal? P => http.HttpContext?.User;
    public bool IsAuthenticated => P?.Identity?.IsAuthenticated == true;
    public string Email => P?.FindFirstValue(ClaimTypes.Email) ?? "";
    public int? EmployeeId => int.TryParse(P?.FindFirstValue("emp"), out var i) ? i : null;
    public int UserId => int.TryParse(P?.FindFirstValue("uid"), out var i) ? i : 0;
    public bool IsInRole(string role) => P?.IsInRole(role) == true;
    public bool Can(string permission) => P?.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value) is { } r && PermissionCache.Has(r, permission);
}

/// <summary>Page-level permission check. Permissions come from the roles an admin configures.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermAttribute(string permission) : Attribute, Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter
{
    public void OnAuthorization(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext ctx)
    {
        var roles = ctx.HttpContext.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        if (ctx.HttpContext.User.Identity?.IsAuthenticated != true || PermissionCache.Has(roles, permission)) return;
        if (ctx.HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory)) is Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory f)
            f.GetTempData(ctx.HttpContext)["err"] = ctx.HttpContext.RequestServices.GetRequiredService<Loc>()["err.forbidden"];
        ctx.Result = new Microsoft.AspNetCore.Mvc.RedirectResult("/");
    }
}
