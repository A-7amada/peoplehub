using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record PendingKind(string Kind,int Pending,int OldestAgeDays);
public record OperationsSnapshot(DateTime AsOf,List<PendingKind> Approvals,OutboxReconciliation Outbox,int UnreadNotifications);
/// <summary>ARC-009: read-only aggregate projection for operators. Counts only: no employee IDs, names, request contents or payloads. Computed on demand from the database, not stored.</summary>
public class OperationsProjection(IRepository repo,AppClock clock,IntegrationOutbox outbox)
{
 async Task<PendingKind> Kind<T>(string kind) where T:Approvable
 {
  var created=await repo.ToListAsync(repo.Query<T>().Where(x=>x.Status==RequestStatus.Pending).Select(x=>x.CreatedAt));
  var oldest=created.Count==0?0:Math.Max(0,(int)(clock.UtcNow-created.Min()).TotalDays);
  return new(kind,created.Count,oldest);
 }
 public async Task<OperationsSnapshot> SnapshotAsync()
 {
  var kinds=new List<PendingKind>{await Kind<LeaveRequest>("Leave"),await Kind<OvertimeRequest>("Overtime"),await Kind<AttendanceCorrection>("Correction"),await Kind<AdvanceRequest>("Advance")};
  var unread=await repo.CountAsync(repo.Query<Notification>().Where(x=>!x.IsRead));
  return new(clock.UtcNow,kinds,await outbox.ReconcileAsync(),unread);
 }
}
