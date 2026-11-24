using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record RecruitmentOfferView(List<CandidateOffer> Offers,List<CandidateOfferEvent> Events);
public class RecruitmentOffers(IRepository repo,ICurrentUser me,AppClock clock,AuditService audit)
{
 private void Need(){if(!me.Can(Perm.TalentManage))throw new DomainException("err.forbidden");}
 public async Task<RecruitmentOfferView> GetAsync(){Need();return new(await repo.ToListAsync(repo.Query<CandidateOffer>().OrderByDescending(x=>x.Id)),await repo.ToListAsync(repo.Query<CandidateOfferEvent>().OrderBy(x=>x.Id)));}
 public Task ProposeAsync(int candidateId,string roleTitle,decimal monthlyBase,string currency,DateTime startOn,DateTime validUntil,string terms)=>repo.AtomicAsync(async()=>
 {
  Need();if(string.IsNullOrWhiteSpace(roleTitle)||roleTitle.Length>200||monthlyBase<0||monthlyBase>1000000000||currency==null||currency.Length!=3||!currency.All(x=>x>='A'&&x<='Z')||startOn.Date<clock.Today||startOn.Year>9998||validUntil.Date<clock.Today||validUntil.Date>startOn.Date||string.IsNullOrWhiteSpace(terms)||terms.Length>4000)throw new DomainException("err.required");
  var c=await repo.FirstOrDefaultAsync(repo.Query<Candidate>().Where(x=>x.Id==candidateId))??throw new DomainException("err.notFound");if(c.Stage!=CandidateStage.Offer)throw new DomainException("err.forbidden");
  var old=await repo.ToListAsync(repo.Query<CandidateOffer>().Where(x=>x.CandidateId==candidateId));var ids=old.Select(x=>x.Id).ToList();var events=await repo.ToListAsync(repo.Query<CandidateOfferEvent>().Where(x=>ids.Contains(x.OfferId)));if(old.Any(x=>events.Where(e=>e.OfferId==x.Id).OrderBy(e=>e.Id).LastOrDefault()?.Kind is not("Rejected" or "Withdrawn" or "Declined")))throw new DomainException("err.forbidden");
  c.InterviewStamp=Guid.NewGuid().ToString("N");repo.Add(new CandidateOffer{CandidateId=c.Id,RoleTitle=roleTitle.Trim(),MonthlyBase=monthlyBase,Currency=currency,StartOn=startOn.Date,ValidUntil=validUntil.Date,Terms=terms.Trim(),CreatedByUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});audit.Log("ProposeOffer","Candidate",c.Id,"Internal proposal, no external letter or commitment");await repo.SaveAsync();
 });
 public Task DecideAsync(int offerId,string decision,string note)=>repo.AtomicAsync(async()=>
 {
  Need();if(string.IsNullOrWhiteSpace(note)||note.Length>4000)throw new DomainException("err.reasonRequired");var offer=await repo.FirstOrDefaultAsync(repo.Query<CandidateOffer>().Where(x=>x.Id==offerId))??throw new DomainException("err.notFound");var c=await repo.FirstOrDefaultAsync(repo.Query<Candidate>().Where(x=>x.Id==offer.CandidateId))??throw new DomainException("err.notFound");if(c.Stage!=CandidateStage.Offer)throw new DomainException("err.forbidden");
  var events=await repo.ToListAsync(repo.Query<CandidateOfferEvent>().Where(x=>x.OfferId==offerId).OrderBy(x=>x.Id));var last=events.LastOrDefault()?.Kind??"Proposed";
  if(decision is "Approved" or "Rejected"){if(last!="Proposed"||offer.CreatedByUserId==me.UserId)throw new DomainException("err.forbidden");if(decision=="Approved"&&offer.ValidUntil<clock.Today)throw new DomainException("err.dates");}
  else if(decision is "Accepted" or "Declined"){if(last!="Approved"||offer.ValidUntil<clock.Today)throw new DomainException("err.forbidden");}
  else if(decision=="Withdrawn"){if(last is not("Proposed" or "Approved"))throw new DomainException("err.forbidden");}
  else throw new DomainException("err.required");
  c.InterviewStamp=Guid.NewGuid().ToString("N");repo.Add(new CandidateOfferEvent{OfferId=offer.Id,Kind=decision,Note=note.Trim(),ActorUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});audit.Log("Offer"+decision,"Candidate",c.Id,"Internal evidence recorded; no employee or payroll created");await repo.SaveAsync();
 });
}
