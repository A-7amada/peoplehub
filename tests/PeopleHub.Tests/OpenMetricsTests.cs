using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PeopleHub.Application.Modules.Operations;
using PeopleHub.Web.Controllers;
namespace PeopleHub.Tests;
public class OpenMetricsTests
{
    static RequestMetrics Sample(){var m=new RequestMetrics();var t=m.Begin("Home/Index");m.End("Home/Index",t,200);var t2=m.Begin("Pay\"roll/Run");m.End("Pay\"roll/Run",t2,500);return m;}
    [Fact]public void OutputHasTypedFamiliesAndEofMarker(){var s=OpenMetricsFormatter.Render(Sample().Snapshot());Assert.Contains("# TYPE peoplehub_http_server_requests counter",s);Assert.Contains("peoplehub_http_server_requests_total{route=\"Home/Index\"} 1",s);Assert.Contains("peoplehub_http_server_errors_total{route=\"Pay\\\"roll/Run\"} 1",s);Assert.EndsWith("# EOF\n",s);}
    [Fact]public void EmptyMetricsStillValid(){var s=OpenMetricsFormatter.Render([]);Assert.EndsWith("# EOF\n",s);Assert.DoesNotContain("{route",s);}
    static MetricsController C(string? token,string? header){var cfg=new ConfigurationBuilder().AddInMemoryCollection(token==null?[]:new Dictionary<string,string?>{["METRICS_TOKEN"]=token}).Build();var c=new MetricsController(Sample(),cfg){ControllerContext=new ControllerContext{HttpContext=new Microsoft.AspNetCore.Http.DefaultHttpContext()}};if(header!=null)c.Request.Headers.Authorization=header;return c;}
    [Fact]public void DisabledWithoutTokenIsNotFound()=>Assert.IsType<NotFoundResult>(C(null,null).Scrape());
    [Fact]public void WrongOrMissingTokenIsUnauthorized(){Assert.IsType<UnauthorizedResult>(C("s3cret",null).Scrape());Assert.IsType<UnauthorizedResult>(C("s3cret","Bearer nope").Scrape());Assert.IsType<UnauthorizedResult>(C("s3cret","s3cret").Scrape());}
    [Fact]public void CorrectTokenReturnsOpenMetrics(){var r=Assert.IsType<ContentResult>(C("s3cret","Bearer s3cret").Scrape());Assert.Contains("openmetrics-text",r.ContentType);Assert.Contains("peoplehub_http_server_requests_total",r.Content);}
}
