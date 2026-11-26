using System.Text.RegularExpressions;
using Xunit;
namespace PeopleHub.Tests.Architecture;

/// <summary>Module boundary ratchet (ADR-0006). Application modules may only reference each other along the edges recorded here.
/// The list is the current dependency graph, measured from the source. It can shrink; a new edge fails the gate until someone
/// decides, in the open, to add it (or routes the call through an interface in Application/Common or a domain event).
/// Known cycles to remove: Attendance-Requests, Attendance-Platform, Payroll-Reporting.</summary>
public class ModuleBoundaryTests
{
    static readonly Dictionary<string, string[]> Allowed = new()
    {
        ["Attendance"] = ["Organization", "Platform", "Requests"],
        ["Leave"] = ["Organization", "Requests"],
        ["Employees"] = ["Platform"],
        ["Identity"] = ["Platform"], // sign-in throttling reads Settings values and recovery-code hashing (decided 2026-11-26); Platform does not reference Identity, so no cycle
        ["Payroll"] = ["Attendance", "Reporting"],
        ["Platform"] = ["Attendance"],
        ["Relations"] = ["Platform"],
        ["Reporting"] = ["Attendance", "Employees", "Leave", "Payroll", "Requests"],
        ["Requests"] = ["Attendance", "Platform"],
        ["Talent"] = ["Payroll", "Platform"],
    };
    static string Root()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !Directory.Exists(Path.Combine(d.FullName, "src", "PeopleHub.Application", "Modules"))) d = d.Parent;
        Assert.NotNull(d); return Path.Combine(d!.FullName, "src", "PeopleHub.Application", "Modules");
    }
    static readonly Regex Use = new(@"^using PeopleHub\.Application\.Modules\.(\w+)", RegexOptions.Multiline);

    [Fact] public void ModulesOnlyReferenceEachOtherAlongRecordedEdges()
    {
        var violations = new List<string>();
        foreach (var dir in Directory.GetDirectories(Root()))
        {
            var module = Path.GetFileName(dir);
            var allowed = Allowed.GetValueOrDefault(module) ?? [];
            foreach (var f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
                foreach (Match m in Use.Matches(File.ReadAllText(f)))
                {
                    var target = m.Groups[1].Value;
                    if (target != module && !allowed.Contains(target)) violations.Add($"{module} -> {target} ({Path.GetFileName(f)})");
                }
        }
        Assert.True(violations.Count == 0, "New module dependency: " + string.Join("; ", violations.Distinct()));
    }
    [Fact] public void EveryModuleFolderHasNamespaceMatchingItsFolder()
    {
        var bad = new List<string>();
        foreach (var dir in Directory.GetDirectories(Root()))
            foreach (var f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                var ns = Regex.Match(File.ReadAllText(f), @"^namespace\s+([\w.]+)", RegexOptions.Multiline).Groups[1].Value;
                if (ns != $"PeopleHub.Application.Modules.{Path.GetFileName(dir)}") bad.Add($"{Path.GetFileName(f)}: {ns}");
            }
        Assert.True(bad.Count == 0, string.Join("; ", bad));
    }
}
