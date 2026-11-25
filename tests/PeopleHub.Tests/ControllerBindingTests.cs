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
  public static DateTime? Clock(string? value)=>ParseClock(new(2026,11,25),value);
  public bool Ran;
  public Task<IActionResult> Run()=>Do(()=>{Ran=true;return Task.CompletedTask;},"msg.saved",()=>new RedirectResult("/"));
 }
 static Harness Controller(){var c=new Harness();var http=new DefaultHttpContext();c.ControllerContext=new ControllerContext{HttpContext=http};c.TempData=new TempDataDictionary(http,new Flash());return c;}
 [Theory][InlineData("amount")][InlineData("effectiveOn")][InlineData("id")]
 public async Task BindingErrorCannotReachMutationOrShowSuccess(string field){var c=Controller();c.ModelState.AddModelError(field,"Invalid input");Assert.IsType<RedirectResult>(await c.Run());Assert.False(c.Ran);Assert.True(c.TempData.ContainsKey("err"));Assert.False(c.TempData.ContainsKey("ok"));}
 [Fact]public async Task ValidBindingStillRunsAndShowsSuccess(){var c=Controller();await c.Run();Assert.True(c.Ran);Assert.True(c.TempData.ContainsKey("ok"));Assert.False(c.TempData.ContainsKey("err"));}
 [Theory][InlineData("bad")][InlineData("25:00")][InlineData("-01:00")][InlineData("1.09:00:00")][InlineData("09:60")]
 public void InvalidClockCannotBecomeMissingPunch(string value)=>Assert.Throws<DomainException>(()=>Harness.Clock(value));
 [Theory][InlineData("09:05",9,5)][InlineData("9:05",9,5)][InlineData("23:59:00",23,59)]
 public void ValidClockRetainsDateAndTime(string value,int hour,int minute)=>Assert.Equal(new DateTime(2026,11,25,hour,minute,0),Harness.Clock(value));
 [Theory][InlineData(null)][InlineData("")][InlineData(" ")]
 public void ExplicitlyEmptyClockRemainsMissing(string? value)=>Assert.Null(Harness.Clock(value));
}
