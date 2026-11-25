using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using PeopleHub.Domain.Entities;
using PeopleHub.Web.Controllers;
using PeopleHub.Web.Services;
namespace PeopleHub.Tests;
public class ControllerBindingTests
{
 class Flash : ITempDataProvider
 {
  public IDictionary<string,object> LoadTempData(HttpContext context)=>new Dictionary<string,object>();
  public void SaveTempData(HttpContext context,IDictionary<string,object> values){}
 }
 class Harness():BaseController(new Loc())
 {
  public bool Ran;
  public Task<IActionResult> Run()=>Do(()=>{Ran=true;return Task.CompletedTask;},"msg.saved",()=>new RedirectResult("/"));
 }
 static Harness Controller(){var c=new Harness();var http=new DefaultHttpContext();c.ControllerContext=new ControllerContext{HttpContext=http};c.TempData=new TempDataDictionary(http,new Flash());return c;}
 [Theory][InlineData("amount")][InlineData("effectiveOn")][InlineData("id")]
 public async Task BindingErrorCannotReachMutationOrShowSuccess(string field){var c=Controller();c.ModelState.AddModelError(field,"Invalid input");Assert.IsType<RedirectResult>(await c.Run());Assert.False(c.Ran);Assert.True(c.TempData.ContainsKey("err"));Assert.False(c.TempData.ContainsKey("ok"));}
 [Fact]public async Task ValidBindingStillRunsAndShowsSuccess(){var c=Controller();await c.Run();Assert.True(c.Ran);Assert.True(c.TempData.ContainsKey("ok"));Assert.False(c.TempData.ContainsKey("err"));}
}
