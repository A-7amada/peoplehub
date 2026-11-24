using System.Security.Claims;
using PeopleHub.Application.Services;

namespace PeopleHub.Web.Services;

public class WebCurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private ClaimsPrincipal? P => http.HttpContext?.User;
    public bool IsAuthenticated => P?.Identity?.IsAuthenticated == true;
    public string Email => P?.FindFirstValue(ClaimTypes.Email) ?? "";
    public int? EmployeeId => int.TryParse(P?.FindFirstValue("emp"), out var i) ? i : null;
    public int UserId => int.TryParse(P?.FindFirstValue("uid"), out var i) ? i : 0;
    public bool IsInRole(string role) => P?.IsInRole(role) == true;
}
