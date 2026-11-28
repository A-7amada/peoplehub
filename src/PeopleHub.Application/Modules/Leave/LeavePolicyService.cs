using System.Globalization;
using PeopleHub.Application.Common;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Modules.Leave;

/// <summary>Pure rules for ADR-0022. Dates are calendar dates; service and age count completed years and months.</summary>
public static class LeavePolicyRules
{
    public static int CompletedMonths(DateTime from, DateTime to)
    {
        var m = (to.Year - from.Year) * 12 + to.Month - from.Month;
        if (to.Day < from.Day) m--;
        return Math.Max(0, m);
    }

    /// <summary>Days a full year grants at the given date: the highest tier whose service years or age the employee has reached. Null when no tier matches.</summary>
    public static int? TierDays(IEnumerable<LeaveEntitlementTier> tiers, DateTime hire, DateTime? birth, DateTime asOf)
    {
        var years = CompletedMonths(hire, asOf) / 12;
        var age = birth == null ? (int?)null : CompletedMonths(birth.Value, asOf) / 12;
        var hits = tiers.Where(t => t.FromAge == null ? years >= t.FromServiceYears : age != null && age >= t.FromAge).Select(t => t.Days).ToList();
        return hits.Count == 0 ? null : hits.Max();
    }

    static decimal RoundDownToHalf(decimal d) => Math.Floor(d * 2) / 2;

    public record Preview(bool Eligible, DateTime EligibleFrom, int FullYearDays, decimal GrantThisYear, decimal AccruedToDate);

    /// <summary>What the policy would give an employee in the year of asOf. GrantThisYear is the year's grant (pro rata in the hire year when asked); AccruedToDate is what has accrued by asOf under the chosen accrual mode.</summary>
    public static Preview Calculate(LeavePolicy policy, IReadOnlyList<LeaveEntitlementTier> tiers, int defaultDays, DateTime hire, DateTime? birth, DateTime asOf)
    {
        var eligibleFrom = hire.Date.AddMonths(policy.WaitingMonths);
        var eligible = asOf.Date >= eligibleFrom;
        var full = TierDays(tiers, hire, birth, new DateTime(asOf.Year, 1, 1) > hire ? new DateTime(asOf.Year, 1, 1) : hire) ?? defaultDays;
        decimal grant = full;
        if (policy.ProrateFirstYear && hire.Year == asOf.Year)
        {
            var monthsLeft = 12 - hire.Month + (hire.Day == 1 ? 1 : 0);
            grant = RoundDownToHalf(full * monthsLeft / 12m);
        }
        if (!eligible) return new(false, eligibleFrom, full, grant, 0);
        decimal accrued = grant;
        if (policy.Accrual == "Monthly")
        {
            var start = hire.Year == asOf.Year ? hire : new DateTime(asOf.Year, 1, 1);
            var months = Math.Min(12, CompletedMonths(start, asOf.Date));
            accrued = RoundDownToHalf(full * months / 12m);
            if (accrued > grant) accrued = grant;
        }
        return new(true, eligibleFrom, full, grant, accrued);
    }

    /// <summary>Whole days written as the opening entitlement of a year. Null when the policy is off or uses monthly accrual (not applied yet), so the type default stays. Zero when the waiting period ends after the year does.</summary>
    public static int? OpeningDays(LeavePolicy? policy, IReadOnlyList<LeaveEntitlementTier> tiers, int defaultDays, DateTime hire, DateTime? birth, int year)
    {
        if (policy is not { Enabled: true } || policy.Accrual != "Annual") return null;
        if (hire.Year > year) return 0;
        var r = Calculate(policy, tiers, defaultDays, hire, birth, new DateTime(year, 12, 31));
        return r.Eligible ? (int)Math.Floor(r.GrantThisYear) : 0;
    }

    /// <summary>ADR-0022 unit 3c. Entitlement accrued by asOf under a Monthly policy (entitlement / 12 per full month, rounded down to a half day, capped at the year grant). Null when the policy is off, Annual, or the employee is not yet eligible. Illustrative default, not legally validated.</summary>
    public static decimal? MonthlyAccrued(LeavePolicy? policy, IReadOnlyList<LeaveEntitlementTier> tiers, int defaultDays, DateTime hire, DateTime? birth, DateTime asOf)
    {
        if (policy is not { Enabled: true } || policy.Accrual != "Monthly") return null;
        if (hire.Year > asOf.Year) return 0;
        var r = Calculate(policy, tiers, defaultDays, hire, birth, asOf);
        return r.Eligible ? r.AccruedToDate : 0;
    }

    /// <summary>Carried days lapse CarryExpiryMonths after the balance year starts (0 = never). Used days consume carried days first, so the lapsing amount is the carried part not yet used. Returns the days to remove from CarriedOver, 0 when nothing lapses.</summary>
    public static decimal ExpiredCarry(LeavePolicy? policy, decimal carriedOver, decimal used, int year, DateTime asOf)
    {
        if (policy is not { Enabled: true } || policy.CarryExpiryMonths <= 0 || carriedOver <= 0) return 0;
        if (asOf.Date < new DateTime(year, 1, 1).AddMonths(policy.CarryExpiryMonths)) return 0;
        return Math.Max(0, carriedOver - Math.Max(0, used));
    }

