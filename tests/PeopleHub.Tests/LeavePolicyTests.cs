using PeopleHub.Application.Modules.Leave;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Application.Common;
using PeopleHub.Infrastructure.Persistence;
using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
/// <summary>ADR-0022: rules of the configurable leave policy. Values are demo defaults, not legal advice.</summary>
public class LeavePolicyTests
{
 static List<LeaveEntitlementTier> Tiers()=>[new(){FromServiceYears=0,Days=21},new(){FromServiceYears=10,Days=30},new(){FromServiceYears=0,FromAge=50,Days=30}];
 static LeavePolicy P(string accrual="Annual",int wait=6,bool prorate=true)=>new(){WaitingMonths=wait,Accrual=accrual,ProrateFirstYear=prorate};
 [Fact]public void TierDaysUsesServiceOrAgeAndTheHighestRowWins()
 {
  Assert.Equal(21,LeavePolicyRules.TierDays(Tiers(),new(2020,1,1),new(1990,1,1),new(2026,1,1)));
  Assert.Equal(30,LeavePolicyRules.TierDays(Tiers(),new(2015,1,1),new(1990,1,1),new(2026,1,1)));
  Assert.Equal(30,LeavePolicyRules.TierDays(Tiers(),new(2024,1,1),new(1970,6,1),new(2026,1,1)));
  Assert.Null(LeavePolicyRules.TierDays([],new(2024,1,1),null,new(2026,1,1)));
 }
 [Fact]public void CompletedMonthsDoesNotCountAnUnfinishedMonth()
 {
  Assert.Equal(4,LeavePolicyRules.CompletedMonths(new(2026,1,15),new(2026,6,14)));
  Assert.Equal(6,LeavePolicyRules.CompletedMonths(new(2026,1,15),new(2026,7,15)));
 }
 [Fact]public void WaitingPeriodBlocksUntilTheEligibleDate()
 {
  var before=LeavePolicyRules.Calculate(P(),Tiers(),21,new(2026,3,10),null,new(2026,9,9));
  Assert.False(before.Eligible);Assert.Equal(new DateTime(2026,9,10),before.EligibleFrom);Assert.Equal(0m,before.AccruedToDate);
  Assert.True(LeavePolicyRules.Calculate(P(),Tiers(),21,new(2026,3,10),null,new(2026,9,10)).Eligible);
 }
 [Fact]public void FirstYearIsProratedByFullMonthsAndRoundedDownToHalfDay()
 {
  // hired mid-March: 9 full months left (Apr to Dec): 21 * 9 / 12 = 15.75 -> 15.5
  var r=LeavePolicyRules.Calculate(P(wait:0),Tiers(),21,new(2026,3,10),null,new(2026,12,31));
  Assert.Equal(15.5m,r.GrantThisYear);
  // hired on the 1st counts that month: 21 * 10 / 12 = 17.5
  Assert.Equal(17.5m,LeavePolicyRules.Calculate(P(wait:0),Tiers(),21,new(2026,3,1),null,new(2026,12,31)).GrantThisYear);
  Assert.Equal(21m,LeavePolicyRules.Calculate(P(wait:0,prorate:false),Tiers(),21,new(2026,3,10),null,new(2026,12,31)).GrantThisYear);
 }
 [Fact]public void MonthlyAccrualGivesEntitlementOverTwelvePerFullMonth()
 {
  var r=LeavePolicyRules.Calculate(P("Monthly",0),Tiers(),21,new(2020,1,1),null,new(2026,6,30));
  Assert.Equal(21,r.FullYearDays);Assert.Equal(21m,r.GrantThisYear);Assert.Equal(8.5m,r.AccruedToDate); // 5 full months (Jan to May): 21*5/12 = 8.75, rounded down to 8.5
 }
 [Fact]public void ParseTiersAcceptsGoodLinesAndRejectsBadOnes()
 {
  var rows=LeavePolicyRules.ParseTiers("0,,21\n10,,30\n,50,30");
  Assert.Equal(3,rows.Count);Assert.Equal((0,(int?)null,21),rows[0]);Assert.Equal((0,(int?)50,30),rows[2]);
  Assert.Throws<DomainException>(()=>LeavePolicyRules.ParseTiers("1,2"));
  Assert.Throws<DomainException>(()=>LeavePolicyRules.ParseTiers("x,,21"));
  Assert.Throws<DomainException>(()=>LeavePolicyRules.ParseTiers("0,5,21"));
 }
 class U(params string[] g):ICurrentUser{public string Email=>"admin@example.local";public int? EmployeeId=>1;public int UserId=>1;public bool IsAuthenticated=>true;public bool IsInRole(string r)=>false;public bool Can(string p)=>g.Contains(p);}
 [Fact]public async Task ServiceNeedsSettingsGrantSavesTiersAuditsAndPreviewChangesNothing()
 {
  var d=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options);await d.Database.OpenConnectionAsync();await d.Database.MigrateAsync();
  await SeedData.InitializeAsync(d,"Admin123!","Demo123!",new DateTime(2026,11,24));
  var c=new AppClock(TimeProvider.System);var admin=new U(Perm.AdmSettings);var none=new U();
  var typeId=(await d.Set<LeaveType>().FirstAsync(t=>t.DeductsBalance)).Id;
  await Assert.ThrowsAsync<DomainException>(()=>new LeavePolicyService(d,none,c,new AuditService(d,none,c)).SaveAsync(typeId,true,6,"Annual",true,3,true,false,8,"0,,21"));
  var svc=new LeavePolicyService(d,admin,c,new AuditService(d,admin,c));
  await svc.SaveAsync(typeId,false,6,"Monthly",true,3,true,false,8,"0,,21\n10,,30\n,50,30");await d.SaveChangesAsync();
  var row=(await svc.ListAsync()).Single(r=>r.Type.Id==typeId);
  Assert.Equal("Monthly",row.Policy.Accrual);Assert.False(row.Policy.Enabled);Assert.Equal(3,row.Tiers.Count);
  await Assert.ThrowsAsync<DomainException>(()=>svc.SaveAsync(typeId,false,6,"Weekly",true,3,true,false,8,""));
  var balancesBefore=await d.Set<LeaveBalance>().CountAsync();
  var code=(await d.Set<Employee>().FirstAsync()).Code;
  var pv=await svc.PreviewAsync(typeId,code,new DateTime(2026,11,24));
  Assert.True(pv.Result.FullYearDays>=21);
  Assert.Equal(balancesBefore,await d.Set<LeaveBalance>().CountAsync());
  Assert.True(await d.Set<AuditLog>().AnyAsync(a=>a.Entity=="LeavePolicy"));
 }
 [Fact]public void OpeningDaysAppliesOnlyToEnabledAnnualPoliciesInWholeDays()
 {
  var on=P();on.Enabled=true;
  Assert.Null(LeavePolicyRules.OpeningDays(P(),Tiers(),21,new(2020,1,1),null,2026));
  Assert.Null(LeavePolicyRules.OpeningDays(null,Tiers(),21,new(2020,1,1),null,2026));
  var mo=P("Monthly");mo.Enabled=true;
  Assert.Null(LeavePolicyRules.OpeningDays(mo,Tiers(),21,new(2020,1,1),null,2026));
  Assert.Equal(21,LeavePolicyRules.OpeningDays(on,Tiers(),21,new(2020,1,1),null,2026));
  Assert.Equal(15,LeavePolicyRules.OpeningDays(on,Tiers(),21,new(2026,3,10),null,2026));
  Assert.Equal(0,LeavePolicyRules.OpeningDays(on,Tiers(),21,new(2026,9,10),null,2026));
  Assert.Equal(0,LeavePolicyRules.OpeningDays(on,Tiers(),21,new(2027,1,1),null,2026));
 }
}
