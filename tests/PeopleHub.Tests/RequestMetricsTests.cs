using PeopleHub.Application.Services;
namespace PeopleHub.Tests;
public class RequestMetricsTests
{
 [Fact]public void CountsResponsesErrorsAndInFlightWithoutIndividualRequests(){var s=new RequestMetrics();var a=s.Begin("Employees/Profile");var b=s.Begin("Employees/Profile");Assert.Equal(2,Assert.Single(s.Snapshot()).InFlight);s.End("Employees/Profile",a,200);s.End("Employees/Profile",b,503);var r=Assert.Single(s.Snapshot());Assert.Equal(2,r.Requests);Assert.Equal(1,r.Errors);Assert.Equal(0,r.InFlight);Assert.True(r.MaxMilliseconds>=r.MeanMilliseconds-0.01);}
 [Fact]public void ClientRejectionsAreNotServerErrorsAndSnapshotsAreCopies(){var s=new RequestMetrics();s.End("unknown",0,500);var t=s.Begin("Leave/Request");s.End("Leave/Request",t,409);var copy=s.Snapshot();copy.Clear();Assert.Single(s.Snapshot());Assert.Equal(0,s.Snapshot()[0].Errors);}
 [Fact]public void ParallelCountersRemainConsistent(){var s=new RequestMetrics();Parallel.For(0,1000,i=>{var t=s.Begin("Health/Ready");s.End("Health/Ready",t,i%10==0?503:200);});var r=Assert.Single(s.Snapshot());Assert.Equal(1000,r.Requests);Assert.Equal(100,r.Errors);Assert.Equal(0,r.InFlight);}
}
