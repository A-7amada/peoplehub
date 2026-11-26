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
