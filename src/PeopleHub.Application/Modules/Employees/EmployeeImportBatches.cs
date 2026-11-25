using System.Security.Cryptography;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Employees;
public record ImportReconciliation(int Staged,int Linked,int Missing);
/// <summary>ENT-002: staged employee import with an explicit, all-or-nothing commit. Creates employees only: no accounts, salary, bank or identity data, no updates of existing people. Rollback of a committed batch TERMINATES the imported people (owner decision, Ahmed 2026-11-25 19:17); it never deletes them and their records stay.</summary>
public class EmployeeImportBatches(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit,EmployeeImportValidation validation)
{
 void Require(){if(!me.Can(Perm.EmpManage))throw new DomainException("err.forbidden");}
 /// <summary>Validates and stages a file. Any row with an issue rejects the whole file. The same file content returns its existing batch.</summary>
 public async Task<int> StageAsync(byte[] data)
 {
  int result=0;
  await repo.AtomicAsync(async()=>
  {
  Require();
  var preview=await validation.PreviewAsync(data);
  if(preview.Rows.Count==0||preview.Rows.Any(r=>r.Errors.Count>0||r.Cells==null))throw new DomainException("err.importInvalid");
  var hash=Convert.ToHexString(SHA256.HashData(data));
  var existing=await repo.FirstOrDefaultAsync(repo.Query<EmployeeImportBatch>().Where(x=>x.FileHash==hash));
  if(existing!=null){result=existing.Id;return;}
  var batch=new EmployeeImportBatch{FileHash=hash,RowCount=preview.Rows.Count,CreatedBy=me.Email,CreatedAt=clock.UtcNow};
  repo.Add(batch);await repo.SaveAsync();
  foreach(var r in preview.Rows)
  {
   var c=r.Cells!;
   repo.Add(new StagedEmployeeRow{BatchId=batch.Id,RowNumber=r.Row,Code=c[0],FullName=c[1],FullNameAr=c[2],Email=c[3].ToLowerInvariant(),HireDate=DateTime.ParseExact(c[4],"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture),DepartmentId=int.Parse(c[5]),BranchId=int.Parse(c[6]),JobTitleId=int.Parse(c[7])});
  }
  audit.Log("ImportStage","EmployeeImportBatch",batch.Id,$"{batch.RowCount} rows staged");
  await repo.SaveAsync();
  result=batch.Id;
  });
  return result;
 }
 public Task DiscardAsync(int batchId)=>repo.AtomicAsync(async()=>
 {
  Require();
  var b=await repo.FirstOrDefaultAsync(repo.Query<EmployeeImportBatch>().Where(x=>x.Id==batchId))??throw new DomainException("err.notFound");
  if(b.Status!="Staged")throw new DomainException("err.importNotStaged");
  foreach(var r in await repo.ToListAsync(repo.Query<StagedEmployeeRow>().Where(x=>x.BatchId==batchId)))repo.Remove(r);
  repo.Remove(b);
  audit.Log("ImportDiscard","EmployeeImportBatch",batchId,"staged batch discarded");
  await repo.SaveAsync();
 });
 /// <summary>Revalidates every staged row against the current database, then creates all employees or none.</summary>
 public Task CommitAsync(int batchId)=>repo.AtomicAsync(async()=>
 {
  Require();
  var b=await repo.FirstOrDefaultAsync(repo.Query<EmployeeImportBatch>().Where(x=>x.Id==batchId))??throw new DomainException("err.notFound");
  if(b.Status!="Staged")throw new DomainException("err.importNotStaged");
  var rows=await repo.ToListAsync(repo.Query<StagedEmployeeRow>().Where(x=>x.BatchId==batchId).OrderBy(x=>x.RowNumber));
  if(rows.Count==0||rows.Count!=b.RowCount)throw new DomainException("err.importStale");
  var existing=await repo.ToListAsync(repo.Query<Employee>().Select(x=>new{x.Code,x.Email}));
  var codes=existing.Select(x=>x.Code.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);var emails=existing.Select(x=>x.Email.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
  var departments=(await repo.ToListAsync(repo.Query<Department>().Select(x=>x.Id))).ToHashSet();var branches=(await repo.ToListAsync(repo.Query<Branch>().Select(x=>x.Id))).ToHashSet();var titles=(await repo.ToListAsync(repo.Query<JobTitle>().Select(x=>x.Id))).ToHashSet();
  foreach(var r in rows)if(codes.Contains(r.Code)||emails.Contains(r.Email)||!departments.Contains(r.DepartmentId)||!branches.Contains(r.BranchId)||!titles.Contains(r.JobTitleId))throw new DomainException("err.importStale");
  var created=new List<(StagedEmployeeRow row,Employee e)>();
  foreach(var r in rows)
  {
   var e=new Employee{Code=r.Code,FullName=r.FullName,FullNameAr=r.FullNameAr,Email=r.Email,HireDate=r.HireDate,DepartmentId=r.DepartmentId,BranchId=r.BranchId,JobTitleId=r.JobTitleId,Status=EmployeeStatus.Active};
   e.Validate(false);repo.Add(e);created.Add((r,e));
  }
  await repo.SaveAsync();
  foreach(var (r,e) in created)
  {
   r.EmployeeId=e.Id;
   repo.Add(EmployeeAssignment.Capture(e,e.HireDate.Date,me.Email,"Imported"));
   repo.Add(new StatusHistory{EmployeeId=e.Id,From=EmployeeStatus.Active,To=EmployeeStatus.Active,At=clock.UtcNow,ChangedBy=me.Email,Reason="Imported"});
  }
  b.Status="Committed";b.CommittedBy=me.Email;b.CommittedAt=clock.UtcNow;
  audit.Log("ImportCommit","EmployeeImportBatch",b.Id,$"{rows.Count} employees created");
  await repo.SaveAsync();
 });
 /// <summary>Owner decision 2026-11-25 19:17: rollback terminates imported people, never deletes them. Needs a reason. People already in a final status are left as they are and counted.</summary>
 public async Task<int> RollbackAsync(int batchId,string? reason)
 {
  int terminated=0;
  await repo.AtomicAsync(async()=>
  {
   Require();
   if(string.IsNullOrWhiteSpace(reason))throw new DomainException("err.reasonRequired");
   var b=await repo.FirstOrDefaultAsync(repo.Query<EmployeeImportBatch>().Where(x=>x.Id==batchId))??throw new DomainException("err.notFound");
   if(b.Status!="Committed")throw new DomainException("err.importNotCommitted");
   var ids=(await repo.ToListAsync(repo.Query<StagedEmployeeRow>().Where(x=>x.BatchId==batchId&&x.EmployeeId!=null).Select(x=>x.EmployeeId!.Value))).ToList();
   var people=await repo.ToListAsync(repo.Query<Employee>().Where(x=>ids.Contains(x.Id)));
   foreach(var e in people)
   {
    if(e.Status is EmployeeStatus.Terminated or EmployeeStatus.Resigned or EmployeeStatus.Retired)continue;
    var from=e.Status;
    repo.Add(e.ChangeStatus(EmployeeStatus.Terminated,me.Email,$"Import batch #{b.Id} rolled back: {reason.Trim()}",clock.UtcNow));
    e.LeaveRequestStamp=Guid.NewGuid().ToString("N");
    audit.Log("StatusChange","Employee",e.Code,$"{from} -> Terminated; import batch #{b.Id} rolled back");
    terminated++;
   }
   b.Status="RolledBack";b.RolledBackBy=me.Email;b.RolledBackAt=clock.UtcNow;b.RollbackReason=reason.Trim();
   audit.Log("ImportRollback","EmployeeImportBatch",b.Id,$"{terminated} employees terminated");
   await repo.SaveAsync();
  });
  return terminated;
 }
 public async Task<ImportReconciliation> ReconcileAsync(int batchId)
 {
  Require();
  var rows=await repo.ToListAsync(repo.Query<StagedEmployeeRow>().Where(x=>x.BatchId==batchId));
  var ids=rows.Where(x=>x.EmployeeId!=null).Select(x=>x.EmployeeId!.Value).ToList();
  var present=(await repo.ToListAsync(repo.Query<Employee>().Where(x=>ids.Contains(x.Id)).Select(x=>x.Id))).Count;
  return new(rows.Count,present,ids.Count-present);
 }
 public Task<EmployeeImportBatch?> GetAsync(int id){Require();return repo.FirstOrDefaultAsync(repo.Query<EmployeeImportBatch>().Where(x=>x.Id==id));}
 public async Task<List<EmployeeImportBatch>> ListAsync(){Require();return await repo.ToListAsync(repo.Query<EmployeeImportBatch>().OrderByDescending(x=>x.Id).Take(50));}
}
