using Xunit;
namespace PeopleHub.Tests;

/// <summary>D1 style guide: the page, its stylesheet and its open fonts (with licence files) stay in the repo and the page only uses local assets.</summary>
public class DesignAssetsTests
{
    static string Web()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !Directory.Exists(Path.Combine(d.FullName, "src"))) d = d.Parent;
        Assert.NotNull(d); return Path.Combine(d!.FullName, "src", "PeopleHub.Web", "wwwroot");
    }
    [Fact] public void StyleGuideAndFontsAreShippedWithLicences()
    {
        var w = Web();
        foreach (var f in new[] { "design/styleguide.html", "design/guide.css", "fonts/Inter-OFL.txt", "fonts/IBM-Plex-Sans-Arabic-OFL.txt", "fonts/inter-latin-wght-normal.woff2", "fonts/ibm-plex-sans-arabic-arabic-400-normal.woff2" })
            Assert.True(File.Exists(Path.Combine(w, f)), f);
    }
    [Fact] public void StyleGuideLoadsNothingFromOutsideThisSite()
    {
        var w = Web();
        foreach (var f in new[] { "design/styleguide.html", "design/guide.css" })
            Assert.DoesNotMatch(@"(src|href)=""https?://|url\(\s*['""]?https?://|@import", File.ReadAllText(Path.Combine(w, f)));
    }
    [Fact] public void StyleGuideHasAllThreePalettesDarkModeAndBothLanguages()
    {
        var w = Web(); var css = File.ReadAllText(Path.Combine(w, "design/guide.css")); var html = File.ReadAllText(Path.Combine(w, "design/styleguide.html"));
        foreach (var p in new[] { "A", "B", "C" }) { Assert.Contains($"[data-palette={p}]", css); Assert.Contains($"[data-theme=dark][data-palette={p}]", css); }
        Assert.Contains("data-ar=", html); Assert.Contains("dir=\"ltr\"", html); Assert.Contains(":focus-visible", css); Assert.Contains("prefers-reduced-motion", css);
    }
}
