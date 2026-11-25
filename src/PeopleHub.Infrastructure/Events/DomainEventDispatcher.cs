using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeopleHub.Application.Common.Events;
namespace PeopleHub.Infrastructure.Events;

/// <summary>In-process Mediator for domain events: resolves every IDomainEventHandler for the runtime event type.
/// Handler failures are logged and isolated so one subscriber cannot break the save that already committed.</summary>
public sealed class DomainEventDispatcher(IServiceProvider sp, ILogger<DomainEventDispatcher> log) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var e in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(e.GetType());
            foreach (var h in sp.GetServices(handlerType))
            {
                try { await (Task)handlerType.GetMethod("HandleAsync")!.Invoke(h, [e, ct])!; }
                catch (Exception ex) { log.LogError(ex, "Domain event handler {Handler} failed for {Event}", h!.GetType().Name, e.GetType().Name); }
            }
        }
    }
}

/// <summary>First subscriber: structured log line for status changes (also the hook other modules attach to).</summary>
public sealed class EmployeeStatusChangedLogger(ILogger<EmployeeStatusChangedLogger> log) : IDomainEventHandler<EmployeeStatusChanged>
{
    public Task HandleAsync(EmployeeStatusChanged e, CancellationToken ct)
    {
        log.LogInformation("Employee {EmployeeId} status {From} -> {To} (tenant {TenantId})", e.EmployeeId, e.From, e.To, e.TenantId);
        return Task.CompletedTask;
    }
}
