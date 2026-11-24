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
public class PerfTests
{
    static PerfGoal G(int w, int? s) => new() { Weight = w, ManagerScore = s };
    [Fact] public void GoalScore_IsWeighted() => Assert.Equal(3.6m, PerfCalc.GoalScore(new[] { G(60, 4), G(40, 3) }));
    [Fact] public void GoalScore_NullWithoutRatings() => Assert.Null(PerfCalc.GoalScore(new[] { G(100, null) }));
    [Fact] public void Computed_Blends70_30() => Assert.Equal(4.3m, PerfCalc.Computed(4m, 5));
    [Fact] public void Bands_AndAdvice() { Assert.Equal("Outstanding", PerfCalc.Band(4.6m)); Assert.Equal("NeedsImprovement", PerfCalc.Band(2.4m)); Assert.Equal("ImprovementPlan", PerfCalc.Recommendation(2m)); Assert.Equal("PromotionReview+Bonus", PerfCalc.Recommendation(4.5m)); }
    [Fact] public void Weights_MustTotal100() { Assert.True(PerfCalc.WeightsOk(new[] { G(60, 1), G(40, 1) })); Assert.False(PerfCalc.WeightsOk(new[] { G(60, 1) })); }
}

public class PerfExtensionTests
{
    [Fact] public void WeightsRequire100() { Assert.True(PerfFormula.Valid(50,30,20)); Assert.False(PerfFormula.Valid(70,40,0)); Assert.False(PerfFormula.Valid(-10,100,10)); }
    [Fact] public void FormulaUses360() => Assert.Equal(3.9m, PerfFormula.Calculate(4m,3,5m,50,30,20));
    [Fact] public void MissingRequiredComponentStaysIncomplete() { Assert.Null(PerfFormula.Calculate(4m,3,null,50,30,20)); Assert.Null(PerfFormula.Calculate(null,3,null,70,30,0)); }
    [Fact] public void ZeroWeightAllowsAbsentComponent() => Assert.Equal(4m, PerfFormula.Calculate(null,4,null,0,100,0));
    [Fact] public void DatePhasesIncludeBoundary() { var cal=new DateTime(2026,6,1);var close=new DateTime(2026,6,8); Assert.Equal(CycleStatus.Open,PerfFormula.Phase(cal.AddDays(-1),cal,close)); Assert.Equal(CycleStatus.Calibration,PerfFormula.Phase(cal,cal,close));Assert.Equal(CycleStatus.Closed,PerfFormula.Phase(close,cal,close)); }
}
