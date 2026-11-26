namespace PeopleHub.Domain.Entities;

/// <summary>Country rule set (Strategy pattern, ADR-0007). The payroll engine runs the calculation contract; a pack supplies the numbers.
/// Layers: general HR rules, country pack, company policy, employee exception. This is the country layer only.
/// A demo: the values must be reviewed by a labour lawyer and a payroll accountant before real use.</summary>
public interface ICountryPack
{
    string Country { get; }
    DateTime EffectiveFrom { get; }
    decimal InsuranceRate { get; }
    decimal InsuranceMin { get; }
    decimal InsuranceCap { get; }
    decimal AnnualExemption { get; }
    decimal MartyrsRate { get; }
    IReadOnlyList<(decimal UpTo, decimal Rate)> TaxSlabs { get; }
}

/// <summary>Egypt 2026 values as already used by the demo (insurance 11% on a wage between 2,700 and 16,700, annual exemption 20,000,
/// Martyrs fund 0.05%, tax slabs up to 27.5%). Constants are public so the calculator can keep compile-time defaults.</summary>
public sealed class EgyptPack : ICountryPack
{
    public const decimal InsuranceRate = 0.11m;
    public const decimal InsuranceMin = 2700m;
    public const decimal InsuranceCap = 16700m;
    public const decimal AnnualExemption = 20000m;
    public const decimal MartyrsRate = 0.0005m;
    public static readonly EgyptPack Y2026 = new();
    static readonly (decimal, decimal)[] Slabs = [(40000, 0m), (55000, 0.10m), (70000, 0.15m), (200000, 0.20m), (400000, 0.225m), (600000, 0.25m), (decimal.MaxValue, 0.275m)];
    public string Country => "EG";
    public DateTime EffectiveFrom => new(2026, 1, 1);
    decimal ICountryPack.InsuranceRate => InsuranceRate;
    decimal ICountryPack.InsuranceMin => InsuranceMin;
    decimal ICountryPack.InsuranceCap => InsuranceCap;
    decimal ICountryPack.AnnualExemption => AnnualExemption;
    decimal ICountryPack.MartyrsRate => MartyrsRate;
    public IReadOnlyList<(decimal UpTo, decimal Rate)> TaxSlabs => Slabs;
}

/// <summary>Registry with effective dating: the pack in force on a date is the latest one whose EffectiveFrom is not after it.
/// It does not guess: an unknown country or a date before every pack is a DomainException.</summary>
public static class CountryPacks
{
    static readonly ICountryPack[] All = [EgyptPack.Y2026];
    public static bool Covers(string country, DateTime on) => All.Any(p => p.Country == country && p.EffectiveFrom <= on);
    public static ICountryPack Resolve(string country, DateTime on) =>
        All.Where(p => p.Country == country && p.EffectiveFrom <= on).OrderByDescending(p => p.EffectiveFrom).FirstOrDefault()
        ?? throw new DomainException("err.countryPackMissing");
}
