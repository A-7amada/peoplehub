using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Platform;
namespace PeopleHub.Application.Modules.Talent;
public record CandidateFileInfo(int Id,int CandidateId,string Token,string Kind,string FileName,long Size,string Actor,DateTime At);
/// <summary>US-PH2-014 candidate CV and document upload. Access-controlled: only TalentManage holders can upload, list or download. Every upload and download is audited without the file name or contents. PDF, JPG and PNG up to 2 MB (illustrative limits, owner can change).</summary>
public class RecruitmentFiles(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 public static readonly string[] Kinds=["CV","Document"];
 public const int MaxPerCandidate=10;
 private void Need(){if(!me.Can(Perm.TalentManage))throw new DomainException("err.forbidden");}

 public async Task<List<CandidateFileInfo>> ListAsync()
 {
  Need();
  var rows=await repo.ToListAsync(repo.Query<CandidateFile>().OrderByDescending(x=>x.At).Select(x=>new{x.Id,x.CandidateId,x.Token,x.Kind,x.FileName,Size=x.Data.Length,x.Actor,x.At}));
  return rows.Select(x=>new CandidateFileInfo(x.Id,x.CandidateId,x.Token,x.Kind,x.FileName,x.Size,x.Actor,x.At)).ToList();
 }

 public Task UploadAsync(int candidateId,string kind,string fileName,byte[] data)=>repo.AtomicAsync(async()=>
 {
  Need();
  if(!Kinds.Contains(kind))throw new DomainException("err.required");
  if(!await repo.AnyAsync(repo.Query<Candidate>().Where(x=>x.Id==candidateId)))throw new DomainException("err.notFound");
  var (name,ct)=AttachmentService.Validate(fileName,data);
  if(await repo.CountAsync(repo.Query<CandidateFile>().Where(x=>x.CandidateId==candidateId))>=MaxPerCandidate)throw new DomainException("err.required");
  repo.Add(new CandidateFile{CandidateId=candidateId,Kind=kind,FileName=name,ContentType=ct,Data=data,Sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data)),Actor=me.Email,At=clock.UtcNow});
  audit.Log("Upload","CandidateFile",candidateId.ToString(),$"Candidate {kind} stored; file name and contents withheld. Format and size checked only, no malware scan");
  await repo.SaveAsync();
 });

 public Task DeleteAsync(int id)=>repo.AtomicAsync(async()=>
 {
  Need();
  var f=await repo.FirstOrDefaultAsync(repo.Query<CandidateFile>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");
  repo.Remove(f);audit.Log("Delete","CandidateFile",f.CandidateId.ToString(),"Candidate file removed");
  await repo.SaveAsync();
 });

 public async Task<CandidateFile> DownloadAsync(string token)
 {
  Need();
  var f=await repo.FirstOrDefaultAsync(repo.Query<CandidateFile>().Where(x=>x.Token==token))??throw new DomainException("err.notFound");
  audit.Log("Download","CandidateFile",f.CandidateId.ToString(),"Candidate file downloaded");
  await repo.SaveAsync();
  return f;
 }
}
