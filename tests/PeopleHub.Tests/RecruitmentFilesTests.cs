using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Talent;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
/// <summary>US-PH2-014 candidate CV and document upload (files in the database, access-controlled, audited).</summary>
public class RecruitmentFilesTests
{
 class Time:TimeProvider{public override DateTimeOffset GetUtcNow()=>new(2026,11,24,18,0,0,TimeSpan.Zero);}
 class User(bool hr=true):ICurrentUser{public string Email=>"cv@example.local";public int? EmployeeId=>1;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>hr;public bool Can(string p)=>hr;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static RecruitmentFiles S(AppDbContext d,bool hr=true){var u=new User(hr);var c=new AppClock(new Time());return new(d,u,c,new AuditService(d,u,c));}
 static byte[] Pdf(string t)=>System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 fictional "+t);
 static async Task<int> Cid(AppDbContext d)=>await d.Set<Candidate>().Select(x=>x.Id).FirstAsync();
 [Fact]public async Task UploadStoresCheckedFileAndRejectsBadInput(){await using var d=await Db();var id=await Cid(d);
  await Assert.ThrowsAsync<DomainException>(()=>S(d,false).UploadAsync(id,"CV","cv.pdf",Pdf("a")));
  await Assert.ThrowsAsync<DomainException>(()=>S(d).UploadAsync(id,"Other","cv.pdf",Pdf("a")));
  await Assert.ThrowsAsync<DomainException>(()=>S(d).UploadAsync(id,"CV","cv.exe",Pdf("a")));
  await Assert.ThrowsAsync<DomainException>(()=>S(d).UploadAsync(id,"CV","cv.pdf",System.Text.Encoding.ASCII.GetBytes("not a pdf")));
  await Assert.ThrowsAsync<DomainException>(()=>S(d).UploadAsync(id,"CV","cv.pdf",new byte[AttachmentServiceMax+1]));
  await Assert.ThrowsAsync<DomainException>(()=>S(d).UploadAsync(99999,"CV","cv.pdf",Pdf("a")));
  await S(d).UploadAsync(id,"CV","../x/cv.pdf",Pdf("a"));var f=await d.Set<CandidateFile>().SingleAsync();Assert.Equal("cv.pdf",f.FileName);Assert.Equal("application/pdf",f.ContentType);Assert.Equal(64,f.Sha256.Length);}
 const int AttachmentServiceMax=2*1024*1024;
 [Fact]public async Task ListAndDownloadNeedAccessAndAreAuditedWithoutNames(){await using var d=await Db();var id=await Cid(d);await S(d).UploadAsync(id,"Document","cert.pdf",Pdf("b"));
  var info=Assert.Single(await S(d).ListAsync());Assert.Equal("Document",info.Kind);
  await Assert.ThrowsAsync<DomainException>(()=>S(d,false).ListAsync());await Assert.ThrowsAsync<DomainException>(()=>S(d,false).DownloadAsync(info.Token));
  await Assert.ThrowsAsync<DomainException>(()=>S(d).DownloadAsync("nope"));
  var f=await S(d).DownloadAsync(info.Token);Assert.Equal(Pdf("b"),f.Data);
  var logs=await d.Set<AuditLog>().Where(a=>a.Entity=="CandidateFile").ToListAsync();Assert.True(logs.Count>=2);Assert.All(logs,a=>Assert.DoesNotContain("cert.pdf",a.Details));}
 [Fact]public async Task LimitPerCandidateAndDelete(){await using var d=await Db();var id=await Cid(d);
  for(var i=0;i<RecruitmentFiles.MaxPerCandidate;i++)await S(d).UploadAsync(id,"Document","d"+i+".pdf",Pdf(i.ToString()));
  await Assert.ThrowsAsync<DomainException>(()=>S(d).UploadAsync(id,"Document","extra.pdf",Pdf("x")));
  var one=(await S(d).ListAsync())[0];await Assert.ThrowsAsync<DomainException>(()=>S(d,false).DeleteAsync(one.Id));await S(d).DeleteAsync(one.Id);
  Assert.Equal(RecruitmentFiles.MaxPerCandidate-1,(await S(d).ListAsync()).Count);await S(d).UploadAsync(id,"CV","new.pdf",Pdf("y"));}
}
