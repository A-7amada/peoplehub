using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleHub.Application.Services;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Web.Controllers;
using PeopleHub.Web.Services;
namespace PeopleHub.Tests;
public class FinancialStepUpTests
{
 class User:ICurrentUser{public string Email=>"hr@peoplehub.local";public int? EmployeeId=>1;public int UserId{get;set;} public bool IsAuthenticated=>true;public bool IsInRole(string r)=>true;public bool Can(string p)=>true;}
 [Theory]
 [InlineData(typeof(RequestsController),"Reassign")]
 [InlineData(typeof(RequestsController),"Settle")]
 [InlineData(typeof(PayrollController),"Recalc")]
 [InlineData(typeof(PayrollController),"Adjust")]
 [InlineData(typeof(PayrollController),"FinanceApprove")]
 [InlineData(typeof(PayrollController),"Paid")]
 [InlineData(typeof(PayrollAdjustmentsController),"Propose")]
 [InlineData(typeof(PayrollAdjustmentsController),"Decide")]
 [InlineData(typeof(PayrollAdjustmentsController),"Cancel")]
 [InlineData(typeof(PayrollAdjustmentsController),"Apply")]
 public void FinancialMutationRequiresFreshSession(Type controller,string method)=>Assert.NotNull(controller.GetMethod(method)!.GetCustomAttribute<RequireStepUpAttribute>());
 [Theory]
 [InlineData("/Requests/Settle","/Requests/Advances")]
 [InlineData("/Requests/Reassign","/Requests")]
 [InlineData("/PayrollAdjustments/Apply","/Payroll")]
 [InlineData("/Payroll/Adjust","/Payroll")]
 public async Task MissingElevationRejectsBeforeActionAndUsesSafeReturn(string path,string target)
 {
  await using var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);
  var u=new User();var c=new AppClock(TimeProvider.System);var s=new SessionService(d,u,c,new AuditService(d,u,c));
  var services=new ServiceCollection().AddSingleton(s).BuildServiceProvider();
  var http=new DefaultHttpContext{RequestServices=services};http.Request.Path=path;
  var context=new ActionExecutingContext(new ActionContext(http,new RouteData(),new ActionDescriptor()),new List<IFilterMetadata>(),new Dictionary<string,object?>(),new object());
  bool invoked=false;
  await new RequireStepUpAttribute().OnActionExecutionAsync(context,()=>{invoked=true;return Task.FromResult(new ActionExecutedContext(context,new List<IFilterMetadata>(),new object()));});
  Assert.False(invoked);var result=Assert.IsType<RedirectToActionResult>(context.Result);Assert.Equal("StepUp",result.ActionName);Assert.Equal("Account",result.ControllerName);Assert.Equal(target,result.RouteValues!["returnUrl"]);
 }
}
