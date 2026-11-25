using PeopleHub.Domain.Entities;
namespace PeopleHub.Application.Common.Events;

/// <summary>Domain event (ADR-0006). A fact that already happened inside one module, published so other modules can react
/// without referencing it. Events are raised after a successful save, in process, within the same tenant scope.</summary>
public interface IDomainEvent { int TenantId { get; } }

/// <summary>Observer pattern: any number of handlers per event type, registered in DI. Handlers must not throw for business reasons;
/// a failing handler is logged and does not undo the committed change.</summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent { Task HandleAsync(TEvent e, CancellationToken ct); }

public interface IDomainEventDispatcher { Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default); }

/// <summary>Raised when an employee's employment status changes (hire, suspension, termination, rehire).</summary>
public sealed record EmployeeStatusChanged(int TenantId, int EmployeeId, EmployeeStatus From, EmployeeStatus To) : IDomainEvent;
