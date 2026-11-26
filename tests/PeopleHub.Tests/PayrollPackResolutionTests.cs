using PeopleHub.Domain.Entities;
namespace PeopleHub.Tests;
public class PayrollPackResolutionTests
{
    static PayrollInput In() => new(15000m, 3000m, 0, 500m, 1, 30, 4m, 200m, 100m);
    static PayrollLine Run(ICountryPack? p){var l=new PayrollLine();if(p==null)PayrollCalculator.Apply(l,In());else PayrollCalculator.Apply(l,In(),p);return l;}
    [Fact]public void ExplicitPackGivesSameResultAsDefault(){var a=Run(null);var b=Run(EgyptPack.Y2026);Assert.Equal((a.Insurance,a.Tax,a.Martyrs,a.Net,a.TotalDeductions),(b.Insurance,b.Tax,b.Martyrs,b.Net,b.TotalDeductions));}
    [Theory][InlineData(2026,1)][InlineData(2026,11)][InlineData(2027,5)]public void PeriodsFromPackStartResolveEgyptPack(int y,int m){Assert.Same(EgyptPack.Y2026,PayrollCalculator.PackFor(y,m));Assert.False(PayrollCalculator.IsFallback(y,m));}
    [Fact]public void PeriodBeforeFirstPackIsFlaggedAsFallbackAndKeepsExistingBehaviour(){Assert.True(PayrollCalculator.IsFallback(2025,12));Assert.Same(EgyptPack.Y2026,PayrollCalculator.PackFor(2025,12));}
    [Fact]public void UnknownCountryHasNoSilentFallback(){Assert.Throws<DomainException>(()=>PayrollCalculator.PackFor(2026,5,"XX"));}
    sealed class Alt:ICountryPack{public string Country=>"EG";public DateTime EffectiveFrom=>new(2030,1,1);public decimal InsuranceRate=>0.10m;public decimal InsuranceMin=>2700m;public decimal InsuranceCap=>16700m;public decimal AnnualExemption=>20000m;public decimal MartyrsRate=>0.0005m;public IReadOnlyList<(decimal UpTo,decimal Rate)> TaxSlabs=>[(decimal.MaxValue,0.10m)];}
    [Fact]public void CalculatorUsesThePackItIsGiven(){var a=Run(EgyptPack.Y2026);var b=Run(new Alt());Assert.NotEqual(a.Insurance,b.Insurance);Assert.NotEqual(a.Tax,b.Tax);}
    [Fact]public void DemoPolicyValuesAreUnchanged(){var l=Run(null);Assert.Equal(PayrollCalculator.InsuranceOf(18000m),l.Insurance);Assert.Equal(PayrollCalculator.Round(l.Gross*EgyptPack.MartyrsRate),l.Martyrs);}
}
