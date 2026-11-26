using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Infrastructure.Security;
namespace PeopleHub.Tests;

public class FieldEncryptionTests
{
 static readonly FieldCipher Shared=new("test-key-one",null);
 sealed class EncDb(DbContextOptions<EncDb> o):AppDbContext(o){protected override FieldCipher Cipher=>Shared;}
 [Fact]public void RoundTripUsesTheEnvelopeAndARandomNonce(){var c=new FieldCipher("k1",null);var a=c.Encrypt("JBSWY3DPEHPK3PXP","AppUser.TotpSecret")!;var b=c.Encrypt("JBSWY3DPEHPK3PXP","AppUser.TotpSecret")!;Assert.StartsWith("enc:v1:",a);Assert.NotEqual(a,b);Assert.DoesNotContain("JBSWY3",a);Assert.Equal("JBSWY3DPEHPK3PXP",c.Decrypt(a,"AppUser.TotpSecret"));Assert.Equal(c.CurrentKeyId,a.Split(':')[2]);}
 [Fact]public void TamperingOrAnotherColumnFailsToDecrypt(){var c=new FieldCipher("k1",null);var a=c.Encrypt("secret","A.B")!;Assert.ThrowsAny<CryptographicException>(()=>c.Decrypt(a,"A.C"));var raw=Convert.FromBase64String(a.Split(':',4)[3]);raw[14]^=1;var bad="enc:v1:"+a.Split(':')[2]+":"+Convert.ToBase64String(raw);Assert.ThrowsAny<CryptographicException>(()=>c.Decrypt(bad,"A.B"));}
 [Fact]public void LegacyPlaintextIsReadAsIsAndNoKeyMeansNoEncryption(){var on=new FieldCipher("k1",null);var off=new FieldCipher(null,null);Assert.Equal("old value",on.Decrypt("old value","A.B"));Assert.Equal("plain",off.Encrypt("plain","A.B"));Assert.False(off.Enabled);Assert.Null(on.Encrypt(null,"A.B"));Assert.Equal("",on.Encrypt("","A.B"));}
 [Fact]public void PreviousKeysStillDecryptAndAMissingKeyFailsClosed(){var old=new FieldCipher("old-key",null);var v=old.Encrypt("secret","A.B")!;var rotated=new FieldCipher("new-key","old-key");Assert.Equal("secret",rotated.Decrypt(v,"A.B"));Assert.NotEqual(old.CurrentKeyId,rotated.CurrentKeyId);var other=new FieldCipher("new-key",null);Assert.ThrowsAny<CryptographicException>(()=>other.Decrypt(v,"A.B"));}
 [Fact]public async Task TotpSecretIsStoredEncryptedAndReadBackThroughTheContext(){var cn=new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");await cn.OpenAsync();await using var d=new EncDb(new DbContextOptionsBuilder<EncDb>().UseSqlite(cn).Options);await d.Database.EnsureCreatedAsync();
  d.Set<AppUser>().Add(new AppUser{Email="enc@example.local",PasswordHash="x",TotpSecret="JBSWY3DPEHPK3PXP",TotpEnabled=true});await d.SaveChangesAsync();
  using var cmd=cn.CreateCommand();cmd.CommandText="select TotpSecret from AppUser where Email='enc@example.local'";var raw=(string)(await cmd.ExecuteScalarAsync())!;Assert.StartsWith("enc:v1:",raw);Assert.DoesNotContain("JBSWY3",raw);
  d.ChangeTracker.Clear();Assert.Equal("JBSWY3DPEHPK3PXP",(await d.Set<AppUser>().SingleAsync(x=>x.Email=="enc@example.local")).TotpSecret);
  using var legacy=cn.CreateCommand();legacy.CommandText="update AppUser set TotpSecret='LEGACYPLAINTEXT' where Email='enc@example.local'";await legacy.ExecuteNonQueryAsync();d.ChangeTracker.Clear();Assert.Equal("LEGACYPLAINTEXT",(await d.Set<AppUser>().SingleAsync(x=>x.Email=="enc@example.local")).TotpSecret);}
}

