using PeopleHub.Domain.Entities;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PeopleHub.Web.Services;

/// <summary>Tiny dictionary-based localizer (en / ar). Keeps the demo free of resource-file tooling.</summary>
public partial class Loc
{
    public bool IsAr => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    public string this[string key] => Map.TryGetValue(key, out var v) ? (IsAr ? v.ar : v.en) : key;
    public string Name(string en, string ar) => IsAr && !string.IsNullOrWhiteSpace(ar) ? ar : en;
    public string RoleName(string code) =>
        PeopleHub.Domain.Entities.PermissionCache.NameOf(code) is { } n ? (IsAr && !string.IsNullOrWhiteSpace(n.Ar) ? n.Ar : n.En) : this["role." + code];
    /// <summary>Label of an approval step token: "Manager" or one or more role codes joined by |.</summary>
    public string Stage(string token) => token=="DepartmentManager"?(IsAr?"مدير القسم عند تقديم الطلب":"Department manager at submission"):ApprovalStage.EmployeeId(token) is {} id?(IsAr?$"معتمد القسم المسجل #{id}":$"Recorded department approver #{id}"):token == "Manager" ? this["stage.Manager"] : string.Join(" / ", token.Split('|').Select(RoleName));
    public string Date(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public string Money(decimal m) => IsAr ? $"{m:N0} ج.م" : $"EGP {m:N0}";

    /// <summary>Label for an enum value or a status/action word. Falls back to a spaced version of the name.</summary>
    public string E(object value)
    {
        var s = value.ToString() ?? "";
        return Map.TryGetValue("enum." + s, out var v) ? (IsAr ? v.ar : v.en) : Regex.Replace(s, "(?<=[a-z])(?=[A-Z])", " ");
    }

    public string Col(string c) => Map.TryGetValue("col." + c, out var v) ? (IsAr ? v.ar : v.en) : c;
    /// <summary>Report cells that hold a known status word are translated; everything else is shown as is.</summary>
    public string Cell(string c) => Map.ContainsKey("enum." + c) ? E(c) : c;
}
