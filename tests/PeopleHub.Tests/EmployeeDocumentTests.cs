using Microsoft.EntityFrameworkCore;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Application.Services;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class EmployeeDocumentTests
{
 class User(int employee=1,bool hr=true):ICurrentUser{public string Email=>"document@example.local";public int? EmployeeId=>employee;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>hr;public bool Can(string p)=>hr;}
 static async Task<AppDbContext> Db(){var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));return d;}
 static AttachmentService F(AppDbContext d,User? u=null){u??=new();return new(d,new AccessScope(d,u),u,new AppClock(TimeProvider.System));}
 static EmployeeDocuments S(AppDbContext d,User? u=null){u??=new();var c=new AppClock(TimeProvider.System);return new(d,u,c,new AuditService(d,u,c),new NotificationService(d,u,c),F(d,u));}
 static byte[] Pdf=>System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\nFictional document test\n%%EOF");
 [Fact]public async Task EmployeeDocumentIsPrivateToEmployeeAndHr(){await using var d=await Db();await S(d).AddAsync(6,"Fictional document",new(2026,12,1),"test.pdf",Pdf);var doc=await d.Set<EmployeeDocument>().SingleAsync();Assert.Single(await S(d,new(6,false)).ListAsync(6));Assert.Equal(Pdf,(await F(d,new(6,false)).GetAsync(doc.AttachmentToken)).Data);await Assert.ThrowsAsync<DomainException>(()=>F(d,new(7,false)).GetAsync(doc.AttachmentToken));await Assert.ThrowsAsync<DomainException>(()=>S(d,new(7,false)).ListAsync(6));}
 [Fact]public async Task DisguisedFileIsRejectedAndNoPartialRowsPersist(){await using var d=await Db();await Assert.ThrowsAsync<DomainException>(()=>S(d).AddAsync(6,"Bad",null,"test.pdf",new byte[]{1,2,3}));Assert.Empty(await d.Set<Attachment>().ToListAsync());Assert.Empty(await d.Set<EmployeeDocument>().ToListAsync());}
 [Fact]public async Task ExpiryAlertsAreIdempotentWithinDay(){await using var d=await Db();await S(d).AddAsync(6,"Fictional expiring",new(2026,11,20),"test.pdf",Pdf);await S(d).AlertAsync();var n=await d.Set<Notification>().CountAsync(x=>x.TitleKey=="notif.documentExpiry");Assert.True(n>0);await S(d).AlertAsync();Assert.Equal(n,await d.Set<Notification>().CountAsync(x=>x.TitleKey=="notif.documentExpiry"));}
 [Fact]public async Task EmployeeCannotUploadAndFutureNonExpiringDocumentDoesNotAlert(){await using var d=await Db();await Assert.ThrowsAsync<DomainException>(()=>S(d,new(6,false)).AddAsync(6,"Mine",null,"test.pdf",Pdf));await S(d).AddAsync(6,"No expiry",null,"test.pdf",Pdf);await S(d).AlertAsync();Assert.False(await d.Set<Notification>().AnyAsync(x=>x.TitleKey=="notif.documentExpiry"));}
}
