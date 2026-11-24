using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record TrainingEvidenceView(List<TrainingCertificate> Certificates,List<TrainingOutcome> Outcomes);
public class TrainingEvidence(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit,AttachmentService attachments)
{
 private async Task<TrainingEnrollment> Enrollment(int id)=>await repo.FirstOrDefaultAsync(repo.Query<TrainingEnrollment>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");
 public async Task<TrainingEvidenceView> GetAsync()
 {
  bool hr=me.Can(Perm.TalentManage);var ids=(await repo.ToListAsync(repo.Query<TrainingEnrollment>())).Where(x=>hr||x.EmployeeId==me.EmployeeId).Select(x=>x.Id).ToList();
  return new(await repo.ToListAsync(repo.Query<TrainingCertificate>().Where(x=>ids.Contains(x.EnrollmentId)).OrderByDescending(x=>x.IssuedOn)),await repo.ToListAsync(repo.Query<TrainingOutcome>().Where(x=>ids.Contains(x.EnrollmentId)).OrderByDescending(x=>x.At)));
 }
 public Task CertificateAsync(int enrollmentId,string issuer,string reference,DateTime issuedOn,DateTime? expiresOn,string fileName,byte[] data)=>repo.AtomicAsync(async()=>
 {
  if(!me.Can(Perm.TalentManage))throw new DomainException("err.forbidden");var e=await Enrollment(enrollmentId);
  if(!e.Completed)throw new DomainException("err.forbidden");
  if(string.IsNullOrWhiteSpace(issuer)||issuer.Length>200||string.IsNullOrWhiteSpace(reference)||reference.Length>150||issuedOn.Year<2000||issuedOn.Date>clock.Today||expiresOn is {} exp&&(exp.Date<=issuedOn.Date||exp.Year>9998))throw new DomainException("err.required");
  var file=attachments.Save(fileName,data);if(file.Token==null)throw new DomainException("err.required");await repo.SaveAsync();var a=(await repo.FirstOrDefaultAsync(repo.Query<Attachment>().Where(x=>x.Token==file.Token)))!;a.OwnerEmployeeId=e.EmployeeId;
  repo.Add(new TrainingCertificate{EnrollmentId=e.Id,EmployeeId=e.EmployeeId,Issuer=issuer.Trim(),Reference=reference.Trim(),IssuedOn=issuedOn.Date,ExpiresOn=expiresOn?.Date,AttachmentToken=file.Token,Actor=me.Email,At=clock.UtcNow});audit.Log("Certificate","Training",e.Id,"Certificate evidence recorded; authenticity not independently verified");await repo.SaveAsync();
 });
 public Task OutcomeAsync(int enrollmentId,string kind,string note)=>repo.AtomicAsync(async()=>
 {
  var e=await Enrollment(enrollmentId);if(!e.Completed)throw new DomainException("err.forbidden");
  if(kind=="Participant"){if(e.EmployeeId!=me.EmployeeId)throw new DomainException("err.forbidden");}
  else if(kind=="Effectiveness"){if(!me.Can(Perm.TalentManage)||e.EmployeeId==me.EmployeeId)throw new DomainException("err.forbidden");}
  else throw new DomainException("err.required");
  if(string.IsNullOrWhiteSpace(note)||note.Length>2000)throw new DomainException("err.required");repo.Add(new TrainingOutcome{EnrollmentId=e.Id,EmployeeId=e.EmployeeId,Kind=kind,Note=note.Trim(),Actor=me.Email,At=clock.UtcNow});audit.Log("Outcome","Training",e.Id,kind);await repo.SaveAsync();
 });
}
