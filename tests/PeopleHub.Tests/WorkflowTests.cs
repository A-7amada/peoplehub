using PeopleHub.Domain.Entities;
using Xunit;

namespace PeopleHub.Tests;

public class WorkflowTests
{
    private static WorkflowStep Mgr(int o = 1) => new() { Order = o, Approver = ApproverType.DirectManager };
    private static WorkflowStep Role(string r, int o, decimal? t = null) => new() { Order = o, Approver = ApproverType.Role, RoleCode = r, Threshold = t };
    private static readonly Employee WithManager = new() { ManagerId = 5 };
    private static readonly Employee NoManager = new();

    [Fact] public void Route_FollowsConfiguredOrder() =>
        Assert.Equal("Manager,PayrollOfficer,FinanceManager", RoutePlanner.Build([Role("FinanceManager", 3), Mgr(1), Role("PayrollOfficer", 2)], WithManager, 1));

    [Fact] public void ManagerStep_SkippedWithoutManager() =>
        Assert.Equal("HRManager", RoutePlanner.Build([Mgr(), Role("HRManager", 2)], NoManager, 1));

    [Fact] public void Threshold_StepAppliesOnlyAboveLimit()
    {
        var steps = new[] { Mgr(), Role("HRManager", 2, 2) };
        Assert.Equal("Manager", RoutePlanner.Build(steps, WithManager, 2));
        Assert.Equal("Manager,HRManager", RoutePlanner.Build(steps, WithManager, 3));
    }

    [Fact] public void AlwaysEscalate_OverridesThreshold() =>
        Assert.Equal("Manager,HRManager", RoutePlanner.Build([Mgr(), Role("HRManager", 2, 10)], WithManager, 1, alwaysEscalate: true));

    [Fact] public void EmptyRoute_FallsBackToFirstRoleStep() =>
        Assert.Equal("FinanceManager", RoutePlanner.Build([Role("FinanceManager", 1, 1000)], WithManager, 5));

    [Fact] public void MultiRoleStep_KeepsAllRoles() =>
        Assert.Equal("Manager,HRManager|HRSpecialist", RoutePlanner.Build([Mgr(), new WorkflowStep { Order = 2, Approver = ApproverType.Role, RoleCode = "HRManager|HRSpecialist" }], WithManager, 1));

    [Fact] public void Permissions_AreRoleDrivenAndAdminHasAll()
    {
        PermissionCache.Load([new Role { Code = "TeamLead", Name = "Lead", NameAr = "قائد" }],
            [new RolePermission { RoleCode = "TeamLead", Permission = Perm.Reports }]);
        Assert.True(PermissionCache.Has(["TeamLead"], Perm.Reports));
        Assert.False(PermissionCache.Has(["TeamLead"], Perm.PayFinance));
        Assert.True(PermissionCache.Has(["SystemAdmin"], Perm.AdmFlows));
        Assert.True(PermissionCache.Has(["Employee", "TeamLead"], Perm.Reports));
    }

    [Fact] public void Approver_RoleChangeIsReflectedInRoute()
    {
        var before = RoutePlanner.Build([Role("HRManager", 1)], WithManager, 1);
        var after = RoutePlanner.Build([Role("TopManagement", 1)], WithManager, 1);
        Assert.NotEqual(before, after);
    }
}
