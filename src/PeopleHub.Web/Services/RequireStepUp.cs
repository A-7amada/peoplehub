using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PeopleHub.Application.Services;
namespace PeopleHub.Web.Services;
/// <summary>Rejects sensitive operations rather than replaying an unconfirmed POST.</summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequireStepUpAttribute:Attribute,IAsyncActionFilter
{
 public async Task OnActionExecutionAsync(ActionExecutingContext ctx,ActionExecutionDelegate next)
 {
  var sessions=ctx.HttpContext.RequestServices.GetRequiredService<SessionService>();if(await sessions.ElevatedAsync(ctx.HttpContext.User.FindFirst("sid")?.Value)){await next();return;}
  var action=ctx.ActionDescriptor.RouteValues.TryGetValue("action",out var name)?name??"":"";var target=ctx.HttpContext.Request.Path.StartsWithSegments("/Admin")?(action is "ReminderPolicy" or "ApprovalComment" or "AddStep" or "RemoveStep" or "MoveStep"?"/Admin/Workflows":action is "SavePermissions" or "AddRole" or "DeleteRole"?"/Admin/Roles":action is "SaveUser" or "CreateUser"?"/Admin/Users":action=="ExportDatabase"?"/Admin/Operations":"/Admin/Settings"):ctx.HttpContext.Request.Path.StartsWithSegments("/SalaryStructures")?"/SalaryStructures":ctx.HttpContext.Request.Path.StartsWithSegments("/Payroll")||ctx.HttpContext.Request.Path.StartsWithSegments("/BankReconciliation")?"/Payroll":"/Account/Security";
  ctx.Result=new RedirectToActionResult("StepUp","Account",new{returnUrl=target});
 }
}
