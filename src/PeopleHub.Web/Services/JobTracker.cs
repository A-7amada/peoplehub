using PeopleHub.Application.Modules.Operations;
namespace PeopleHub.Web.Services;
/// <summary>ARC-004: wraps a maintenance job with a persisted lease and run record. Claim and completion use their own scopes so a failed job cannot leak tracked changes into the record.</summary>
public static class JobTracker
{
 public static async Task RunAsync(IServiceScopeFactory scopes,string name,TimeSpan lease,Func<Task> run)
 {
  using(var scope=scopes.CreateScope()){if(!await scope.ServiceProvider.GetRequiredService<ScheduledJobs>().TryClaimAsync(name,lease))return;}
  Exception? error=null;
  try{await run();}catch(Exception ex){error=ex;}
  try{using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<ScheduledJobs>().CompleteAsync(name,error);}catch{}
  if(error!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
 }
 public static Func<IServiceProvider,Task> Track(IServiceScopeFactory scopes,string name,Func<IServiceProvider,Task> run)=>sp=>RunAsync(scopes,name,TimeSpan.FromMinutes(10),()=>run(sp));
}
