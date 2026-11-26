using PeopleHub.Application.Modules.Platform;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
/// <summary>US-PH2-018: outbox delivery states shown to administrators. Sent means accepted by the provider.</summary>
public class MailDeliveryStatusTests
{
 [Theory]
 [InlineData(false,0,null,"Queued")]
 [InlineData(false,2,"Delivery failed; retry pending (maximum 5 attempts)","Retrying")]
 [InlineData(false,5,"Delivery failed; retry pending (maximum 5 attempts)","Failed")]
 [InlineData(false,0,"Blocked: recipient is not on the explicit delivery allowlist","Blocked")]
 [InlineData(true,1,"Accepted by provider, not proof of inbox delivery","Sent")]
 public void StatusFollowsAttemptsNotesAndSent(bool sent,int attempts,string? note,string expected)
  =>Assert.Equal(expected,EmailService.StatusOf(new EmailOutbox{Sent=sent,Attempts=attempts,Note=note}));
 [Fact]public async Task SummaryCountsEveryRowOnceAndRequeueOnlyTouchesFailedRows()
 {
  var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();
  d.AddRange(new EmailOutbox{To="a@x.test",Attempts=0},new EmailOutbox{To="b@x.test",Attempts=5},new EmailOutbox{To="c@x.test",Sent=true,Attempts=1},new EmailOutbox{To="d@x.test",Attempts=2});await d.SaveChangesAsync();
  var svc=new EmailService(d,new AppClock(TimeProvider.System));
  var before=await svc.DeliverySummaryAsync();var baseline=before.Values.Sum();
  Assert.Equal(1,before["Failed"]);Assert.Equal(1,before["Sent"]);Assert.Equal(1,before["Retrying"]);
  Assert.Equal(1,await svc.RequeueFailedAsync());
  var after=await svc.DeliverySummaryAsync();
  Assert.Equal(0,after["Failed"]);Assert.Equal(baseline,after.Values.Sum());Assert.Equal(1,after["Sent"]);Assert.Equal(1,after["Retrying"]);
 }
}
