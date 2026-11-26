using PeopleHub.Domain.Entities;
using Xunit;
namespace PeopleHub.Tests.Architecture;

public class CountryPackTests
{
    [Fact] public void CalculatorDefaultsComeFromTheEgyptPack()
    {
        ICountryPack p = EgyptPack.Y2026;
        Assert.Equal((p.InsuranceRate, p.InsuranceMin, p.InsuranceCap, p.AnnualExemption, p.MartyrsRate),
            (PayrollCalculator.InsuranceRate, PayrollCalculator.InsuranceMin, PayrollCalculator.InsuranceCap, PayrollCalculator.AnnualExemption, PayrollCalculator.MartyrsRate));
        Assert.Equal((0.11m, 2700m, 16700m), (p.InsuranceRate, p.InsuranceMin, p.InsuranceCap));
    }
    [Fact] public void ResolveUsesEffectiveDateAndRefusesToGuess()
    {
        Assert.Same(EgyptPack.Y2026, CountryPacks.Resolve("EG", new DateTime(2026, 11, 1)));
        Assert.Throws<DomainException>(() => CountryPacks.Resolve("EG", new DateTime(2025, 12, 31)));
        Assert.Throws<DomainException>(() => CountryPacks.Resolve("XX", new DateTime(2026, 11, 1)));
    }
    [Fact] public void TaxSlabsMatchTheCalculator()
    {
        foreach (var annual in new[] { 0m, 39000m, 60000m, 150000m, 500000m, 900000m })
        {
            decimal tax = 0, prev = 0;
            foreach (var (upTo, rate) in EgyptPack.Y2026.TaxSlabs) { if (annual > prev) tax += (Math.Min(annual, upTo) - prev) * rate; prev = upTo; }
            Assert.Equal(PayrollCalculator.AnnualTax(annual), tax);
        }
    }
}