public class FieldEncryptionBackfillTests
{
 // EF caches the model per context type, and the converter captures the cipher, so each cipher needs its own type.
 sealed class PlainDb(DbContextOptions<PlainDb> o):AppDbContext(o){protected override FieldCipher Cipher=>new(null,null);}
 sealed class KeyedDb(DbContextOptions<KeyedDb> o):AppDbContext(o){public static readonly FieldCipher C=new("k1",null);protected override FieldCipher Cipher=>C;}
 static async Task<(Microsoft.Data.Sqlite.SqliteConnection cn,PlainDb db)> Open(){var cn=new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");await cn.OpenAsync();var db=new PlainDb(new DbContextOptionsBuilder<PlainDb>().UseSqlite(cn).Options);await db.Database.EnsureCreatedAsync();return(cn,db);}
 static async Task<string> Raw(Microsoft.Data.Sqlite.SqliteConnection cn,string sql){using var c=cn.CreateCommand();c.CommandText=sql;return (string)(await c.ExecuteScalarAsync())!;}
 [Fact]public async Task LegacyPlaintextIsEncryptedOnceAndStillReadable()
 {
  var (cn,plainDb)=await Open();
  plainDb.Set<AppUser>().Add(new AppUser{Email="b@example.local",PasswordHash="x",TotpSecret="LEGACYSEED",TotpEnabled=true});await plainDb.SaveChangesAsync();
  Assert.Equal("LEGACYSEED",await Raw(cn,"select TotpSecret from AppUser where Email='b@example.local'"));
  var cipher=KeyedDb.C;
  await using var db=new KeyedDb(new DbContextOptionsBuilder<KeyedDb>().UseSqlite(cn).Options);
  var first=await PeopleHub.Infrastructure.Security.FieldEncryptionBackfill.RunAsync(db,cipher);
  Assert.True(first.Enabled);Assert.True(first.Encrypted>=1);Assert.Equal(0,first.Remaining);
  var second=await PeopleHub.Infrastructure.Security.FieldEncryptionBackfill.RunAsync(db,cipher);Assert.Equal(0,second.Encrypted);
  Assert.StartsWith("enc:v1:",await Raw(cn,"select TotpSecret from AppUser where Email='b@example.local'"));
  Assert.Equal("LEGACYSEED",(await db.Set<AppUser>().SingleAsync(x=>x.Email=="b@example.local")).TotpSecret);
 }
 [Fact]public async Task DisabledCipherReportsPlaintextAndWritesNothing()
 {
  var (cn,db)=await Open();
  var r=await PeopleHub.Infrastructure.Security.FieldEncryptionBackfill.RunAsync(db,new FieldCipher(null,null));
  Assert.False(r.Enabled);Assert.Equal(0,r.Encrypted);
 }
}

public class HeadcountReportTests
{
 static Employee E(int id,string dept,DateTime hire)=>new(){Id=id,Code="E"+id,FullName="N"+id,Department=new Department{Name=dept},HireDate=hire};
 static StatusHistory H(int id,int emp,EmployeeStatus from,EmployeeStatus to,DateTime at)=>new(){Id=id,EmployeeId=emp,From=from,To=to,At=at};
 [Fact]public void CountsStartEndHiresExitsRehiresAndTurnover()
 {
  var emps=new List<Employee>{E(1,"Ops",new(2020,1,1)),E(2,"Ops",new(2021,1,1)),E(3,"Ops",new(2026,11,10)),E(4,"Ops",new(2022,1,1)),E(5,"Fin",new(2026,12,5))};
  var hist=new List<StatusHistory>{H(1,2,EmployeeStatus.Active,EmployeeStatus.Resigned,new(2026,11,20)),H(2,4,EmployeeStatus.Active,EmployeeStatus.Terminated,new(2026,9,1)),H(3,4,EmployeeStatus.Terminated,EmployeeStatus.Active,new(2026,11,25))};
  var rows=PeopleHub.Application.Modules.Reporting.ReportService.HeadcountRows(emps,hist,2026,11);
  var ops=rows.Single(r=>r[0]=="Ops");
  // start (Oct 31): 1 and 2 employed, 4 gone since Sept. end (Nov 30): 1, 3, 4 employed; 2 left.
  Assert.Equal(new[]{"Ops","2","1","1","1","3","40.0"},ops.ToArray());
  var fin=rows.Single(r=>r[0]=="Fin");
  Assert.Equal(new[]{"Fin","0","0","0","0","0","-"},fin.ToArray());
 }
 [Fact]public void SuspendedStillCountsAndSameMonthHireAndExitAreBothCounted()
 {
  var emps=new List<Employee>{E(1,"Ops",new(2026,11,2))};
  var hist=new List<StatusHistory>{H(1,1,EmployeeStatus.Active,EmployeeStatus.Suspended,new(2026,11,5)),H(2,1,EmployeeStatus.Suspended,EmployeeStatus.Resigned,new(2026,11,28))};
  var r=PeopleHub.Application.Modules.Reporting.ReportService.HeadcountRows(emps,hist,2026,11).Single();
  Assert.Equal(new[]{"Ops","0","1","0","1","0","-"},r.ToArray());
 }
}
