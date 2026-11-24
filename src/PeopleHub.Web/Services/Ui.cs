using Microsoft.AspNetCore.Html;
using PeopleHub.Domain.Entities;

namespace PeopleHub.Web.Services;

/// <summary>Small view helpers: inline SVG icons (no external assets), avatars and status chips.</summary>
public static class Ui
{
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["home"] = "<path d='M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z'/>",
        ["users"] = "<circle cx='9' cy='8' r='3.2'/><path d='M3 20c0-3.3 2.7-6 6-6s6 2.7 6 6'/><circle cx='17' cy='9' r='2.4'/><path d='M17 14c2.5 0 4 1.8 4 4.5'/>",
        ["org"] = "<rect x='9' y='3' width='6' height='5' rx='1'/><rect x='3' y='16' width='6' height='5' rx='1'/><rect x='15' y='16' width='6' height='5' rx='1'/><path d='M12 8v4M6 16v-4h12v4'/>",
        ["clock"] = "<circle cx='12' cy='12' r='9'/><path d='M12 7v5l3 2'/>",
        ["calendar"] = "<rect x='3' y='5' width='18' height='16' rx='2'/><path d='M3 10h18M8 3v4M16 3v4'/>",
        ["inbox"] = "<path d='M3 13h5l1 3h6l1-3h5'/><path d='M5 5h14l2 8v6a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1v-6z'/>",
        ["wallet"] = "<rect x='3' y='6' width='18' height='14' rx='2'/><path d='M3 10h18M16 15h2'/>",
        ["chart"] = "<path d='M4 20V10M10 20V4M16 20v-7M22 20H2'/>",
        ["bell"] = "<path d='M6 17V11a6 6 0 1 1 12 0v6l1.5 2h-15zM10 21h4'/>",
        ["shield"] = "<path d='M12 3 4 6v6c0 4.5 3.3 7.8 8 9 4.7-1.2 8-4.5 8-9V6z'/><path d='m9 12 2 2 4-4'/>",
        ["key"] = "<circle cx='8' cy='15' r='4'/><path d='m11 12 9-9M16 7l3 3'/>",
        ["globe"] = "<circle cx='12' cy='12' r='9'/><path d='M3 12h18M12 3c3 3.3 3 14.7 0 18M12 3c-3 3.3-3 14.7 0 18'/>",
        ["logout"] = "<path d='M9 4H5a1 1 0 0 0-1 1v14a1 1 0 0 0 1 1h4M16 8l4 4-4 4M20 12H9'/>",
        ["plus"] = "<path d='M12 5v14M5 12h14'/>",
        ["check"] = "<path d='m5 12 5 5 9-10'/>",
        ["x"] = "<path d='M6 6l12 12M18 6 6 18'/>",
        ["download"] = "<path d='M12 4v11M7 11l5 5 5-5M5 20h14'/>",
        ["search"] = "<circle cx='11' cy='11' r='6'/><path d='m20 20-4-4'/>",
        ["arrow"] = "<path d='M5 12h14M13 6l6 6-6 6'/>",
        ["file"] = "<path d='M6 3h8l5 5v13H6z'/><path d='M14 3v5h5M9 13h6M9 17h6'/>",
        ["print"] = "<path d='M7 8V3h10v5M7 17H4v-7h16v7h-3M7 14h10v7H7z'/>",
        ["briefcase"] = "<rect x='3' y='7' width='18' height='13' rx='2'/><path d='M9 7V4h6v3M3 13h18'/>",
        ["sun"] = "<circle cx='12' cy='12' r='4'/><path d='M12 3v2M12 19v2M3 12h2M19 12h2M5.6 5.6 7 7M17 17l1.4 1.4M5.6 18.4 7 17M17 7l1.4-1.4'/>",
        ["lock"] = "<rect x='5' y='11' width='14' height='9' rx='2'/><path d='M8 11V8a4 4 0 0 1 8 0v3'/>",
        ["info"] = "<circle cx='12' cy='12' r='9'/><path d='M12 11v5M12 8h.01'/>",
    };

    public static IHtmlContent Icon(string name, int size = 18) =>
        new HtmlString($"<svg class='ic' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round' aria-hidden='true'>{Paths.GetValueOrDefault(name, "")}</svg>");

    private static readonly string[] Hues = ["#6366f1", "#0ea5e9", "#10b981", "#f59e0b", "#ec4899", "#8b5cf6", "#14b8a6", "#ef4444"];

    public static IHtmlContent Avatar(Employee? e, Loc loc, int size = 32)
    {
        if (e == null) return HtmlString.Empty;
        var name = loc.Name(e.FullName, e.FullNameAr);
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var ini = parts.Length > 1 ? $"{parts[0][0]}{parts[1][0]}" : name[..Math.Min(2, name.Length)];
        var hue = Hues[Math.Abs(e.Code.GetHashCode() % Hues.Length)];
        return new HtmlString($"<span class='avatar' style='width:{size}px;height:{size}px;font-size:{size * 0.38:0}px;background:{hue}1f;color:{hue}'>{System.Net.WebUtility.HtmlEncode(ini)}</span>");
    }

    public static string StatusTone(object s) => s.ToString() switch
    {
        "Active" or "Approved" or "Present" or "Paid" or "Finalized" => "green",
        "Pending" or "Draft" or "OnLeave" or "Late" or "HRReviewed" => "amber",
        "Rejected" or "Absent" or "Terminated" or "Suspended" => "red",
        "FinanceApproved" => "blue",
        _ => "gray"
    };

    public static IHtmlContent Chip(string text, string tone) => new HtmlString($"<span class='chip chip-{tone}'>{System.Net.WebUtility.HtmlEncode(text)}</span>");
}
