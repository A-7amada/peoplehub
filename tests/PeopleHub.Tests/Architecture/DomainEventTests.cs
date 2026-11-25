using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common.Events;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests.Architecture;

public class DomainEventTests
{
    sealed class Capture : IDomainEventDispatcher
    {
        public List<IDomainEvent> Seen = new();
        public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> e, CancellationToken ct = default) { Seen.AddRange(e); return Task.CompletedTask; }
    }
    static async Task<(AppDbContext db, Capture cap)> Make()
    {
        var cap = new Capture();
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options, null, cap);
        await db.Database.OpenConnectionAsync(); await db.Database.MigrateAsync();
        await PeopleHub.Infrastructure.Persistence.SeedData.InitializeAsync(db, "Admin123!", "Demo123!", new DateTime(2026, 11, 24));
        cap.Seen.Clear(); return (db, cap);
    }
    [Fact] public async Task StatusChangeRaisesEventAfterSaveOnly()
    {
        var (db, cap) = await Make();
        var emp = await db.Set<Employee>().FirstAsync();
        var from = emp.Status; var to = from == EmployeeStatus.Suspended ? EmployeeStatus.Active : EmployeeStatus.Suspended;
        emp.Status = to; Assert.Empty(cap.Seen);
        await db.SaveChangesAsync();
        var ev = Assert.IsType<EmployeeStatusChanged>(Assert.Single(cap.Seen));
        Assert.Equal((emp.Id, from, to, 1), (ev.EmployeeId, ev.From, ev.To, ev.TenantId));
    }
    [Fact] public async Task UnrelatedChangeRaisesNothing()
    {
        var (db, cap) = await Make();
        var emp = await db.Set<Employee>().FirstAsync(); emp.MaritalStatus = emp.MaritalStatus + "x"; await db.SaveChangesAsync();
        Assert.Empty(cap.Seen);
    }
}
