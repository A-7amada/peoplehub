using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Security;
using Xunit;

namespace PeopleHub.Tests;

public class LeaveTests
{
    private static readonly DateTime Today = new(2026, 11, 22); // Sunday
    private static Employee Emp(int balance = 21) => new() { Id = 1, FullName = "A", Email = "a@b.c", AnnualLeaveBalance = balance };

    [Fact]
    public void Weekend_days_are_not_counted()
    {
        // Sun 22 Nov to Sat 28 Nov: Fri and Sat excluded => 5 working days
        Assert.Equal(5, LeaveRequest.CountWorkingDays(new(2026, 11, 22), new(2026, 11, 28)));
    }

    [Fact]
    public void Annual_leave_over_balance_is_rejected() =>
        Assert.Equal("err.balance", Assert.Throws<DomainException>(() =>
            LeaveRequest.Create(Emp(2), LeaveType.Annual, Today, Today.AddDays(4), null, Today)).Message);

    [Fact]
    public void Approval_deducts_balance_and_cancel_restores_it()
    {
        var e = Emp(10);
        var l = LeaveRequest.Create(e, LeaveType.Annual, Today, Today.AddDays(2), null, Today);
        l.Approve(e);
        Assert.Equal(7, e.AnnualLeaveBalance);
        l.Cancel(e);
        Assert.Equal(10, e.AnnualLeaveBalance);
    }

    [Fact]
    public void Sick_leave_does_not_use_annual_balance()
    {
        var e = Emp(5);
        var l = LeaveRequest.Create(e, LeaveType.Sick, Today, Today.AddDays(1), null, Today);
        l.Approve(e);
        Assert.Equal(5, e.AnnualLeaveBalance);
    }

    [Fact]
    public void Cannot_decide_twice()
    {
        var e = Emp();
        var l = LeaveRequest.Create(e, LeaveType.Unpaid, Today, Today, null, Today);
        l.Reject();
        Assert.Throws<DomainException>(() => l.Approve(e));
    }

    [Fact]
    public void Overlapping_requests_are_detected()
    {
        var e = Emp();
        var a = LeaveRequest.Create(e, LeaveType.Annual, Today, Today.AddDays(2), null, Today);
        var b = LeaveRequest.Create(e, LeaveType.Annual, Today.AddDays(2), Today.AddDays(3), null, Today);
        Assert.True(b.Overlaps(a));
    }

    [Fact]
    public void Password_hash_roundtrip()
    {
        var h = PasswordHasher.Hash("secret1");
        Assert.True(PasswordHasher.Verify("secret1", h));
        Assert.False(PasswordHasher.Verify("nope", h));
    }
}