    /// <summary>Parses tier lines "serviceYears,age,days" (age may be empty). Throws a DomainException on a bad line.</summary>
    public static List<(int Years, int? Age, int Days)> ParseTiers(string? text)
    {
        var rows = new List<(int, int?, int)>();
        foreach (var raw in (text ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var p = raw.Split(',', StringSplitOptions.TrimEntries);
            if (p.Length != 3 || !int.TryParse(p[0].Length == 0 ? "0" : p[0], NumberStyles.None, CultureInfo.InvariantCulture, out var y) || !int.TryParse(p[2], NumberStyles.None, CultureInfo.InvariantCulture, out var d) || d > 366 || y > 60)
                throw new DomainException("err.required");
            int? age = null;
            if (p[1].Length > 0) { if (!int.TryParse(p[1], NumberStyles.None, CultureInfo.InvariantCulture, out var a) || a < 14 || a > 100) throw new DomainException("err.required"); age = a; }
            rows.Add((y, age, d));
        }
        if (rows.Count > 12) throw new DomainException("err.required");
        return rows;
    }
}

public class LeavePolicyService(IRepository repo, ICurrentUser me, AppClock clock, AuditService audit)
{
    void Guard() { if (!me.Can(Perm.AdmSettings)) throw new DomainException("err.forbidden"); }

    public record Row(LeaveType Type, LeavePolicy Policy, List<LeaveEntitlementTier> Tiers);

    public async Task<List<Row>> ListAsync()
    {
        Guard();
        var types = await repo.ToListAsync(repo.Query<LeaveType>().Where(t => t.DeductsBalance).OrderBy(t => t.Id));
        var policies = await repo.ToListAsync(repo.Query<LeavePolicy>());
        var tiers = await repo.ToListAsync(repo.Query<LeaveEntitlementTier>().OrderBy(t => t.FromServiceYears).ThenBy(t => t.FromAge));
        return types.Select(t => new Row(t, policies.FirstOrDefault(p => p.LeaveTypeId == t.Id) ?? new LeavePolicy { LeaveTypeId = t.Id }, tiers.Where(x => x.LeaveTypeId == t.Id).ToList())).ToList();
    }

    public Task SaveAsync(int typeId, bool enabled, int waitingMonths, string accrual, bool prorate, int expiryMonths, bool halfDay, bool hourly, int hoursPerDay, string? tierText) => repo.AtomicAsync(async () =>
    {
        Guard();
        var type = await repo.FirstOrDefaultAsync(repo.Query<LeaveType>().Where(t => t.Id == typeId)) ?? throw new DomainException("err.notFound");
        if (waitingMonths is < 0 or > 60 || expiryMonths is < 0 or > 24 || hoursPerDay is < 1 or > 24 || accrual is not ("Annual" or "Monthly")) throw new DomainException("err.required");
        var parsed = LeavePolicyRules.ParseTiers(tierText);
        var pol = await repo.FirstOrDefaultAsync(repo.Query<LeavePolicy>().Where(p => p.LeaveTypeId == typeId));
        if (pol == null) { pol = new LeavePolicy { LeaveTypeId = typeId }; repo.Add(pol); }
        pol.Enabled = enabled; pol.WaitingMonths = waitingMonths; pol.Accrual = accrual; pol.ProrateFirstYear = prorate; pol.CarryExpiryMonths = expiryMonths; pol.AllowHalfDay = halfDay; pol.AllowHourly = hourly; pol.HoursPerDay = hoursPerDay;
        foreach (var old in await repo.ToListAsync(repo.Query<LeaveEntitlementTier>().Where(t => t.LeaveTypeId == typeId))) repo.Remove(old);
        foreach (var (y, a, d) in parsed) repo.Add(new LeaveEntitlementTier { LeaveTypeId = typeId, FromServiceYears = y, FromAge = a, Days = d });
        audit.Log("Update", "LeavePolicy", type.Code, $"enabled={enabled}; waiting={waitingMonths}m; accrual={accrual}; prorate={prorate}; expiry={expiryMonths}m; half={halfDay}; hourly={hourly}; tiers={parsed.Count}");
    });

    /// <summary>Preview only: reads the employee's hire and birth dates and shows what the saved policy would grant. Changes nothing.</summary>
    public async Task<(Employee Employee, LeaveType Type, LeavePolicyRules.Preview Result)> PreviewAsync(int typeId, string code, DateTime? asOf)
    {
        Guard();
        var type = await repo.FirstOrDefaultAsync(repo.Query<LeaveType>().Where(t => t.Id == typeId)) ?? throw new DomainException("err.notFound");
        var c = (code ?? "").Trim();
        var emp = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e => e.Code == c)) ?? throw new DomainException("err.notFound");
        var pol = await repo.FirstOrDefaultAsync(repo.Query<LeavePolicy>().Where(p => p.LeaveTypeId == typeId)) ?? new LeavePolicy { LeaveTypeId = typeId };
        var tiers = await repo.ToListAsync(repo.Query<LeaveEntitlementTier>().Where(t => t.LeaveTypeId == typeId));
        return (emp, type, LeavePolicyRules.Calculate(pol, tiers, type.DefaultDays, emp.HireDate, emp.BirthDate, asOf ?? clock.Today));
    }
}
