using PeopleHub.Application.Modules.Leave;
using PeopleHub.Domain.Entities;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>ADR-0022 unit 3c: monthly accrual and carry-forward expiry. Defaults are illustrative, not legally validated.</summary>
public class LeaveAccrualExpiryTests
{
    static LeavePolicy Pol(string accrual = "Monthly", int expiry = 3, bool enabled = true, int wait = 0) => new() { Enabled = enabled, Accrual = accrual, CarryExpiryMonths = expiry, WaitingMonths = wait };
    static readonly List<LeaveEntitlementTier> NoTiers = [];

    [Fact] public void MonthlyAccrualGrowsWithFullMonths_AndNeverExceedsTheYearGrant()
    {
        var hire = new DateTime(2020, 1, 1);
        Assert.Equal(0m, LeavePolicyRules.MonthlyAccrued(Pol(), NoTiers, 24, hire, null, new(2026, 1, 1)));
        Assert.Equal(2m, LeavePolicyRules.MonthlyAccrued(Pol(), NoTiers, 24, hire, null, new(2026, 2, 1)));
        Assert.Equal(12m, LeavePolicyRules.MonthlyAccrued(Pol(), NoTiers, 24, hire, null, new(2026, 7, 1)));
        Assert.True(LeavePolicyRules.MonthlyAccrued(Pol(), NoTiers, 24, hire, null, new(2026, 12, 31)) <= 24m);
    }
    [Fact] public void MonthlyAccrualIsNullWhenOffOrAnnual_AndZeroBeforeEligibility()
    {
        var hire = new DateTime(2026, 3, 10);
        Assert.Null(LeavePolicyRules.MonthlyAccrued(Pol(enabled: false), NoTiers, 21, hire, null, new(2026, 9, 1)));
        Assert.Null(LeavePolicyRules.MonthlyAccrued(Pol("Annual"), NoTiers, 21, hire, null, new(2026, 9, 1)));
        Assert.Null(LeavePolicyRules.MonthlyAccrued(null, NoTiers, 21, hire, null, new(2026, 9, 1)));
        Assert.Equal(0m, LeavePolicyRules.MonthlyAccrued(Pol(wait: 6), NoTiers, 21, hire, null, new(2026, 8, 1)));
        Assert.Equal(0m, LeavePolicyRules.MonthlyAccrued(Pol(), NoTiers, 21, new(2027, 1, 1), null, new(2026, 8, 1)));
    }
    [Fact] public void CarryLapsesOnlyAfterTheExpiryMonthsAndOnlyTheUnusedPart()
    {
        Assert.Equal(0m, LeavePolicyRules.ExpiredCarry(Pol(), 5, 0, 2026, new(2026, 3, 31)));
        Assert.Equal(5m, LeavePolicyRules.ExpiredCarry(Pol(), 5, 0, 2026, new(2026, 4, 1)));
        Assert.Equal(2m, LeavePolicyRules.ExpiredCarry(Pol(), 5, 3, 2026, new(2026, 6, 1)));
        Assert.Equal(0m, LeavePolicyRules.ExpiredCarry(Pol(), 5, 9, 2026, new(2026, 6, 1)));
    }
    [Fact] public void CarryNeverLapsWhenExpiryIsZero_OrPolicyOff_OrNothingCarried()
    {
        Assert.Equal(0m, LeavePolicyRules.ExpiredCarry(Pol(expiry: 0), 5, 0, 2026, new(2026, 12, 1)));
        Assert.Equal(0m, LeavePolicyRules.ExpiredCarry(Pol(enabled: false), 5, 0, 2026, new(2026, 12, 1)));
        Assert.Equal(0m, LeavePolicyRules.ExpiredCarry(null, 5, 0, 2026, new(2026, 12, 1)));
        Assert.Equal(0m, LeavePolicyRules.ExpiredCarry(Pol(), 0, 0, 2026, new(2026, 12, 1)));
    }
}
