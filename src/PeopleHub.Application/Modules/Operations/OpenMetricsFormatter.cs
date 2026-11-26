using System.Globalization;
using System.Text;
namespace PeopleHub.Application.Modules.Operations;

/// <summary>Renders request metrics in the OpenMetrics / Prometheus text format, which an OpenTelemetry Collector (prometheus receiver) or any Prometheus-compatible scraper reads.
/// No new package and no paid service: the in-process counters are the source. Names follow the OpenTelemetry HTTP semantic-convention style with a peoplehub_ prefix.</summary>
public static class OpenMetricsFormatter
{
    public static string Render(IEnumerable<RouteMetric> routes)
    {
        var rs = routes.OrderBy(r => r.Route, StringComparer.Ordinal).ToList();
        var sb = new StringBuilder();
        Family(sb, "peoplehub_http_server_requests", "counter", "Completed requests per route.", rs, r => r.Requests, "_total");
        Family(sb, "peoplehub_http_server_errors", "counter", "Requests per route that ended with status 500 or higher.", rs, r => r.Errors, "_total");
        Family(sb, "peoplehub_http_server_active_requests", "gauge", "Requests in flight per route.", rs, r => r.InFlight, "");
        Family(sb, "peoplehub_http_server_duration_mean_milliseconds", "gauge", "Mean request duration per route.", rs, r => r.MeanMilliseconds, "");
        Family(sb, "peoplehub_http_server_duration_max_milliseconds", "gauge", "Slowest request duration per route.", rs, r => r.MaxMilliseconds, "");
        sb.Append("# EOF\n");
        return sb.ToString();
    }

    static void Family(StringBuilder sb, string name, string type, string help, List<RouteMetric> rs, Func<RouteMetric, double> value, string suffix)
    {
        sb.Append("# HELP ").Append(name).Append(' ').Append(help).Append('\n');
        sb.Append("# TYPE ").Append(name).Append(' ').Append(type).Append('\n');
        foreach (var r in rs)
            sb.Append(name).Append(suffix).Append("{route=\"").Append(Escape(r.Route)).Append("\"} ").Append(value(r).ToString("0.##", CultureInfo.InvariantCulture)).Append('\n');
    }

    static string Escape(string v) => v.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
}
