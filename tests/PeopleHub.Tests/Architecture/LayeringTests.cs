using System.Reflection;
using PeopleHub.Application.Common.Tenancy;
using PeopleHub.Domain.Entities;
using PeopleHub.Infrastructure.Persistence;
using Xunit;
namespace PeopleHub.Tests.Architecture;

/// <summary>Architecture tests (ADR-0006, ADR-0018): the dependency direction Web -> Infrastructure -> Application -> Domain
/// is enforced by tests, not by convention. A violation fails the build gate.</summary>
public class LayeringTests
{
    static readonly Assembly Domain = typeof(Employee).Assembly;
    static readonly Assembly Application = typeof(ITenantContext).Assembly;
    static readonly Assembly Infrastructure = typeof(AppDbContext).Assembly;
    static readonly Assembly Web = typeof(PeopleHub.Web.Services.HttpTenantContext).Assembly;
    static string[] Refs(Assembly a) => a.GetReferencedAssemblies().Select(x => x.Name!).Where(n => n.StartsWith("PeopleHub")).ToArray();

    [Fact] public void DomainDependsOnNothingInTheSolution() => Assert.Empty(Refs(Domain));
    [Fact] public void ApplicationDependsOnDomainOnly() => Assert.Equal(["PeopleHub.Domain"], Refs(Application).Distinct().Order());
    [Fact] public void InfrastructureDoesNotReferenceWeb() => Assert.DoesNotContain("PeopleHub.Web", Refs(Infrastructure));
    [Fact] public void ApplicationHasNoEntityFrameworkDependency() =>
        Assert.DoesNotContain(Application.GetReferencedAssemblies(), a => a.Name!.StartsWith("Microsoft.EntityFrameworkCore"));
    [Fact] public void DomainHasNoFrameworkDependencies() =>
        Assert.DoesNotContain(Domain.GetReferencedAssemblies(), a => a.Name!.StartsWith("Microsoft.EntityFrameworkCore") || a.Name.StartsWith("Microsoft.AspNetCore"));
    [Fact] public void PlatformContractsLiveInApplicationCommon() =>
        Assert.All(new[] { typeof(ITenantContext), typeof(PeopleHub.Application.Common.Caching.ICache), typeof(PeopleHub.Application.Common.Files.IFileStore) },
            t => Assert.StartsWith("PeopleHub.Application.Common.", t.Namespace));
    [Fact] public void WebReferencesApplication() => Assert.Contains("PeopleHub.Application", Refs(Web));
}
