using PeopleHub.Application.Modules.Platform;
using Xunit;
namespace PeopleHub.Tests;

/// <summary>ADR-0023 stage 4: settings are cached per company.</summary>
public class TenantSettingsTests
{
    static void With(int tenant, Action a) => SettingsService.RunAs(tenant, a);

    [Fact] public void OneCompanysValueNeverReachesAnother()
    {
        const int a = 9101, b = 9102;
        try
        {
            SettingsService.Cache.Set(a, "pwd.minLength", "12");
            With(a, () => Assert.Equal("12", SettingsService.Get("pwd.minLength")));
            With(b, () => Assert.Equal(SettingsService.Defaults["pwd.minLength"], SettingsService.Get("pwd.minLength")));
            SettingsService.Cache.Set(b, "pwd.minLength", "7");
            With(a, () => Assert.Equal("12", SettingsService.Get("pwd.minLength")));
            With(b, () => Assert.Equal("7", SettingsService.Get("pwd.minLength")));
        }
        finally { SettingsService.Cache.Reset(a); SettingsService.Cache.Reset(b); }
    }

    [Fact] public void FirstUseReadsThatCompanysSavedValuesOnce_AndResetForgetsThem()
    {
        const int c = 9103; var loads = 0; var oldLoader = SettingsService.Loader;
        SettingsService.Loader = t => { if (t == c) loads++; return t == c ? new Dictionary<string, string> { ["auth.maxFailed"] = "9" } : new(); };
        try
        {
            With(c, () => { Assert.Equal(9, SettingsService.GetInt("auth.maxFailed")); Assert.Equal(9, SettingsService.GetInt("auth.maxFailed")); });
            Assert.Equal(1, loads);
            SettingsService.Cache.Reset(c);
            With(c, () => Assert.Equal(9, SettingsService.GetInt("auth.maxFailed")));
            Assert.Equal(2, loads);
        }
        finally { SettingsService.Loader = oldLoader; SettingsService.Cache.Reset(c); }
    }

    [Fact] public void ResetLinkCarriesTheCompanyOnlyWhenGiven()
    {
        var m = typeof(AccountService).GetMethod("RequestResetAsync")!;
        Assert.Contains(m.GetParameters(), p => p.Name == "company" && p.IsOptional);
    }
}
