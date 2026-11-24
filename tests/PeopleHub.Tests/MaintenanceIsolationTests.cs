using Microsoft.Extensions.DependencyInjection;
using PeopleHub.Web.Services;
namespace PeopleHub.Tests;
public class MaintenanceIsolationTests
{
 class ScopeFactory:IServiceScopeFactory
 {
  public List<Scope> Scopes=[];
  public IServiceScope CreateScope(){var s=new Scope();Scopes.Add(s);return s;}
 }
 class Scope:IServiceScope,IServiceProvider{public bool Disposed;public IServiceProvider ServiceProvider=>this;public object? GetService(Type t)=>null;public void Dispose()=>Disposed=true;}
 [Fact]public async Task FailureDoesNotSkipOtherJobsAndEachScopeIsDisposed(){var f=new ScopeFactory();var ran=new List<string>();var failures=new List<string>();IServiceProvider? first=null,second=null;await PerfWorker.RunJobsAsync(f,[("fail",sp=>{first=sp;ran.Add("fail");throw new InvalidOperationException("Synthetic failure");}),("ok",sp=>{second=sp;ran.Add("ok");return Task.CompletedTask;})],(name,ex)=>failures.Add(name),CancellationToken.None);Assert.Equal(new[]{"fail","ok"},ran);Assert.Equal(new[]{"fail"},failures);Assert.NotSame(first,second);Assert.Equal(2,f.Scopes.Count);Assert.All(f.Scopes,x=>Assert.True(x.Disposed));}
 [Fact]public async Task CancellationBeforeRunDoesNoWork(){var f=new ScopeFactory();using var c=new CancellationTokenSource();c.Cancel();await PerfWorker.RunJobsAsync(f,[("skip",sp=>throw new Exception())],(_,_)=>throw new Exception(),c.Token);Assert.Empty(f.Scopes);}
 [Fact]public async Task ShutdownCancellationStopsRemainingJobsWithoutFailure(){var f=new ScopeFactory();using var c=new CancellationTokenSource();bool next=false,failed=false;await PerfWorker.RunJobsAsync(f,[("stop",sp=>{c.Cancel();throw new OperationCanceledException(c.Token);}),("skip",sp=>{next=true;return Task.CompletedTask;})],(_,_)=>failed=true,c.Token);Assert.False(next);Assert.False(failed);Assert.True(Assert.Single(f.Scopes).Disposed);}
 [Fact]public async Task UnrelatedCancellationIsJobFailureButNextJobStillRuns(){var f=new ScopeFactory();bool next=false;int failed=0;await PerfWorker.RunJobsAsync(f,[("cancel",sp=>throw new OperationCanceledException()),("next",sp=>{next=true;return Task.CompletedTask;})],(_,_)=>failed++,CancellationToken.None);Assert.True(next);Assert.Equal(1,failed);}
}
