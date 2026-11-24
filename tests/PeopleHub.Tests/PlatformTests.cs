using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Security;
using Xunit;
namespace PeopleHub.Tests;
public class PlatformTests
{
    [Fact] public void Totp_Rfc6238Vector() => Assert.Equal("287082", Totp.Code(Totp.Base32(System.Text.Encoding.ASCII.GetBytes("12345678901234567890")), 59));
    [Fact] public void Totp_VerifyWindow() { var s = Totp.NewSecret(); var now = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc); var c = Totp.Code(s, new DateTimeOffset(now).ToUnixTimeSeconds()); Assert.True(Totp.Verify(s, c, now.AddSeconds(20))); Assert.False(Totp.Verify(s, c, now.AddMinutes(5))); }
    [Fact] public void Insurance_HasFloorAndCap() { Assert.Equal(297m, PayrollCalculator.InsuranceOf(1000)); Assert.Equal(1837m, PayrollCalculator.InsuranceOf(99999)); }
    [Fact] public void Tax_ExemptBelowFortyThousandAnnual() => Assert.Equal(0m, PayrollCalculator.AnnualTax(40000));
    [Fact] public void Tax_TopBracket() => Assert.Equal(40000m*0 + 1500m + 2250m + 26000m + 45000m + 50000m + 27500m, PayrollCalculator.AnnualTax(700000));
    [Fact] public void Martyrs_IsHalfPermille() { var l = new PayrollLine(); PayrollCalculator.Apply(l, new PayrollInput(20000, 0, 0, 0, 0, 0, 0, 0, 0)); Assert.Equal(10m, l.Martyrs); }
    [Fact] public void Settlement_AddsComponents() { var r = Settlement.Calculate(30000, new DateTime(2020,1,1), new DateTime(2026,1,15), 10, 15); Assert.Equal(15000m, r.ProratedSalary); Assert.Equal(10000m, r.LeaveEncashment); Assert.True(r.Gratuity > 0); Assert.Equal(r.ProratedSalary + r.LeaveEncashment + r.Gratuity, r.Total); }
    [Fact] public void Candidate_StagesAdvance() { Assert.Equal(CandidateStage.Screening, Candidate.Next(CandidateStage.Applied)); Assert.Null(Candidate.Next(CandidateStage.Hired)); }
    [Fact] public void Review_ScoreRange() { Assert.True(PerformanceReview.ValidScore(5)); Assert.False(PerformanceReview.ValidScore(6)); }
}
