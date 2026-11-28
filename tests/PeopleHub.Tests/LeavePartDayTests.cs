using PeopleHub.Domain.Entities;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>ADR-0022 unit 3b: half-day and hourly leave requests.</summary>
public class LeavePartDayTests
{
    static readonly Employee Emp = new() { Id = 1, ManagerId = 9, Status = EmployeeStatus.Active };
    static readonly LeaveType Annual = new() { Id = 1, Code = "ANN", Name = "Annual", DefaultDays = 21 };
    static readonly DateTime Today = new(2026, 11, 27);
    static readonly DateTime Mon = new(2026, 11, 30), Tue = new(2026, 12, 1);
    static LeaveBalance Bal(decimal rem) => new() { Entitled = rem };
    static LeaveRequest Make(DateTime s, DateTime e, string? part = null, decimal? hours = null, bool half = true, bool hourly = true, decimal bal = 10, int hpd = 8) =>
        LeaveRequest.Create(Emp, Annual, Bal(bal), s, e, null, null, Today, null, part, hours, half, hourly, hpd);

    [Fact] public void HalfDayCountsAsHalfADay() { var r = Make(Mon, Mon, "AM"); Assert.Equal(0.5m, r.Days); Assert.Equal("AM", r.PartOfDay); Assert.Null(r.Hours); }
    [Fact] public void HourlyIsHoursOverHoursPerDay_ExactToFourDecimals()
    { Assert.Equal(0.125m, Make(Mon, Mon, null, 1).Days); Assert.Equal(0.375m, Make(Mon, Mon, null, 3).Days); Assert.Equal(0.4286m, Make(Mon, Mon, null, 3, hpd: 7).Days); }
    [Fact] public void HalfDayNeedsTheBalanceItUses() { Assert.Throws<DomainException>(() => Make(Mon, Mon, "PM", null, bal: 0.25m)); Assert.Equal(0.5m, Make(Mon, Mon, "PM", null, bal: 0.5m).Days); }
    [Fact] public void PartDayIsRefusedWhenThePolicyDoesNotAllowIt()
    { Assert.Throws<DomainException>(() => Make(Mon, Mon, "AM", half: false)); Assert.Throws<DomainException>(() => Make(Mon, Mon, null, 2, hourly: false)); }
    [Theory] [InlineData("XX", null)] [InlineData("AM", 2.0)] [InlineData(null, 0.0)] [InlineData(null, 8.0)] [InlineData(null, 9.0)] [InlineData(null, 1.5)]
    public void InvalidPartDayInputIsRefused(string? part, double? hours) => Assert.Throws<DomainException>(() => Make(Mon, Mon, part, hours == null ? null : (decimal)hours));
    [Fact] public void PartDayMustBeASingleWorkingDay()
    { Assert.Throws<DomainException>(() => Make(Mon, Tue, "AM")); Assert.Throws<DomainException>(() => Make(new(2026, 11, 28), new(2026, 11, 28), "AM")); }
    [Fact] public void WholeDaysAreUnchanged() { Assert.Equal(2, Make(Mon, Tue).Days); Assert.Equal(1, Make(Mon, Mon, "Full").Days); }

    static LeaveRequest Saved(LeaveRequest r, int id) { r.Id = id; return r; }
    [Fact] public void MorningAndAfternoonOfOneDayCoexist_ButNotTwoMornings_AndNotOverAWholeDay()
    {
        var am = Saved(Make(Mon, Mon, "AM"), 1); var pm = Make(Mon, Mon, "PM"); var am2 = Make(Mon, Mon, "AM");
        Assert.False(am.Overlaps(pm)); Assert.True(am.Overlaps(am2));
        Assert.True(Make(Mon, Mon).Overlaps(am)); // a whole day overlaps any part of it
        Assert.True(Make(Mon, Mon, null, 5).Overlaps(Saved(Make(Mon, Mon, null, 4), 2))); // 9/8 > 1 day
        Assert.False(Make(Mon, Mon, null, 3).Overlaps(Saved(Make(Mon, Mon, null, 4), 3)));
    }

    [Fact] public void ApplyAndCancelMoveTheBalanceByTheFraction()
    {
        var b = Bal(10); var r = Make(Mon, Mon, null, 3); r.LeaveType = Annual; r.ApplyToBalance(b);
        Assert.Equal(0.375m, b.Used); Assert.Equal(9.625m, b.Remaining);
        r.Status = RequestStatus.Approved; r.CancelApproved(b, Today, DateTime.UtcNow);
        Assert.Equal(0m, b.Used);
    }
}
