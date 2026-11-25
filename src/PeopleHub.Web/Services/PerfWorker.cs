using PeopleHub.Application.Services;
namespace PeopleHub.Web.Services;
public class PerfWorker(IServiceScopeFactory scopes, ILogger<PerfWorker> log) : BackgroundService
{
    public static async Task RunJobsAsync(IServiceScopeFactory scopes, IEnumerable<(string Name,Func<IServiceProvider,Task> Run)> jobs, Action<string,Exception> failed,CancellationToken stop)
    {
        foreach(var job in jobs)
        {
            if(stop.IsCancellationRequested)return;
            try
            {
                // A failed job must not leak tracked changes into another job.
                using var scope=scopes.CreateScope();
                await job.Run(scope.ServiceProvider);
            }
            catch(OperationCanceledException) when(stop.IsCancellationRequested){return;}
            catch(Exception ex){failed(job.Name,ex);}
        }
    }
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        (string Name,Func<IServiceProvider,Task> Run)[] jobs=[
            ("DocumentExpiry",JobTracker.Track(scopes,"DocumentExpiry",sp=>sp.GetRequiredService<EmployeeDocuments>().AlertAsync())),
            ("TrainingExpiry",JobTracker.Track(scopes,"TrainingExpiry",sp=>sp.GetRequiredService<TrainingExpiry>().AlertAsync())),
            ("ApprovalReminders",JobTracker.Track(scopes,"ApprovalReminders",sp=>sp.GetRequiredService<ApprovalReminders>().RunAsync())),
            ("EmploymentSchedule",JobTracker.Track(scopes,"EmploymentSchedule",sp=>sp.GetRequiredService<EmploymentTimeline>().ApplyDueAsync())),
            ("PerformanceMaintenance",JobTracker.Track(scopes,"PerformanceMaintenance",sp=>sp.GetRequiredService<PerfService>().MaintainAsync()))];
        while (!stop.IsCancellationRequested)
        {
            await RunJobsAsync(scopes,jobs,(name,ex)=>log.LogWarning(ex,"Maintenance job {Job} deferred",name),stop);
            try{await Task.Delay(TimeSpan.FromMinutes(15),stop);}
            catch(OperationCanceledException) when(stop.IsCancellationRequested){break;}
        }
    }
}
