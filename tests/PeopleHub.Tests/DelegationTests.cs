using PeopleHub.Domain.Entities;
using Xunit;
namespace PeopleHub.Tests;
public class DelegationTests
{
    [Fact] public void ActiveInsideRange() => Assert.True(new Delegation { StartDate = new DateTime(2026,1,1), EndDate = new DateTime(2026,1,10) }.IsActiveOn(new DateTime(2026,1,5)));
    [Fact] public void InactiveOutsideRange() => Assert.False(new Delegation { StartDate = new DateTime(2026,1,1), EndDate = new DateTime(2026,1,10) }.IsActiveOn(new DateTime(2026,1,11)));
    [Fact] public void InactiveWhenRevoked() => Assert.False(new Delegation { StartDate = new DateTime(2026,1,1), EndDate = new DateTime(2026,1,10), Revoked = true }.IsActiveOn(new DateTime(2026,1,5)));
}
