using System.Diagnostics;
namespace PeopleHub.Application.Services;
public record RouteMetric(string Route,long Requests,long Errors,long InFlight,double MeanMilliseconds,double MaxMilliseconds);
public class RequestMetrics
{
 private sealed class Counter{public long Count,Errors,InFlight;public double Total,Max;}
 private readonly object gate=new();private readonly Dictionary<string,Counter> routes=[];
 public long Begin(string route){lock(gate){if(!routes.ContainsKey(route))routes[route]=new();routes[route].InFlight++;}return Stopwatch.GetTimestamp();}
 public void End(string route,long started,int status){var elapsed=Stopwatch.GetElapsedTime(started).TotalMilliseconds;lock(gate){if(!routes.TryGetValue(route,out var c))return;c.InFlight=Math.Max(0,c.InFlight-1);c.Count++;if(status>=500)c.Errors++;c.Total+=elapsed;c.Max=Math.Max(c.Max,elapsed);}}
 public List<RouteMetric> Snapshot(){lock(gate)return routes.Select(x=>new RouteMetric(x.Key,x.Value.Count,x.Value.Errors,x.Value.InFlight,x.Value.Count==0?0:Math.Round(x.Value.Total/x.Value.Count,2),Math.Round(x.Value.Max,2))).OrderByDescending(x=>x.Requests).ToList();}
}
