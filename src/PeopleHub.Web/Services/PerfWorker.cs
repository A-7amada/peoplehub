using PeopleHub.Application.Services;
namespace PeopleHub.Web.Services;
public class PerfWorker(IServiceScopeFactory scopes, ILogger<PerfWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<EmployeeDocuments>().AlertAsync(); await scope.ServiceProvider.GetRequiredService<EmploymentTimeline>().ApplyDueAsync(); await scope.ServiceProvider.GetRequiredService<PerfService>().MaintainAsync(); }
            catch (Exception ex) { log.LogWarning(ex, "Performance maintenance deferred"); }
            await Task.Delay(TimeSpan.FromMinutes(15), stop);
        }
    }
}
