using System.Collections.Concurrent;
using System.Net.Sockets;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Domain.Security;

using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
namespace PeopleHub.Application.Modules.Platform;

/// <summary>Key/value settings stored in the database, with a process-wide cache for synchronous reads.</summary>
public class SettingsService(IRepository repo, ICurrentUser me, AuditService audit)
{
    public static readonly ConcurrentDictionary<string, string> Cache = new();
    public static readonly Dictionary<string, string> Defaults = new()
    {
        ["att.mode"] = "System", ["zk.ip"] = "192.168.1.201", ["zk.port"] = "4370", ["zk.simulate"] = "true", ["zk.apiKey"] = "demo-device-key",
        ["email.notify"] = "true", ["smtp.host"] = "", ["smtp.port"] = "587", ["smtp.user"] = "", ["smtp.from"] = "hr@peoplehub.local",
        ["eosb.days"] = "15", ["pwd.minLength"] = "8",
        ["auth.maxFailed"] = "5", ["auth.lockMinutes"] = "15", ["auth.failWindowMinutes"] = "15", ["auth.rateLimitPerMinute"] = "12"
    };
    public static string Get(string key) => Cache.TryGetValue(key, out var v) ? v : Defaults.GetValueOrDefault(key, "");
    /// <summary>Sign-in throttling values. Defaults equal the values that were hardcoded before (5 failures, 15-minute window, 15-minute lock, 12 sign-in requests per minute per address). Out-of-range or unreadable values fall back to the default.</summary>
    public static readonly Dictionary<string, (int Min, int Max)> AuthRanges = new() { ["auth.maxFailed"] = (3, 20), ["auth.lockMinutes"] = (1, 1440), ["auth.failWindowMinutes"] = (1, 1440), ["auth.rateLimitPerMinute"] = (3, 120) };
    public static int GetInt(string key)
    {
        var def = int.Parse(Defaults[key]);
        if (!int.TryParse(Get(key), out var v)) return def;
        return AuthRanges.TryGetValue(key, out var r) && (v < r.Min || v > r.Max) ? def : v;
    }
    public static bool DeviceMode => Get("att.mode") == "Device";

    public async Task LoadAsync()
    {
        foreach (var s in await repo.ToListAsync(repo.Query<AppSetting>())) Cache[s.Key] = s.Value;
    }

    public async Task<Dictionary<string, string>> AllAsync()
    {
        await LoadAsync();
        return Defaults.ToDictionary(d => d.Key, d => Get(d.Key));
    }

    public async Task SaveAsync(Dictionary<string, string> values)
    {
        if (!me.Can(Perm.AdmSettings)) throw new DomainException("err.forbidden");
        var mode = values.GetValueOrDefault("att.mode", "System"); if (mode is not ("System" or "Device")) throw new DomainException("err.generic");
        if (mode == "Device")
        {
            if (!System.Net.IPAddress.TryParse(values.GetValueOrDefault("zk.ip", ""), out _) && Uri.CheckHostName(values.GetValueOrDefault("zk.ip", "")) == UriHostNameType.Unknown) throw new DomainException("err.badIp");
            if (!int.TryParse(values.GetValueOrDefault("zk.port", ""), out var p) || p is < 1 or > 65535) throw new DomainException("err.badPort");
        }
        foreach (var (k, r) in AuthRanges)
            if (values.TryGetValue(k, out var raw) && (!int.TryParse(raw, out var n) || n < r.Min || n > r.Max)) throw new DomainException("err.badAuthSetting");
        var rows = await repo.ToListAsync(repo.Query<AppSetting>());
        foreach (var (k, v) in values)
        {
            if (!Defaults.ContainsKey(k)) continue;
            var row = rows.FirstOrDefault(r => r.Key == k);
            if (row == null) repo.Add(new AppSetting { Key = k, Value = v }); else row.Value = v;
            Cache[k] = v;
        }
        audit.Log("Update", "Settings", "attendance/email/payroll", $"att.mode={mode}");
        await repo.SaveAsync();
    }
}

/// <summary>ZKTeco attendance device integration. The Render demo cannot reach a private LAN address, so the real path is a bridge that pushes punches to the API; a simulator stands in for the device in the demo.</summary>
public class DeviceService(IRepository repo, AppClock clock, AuditService audit, ICurrentUser me)
{
    public record PunchIn(string UserId, DateTime Time);

    public async Task<(bool Ok, string Message)> PingAsync()
    {
        var ip = SettingsService.Get("zk.ip"); int.TryParse(SettingsService.Get("zk.port"), out var port);
        if (SettingsService.Get("zk.simulate") == "true") return (true, $"simulated device at {ip}:{port}");
        try
        {
            using var c = new TcpClient(); var t = c.ConnectAsync(ip, port); 
            if (await Task.WhenAny(t, Task.Delay(2500)) != t || !c.Connected) return (false, $"no response from {ip}:{port} within 2.5s");
            return (true, $"{ip}:{port} accepted a TCP connection");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    /// <summary>Stores punches (from the push API or the simulator), then turns them into attendance records.</summary>
    public async Task<int> IngestAsync(IEnumerable<PunchIn> punches)
    {
        int added=0;await repo.AtomicAsync(async()=>added=await IngestCoreAsync(punches));return added;
    }
    private async Task<int> IngestCoreAsync(IEnumerable<PunchIn> punches)
    {
        var emps = await repo.ToListAsync(repo.Query<Employee>());
        var byCode = emps.ToDictionary(e => e.Code, StringComparer.OrdinalIgnoreCase);
        var existing = (await repo.ToListAsync(repo.Query<DevicePunch>())).Select(p => (p.DeviceUserId.Trim().ToUpperInvariant(), p.Time)).ToHashSet();
        var added = 0;
        foreach (var p in punches)
        {
            if(p==null) throw new DomainException("err.required");
            var code=(p.UserId??"").Trim().ToUpperInvariant();
            if(string.IsNullOrEmpty(code) || code.Length>100 || p.Time.Year<2000 || p.Time.Year>9998) throw new DomainException("err.required");
            if (!existing.Add((code, p.Time))) continue;
            var emp = byCode.GetValueOrDefault(code);
            var key=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{code}|{p.Time.Ticks}")));
            repo.Add(new DevicePunch { DeviceUserId = code, Time = p.Time, EmployeeId = emp?.Id, Matched = emp != null,IdempotencyKey=key,ReceivedAt=clock.UtcNow }); added++;
        }
        await repo.SaveAsync();
        await RebuildAsync(emps);
        return added;
    }

    public Task ReprocessAsync()=>repo.AtomicAsync(async()=>{if(!me.Can(Perm.AttManage))throw new DomainException("err.forbidden");await RebuildAsync(await repo.ToListAsync(repo.Query<Employee>()));audit.Log("Reprocess","Attendance", "retained punches", "Explicit reprocessing, manual and locked records preserved");await repo.SaveAsync();});

    /// <summary>First punch of the day is the check-in, the last (at least a minute later) the check-out. Records already approved by hand are not overwritten.</summary>
    private async Task RebuildAsync(List<Employee> emps)
    {
        var raw=await repo.ToListAsync(repo.Query<DevicePunch>());
        var assignments=await repo.ToListAsync(repo.Query<EmployeeAssignment>());
        var overrides=await repo.ToListAsync(repo.Query<EmployeeShiftDay>());
        var shifts = await repo.ToListAsync(repo.Query<Shift>().OrderBy(s => s.Id));
        Shift For(Employee e,DateTime d)=>shifts.FirstOrDefault(s=>s.Id==ShiftCalendar.ShiftId(e,d,assignments,overrides))??shifts.FirstOrDefault()??new Shift{Start=new(9,0,0),End=new(17,0,0)};
        DateTime Day(DevicePunch p){var e=emps.First(e=>e.Id==p.EmployeeId);var previous=For(e,p.Time.Date.AddDays(-1));return ShiftCalendar.WorkDate(p.Time,previous);}
        var punches=raw.Where(p=>p.EmployeeId!=null && emps.Any(e=>e.Id==p.EmployeeId)).GroupBy(p=>(p.EmployeeId!.Value,Day(p)));
        var recs = await repo.ToListAsync(repo.Query<AttendanceRecord>());
        var locks=await repo.ToListAsync(repo.Query<AttendancePeriodLock>());
        foreach (var g in punches)
        {
            var (eid, day) = g.Key; if(locks.Any(x=>x.EmployeeId==eid && x.Year==day.Year && x.Month==day.Month)) continue; var times = g.Select(p => p.Time).OrderBy(t => t).ToList();
            var rec = recs.FirstOrDefault(r => r.EmployeeId == eid && r.Date == day);
            if (rec != null && (rec.IsExcused || (rec.Source == AttendanceSource.Manual && rec.CheckIn != null))) continue;
            var emp = emps.First(e => e.Id == eid);
            emp.LeaveRequestStamp=Guid.NewGuid().ToString("N");
            var isNew = rec == null; rec ??= new AttendanceRecord { EmployeeId = eid, Date = day };
            var beforeIn=rec.CheckIn;var beforeOut=rec.CheckOut;
            rec.CheckIn = times[0]; rec.CheckOut = times.Count > 1 && times[^1] > times[0].AddMinutes(1) ? times[^1] : null; rec.Source = AttendanceSource.Fingerprint;
            rec.Recompute(For(emp,day));
            if (isNew) { repo.Add(rec); recs.Add(rec); }
            if(isNew || beforeIn!=rec.CheckIn || beforeOut!=rec.CheckOut) repo.Add(AttendanceEvidence.Capture(rec,"DeviceProjection",me.Email,clock.UtcNow,"Derived from retained device punches",beforeIn,beforeOut));
        }
        await repo.SaveAsync();
    }

    /// <summary>Demo-only: pretends the machine holds today's punches for the working employees.</summary>
    public async Task<int> SyncAsync()
    {
        if (!me.Can(Perm.AdmSettings) && !me.Can(Perm.AttManage)) throw new DomainException("err.forbidden");
        if (!SettingsService.DeviceMode) throw new DomainException("err.notDeviceMode");
        var (ok, msg) = await PingAsync(); if (!ok) throw new DomainException("err.deviceDown");
        if (SettingsService.Get("zk.simulate") != "true") return 0; // real devices push through /api/device/punches
        var today = clock.Today; var rnd = new Random(today.DayOfYear);
        var emps = (await repo.ToListAsync(repo.Query<Employee>())).Where(e => e.IsWorking && e.HireDate <= today && !AttendanceCalculator.IsWeekend(today)).ToList();
        var list = new List<PunchIn>();
        foreach (var e in emps.Where(e => rnd.NextDouble() < 0.9))
        {
            var start = today + new TimeSpan(9, 0, 0) + TimeSpan.FromMinutes(rnd.Next(-15, 25)); if (start > clock.Now) continue;
            list.Add(new(e.Code, start)); list.Add(new(e.Code, start.AddSeconds(40))); // duplicate scan, as real machines do
            var end = today + new TimeSpan(17, 0, 0) + TimeSpan.FromMinutes(rnd.Next(-5, 40)); if (end < clock.Now) list.Add(new(e.Code, end));
        }
        var n = await IngestAsync(list);
        audit.Log("Sync", "Device", SettingsService.Get("zk.ip"), $"{n} punches");
        return n;
    }

    public async Task<List<DevicePunch>> RecentAsync() => await repo.ToListAsync(repo.Query<DevicePunch>().OrderByDescending(p => p.Time).Take(40));
}

public class EmailService(IRepository repo, AppClock clock)
{
    public void Queue(string to, string subject, string body)
    {
        var m = new EmailOutbox { To = to, Subject = subject, Body = body, CreatedAt = clock.UtcNow, Sent = false, Note = "SMTP not configured (demo): kept in the outbox" };
        repo.Add(m);
    }

    /// <summary>Sends queued mail through SMTP when a host is configured; otherwise leaves it in the outbox.</summary>
    private static readonly SemaphoreSlim SendGate = new(1, 1);
    public async Task FlushAsync()
    {
        if (!await SendGate.WaitAsync(0)) return;
        try {
        var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? SettingsService.Get("smtp.host");
        var apiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY");
        var allowed = (Environment.GetEnvironmentVariable("MAIL_ALLOWED_RECIPIENTS") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (string.IsNullOrWhiteSpace(host) && string.IsNullOrWhiteSpace(apiKey)) return;
        foreach (var m in await repo.ToListAsync(repo.Query<EmailOutbox>().Where(x => !x.Sent && x.Attempts < 5 && (x.NextAttemptAt == null || x.NextAttemptAt <= clock.UtcNow)).OrderBy(x => x.Id).Take(20)))
        {
            if (!allowed.Contains(m.To, StringComparer.OrdinalIgnoreCase) || m.To.EndsWith(".local", StringComparison.OrdinalIgnoreCase)) { m.Note = "Blocked: recipient is not on the explicit delivery allowlist"; continue; }
            try
            {
                m.Attempts++;
                if (!string.IsNullOrWhiteSpace(apiKey)) {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
                    http.DefaultRequestHeaders.Add("api-key", apiKey);
                    var from = Environment.GetEnvironmentVariable("MAIL_FROM") ?? "";
                    var payload = System.Text.Json.JsonSerializer.Serialize(new { sender = new { email = from, name = "PeopleHub Demo" }, to = new[] { new { email = m.To } }, subject = m.Subject, textContent = m.Body });
                    using var response = await http.PostAsync("https://api.brevo.com/v3/smtp/email", new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
                    response.EnsureSuccessStatusCode(); m.Sent = true; m.Note = "Accepted by provider, not proof of inbox delivery"; continue;
                }
                int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? SettingsService.Get("smtp.port"), out var port);
                using var c = new System.Net.Mail.SmtpClient(host, port == 0 ? 587 : port) { EnableSsl = true, Timeout = 8000 };
                var pass = Environment.GetEnvironmentVariable("SMTP_PASSWORD"); if (!string.IsNullOrEmpty(pass)) c.Credentials = new System.Net.NetworkCredential(SettingsService.Get("smtp.user"), pass);
                await c.SendMailAsync(new System.Net.Mail.MailMessage(SettingsService.Get("smtp.from"), m.To, m.Subject, m.Body)); m.Sent = true; m.Note = null;
            }
            catch (Exception) { m.Note = "Delivery failed; retry pending (maximum 5 attempts)"; m.NextAttemptAt = clock.UtcNow.AddMinutes(Math.Pow(2, m.Attempts)); }
        }
        await repo.SaveAsync();
        } finally { SendGate.Release(); }
    }

    public Task<List<EmailOutbox>> OutboxAsync() => repo.ToListAsync(repo.Query<EmailOutbox>().OrderByDescending(m => m.CreatedAt).Take(50));
}

public class AccountService(IRepository repo, ICurrentUser me, AppClock clock, EmailService mail, AuditService audit)
{
    private async Task RevokeSignInsAsync(int userId){foreach(var session in await repo.ToListAsync(repo.Query<LoginSession>().Where(x=>x.UserId==userId&&x.RevokedAt==null)))session.RevokedAt=clock.UtcNow;}
    private static void CheckPolicy(string pw)
    {
        int.TryParse(SettingsService.Get("pwd.minLength"), out var min); if (min < 6) min = 8;
        if (pw.Length < min || !pw.Any(char.IsLetter) || !pw.Any(char.IsDigit)) throw new DomainException("err.weakPassword");
    }

    public async Task ChangePasswordAsync(string current, string next)
    {
        var u = await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x => x.Id == me.UserId)) ?? throw new DomainException("err.forbidden");
        if (!PasswordHasher.Verify(current ?? "", u.PasswordHash)) throw new DomainException("err.wrongPassword");
        CheckPolicy(next ?? "");await RevokeSignInsAsync(u.Id); u.PasswordHash = PasswordHasher.Hash(next!); u.SecurityStamp = Guid.NewGuid().ToString("N");
        audit.Log("Update", "Password", u.Email, "changed by user"); await repo.SaveAsync();
    }

    /// <summary>Creates a one-hour token, stores only its hash, and queues delivery.</summary>
    public async Task<string?> RequestResetAsync(string email, string baseUrl)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var u = await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x => x.Email == email && x.IsActive)); if (u == null) return null;
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        foreach (var old in await repo.ToListAsync(repo.Query<PasswordReset>().Where(r => r.UserId == u.Id && !r.Used))) old.Used = true;
        repo.Add(new PasswordReset { UserId = u.Id, Token = HashToken(token), Expires = clock.UtcNow.AddHours(1) });
        mail.Queue(u.Email, "Reset your PeopleHub password", $"Use this link within one hour: {baseUrl}/Account/Reset?token={token}");
        await repo.SaveAsync(); return token;
    }

    public async Task<bool> ResetAsync(string token, string next)
    {
        token = HashToken(token);
        var r = await repo.FirstOrDefaultAsync(repo.Query<PasswordReset>().Where(x => x.Token == token && !x.Used));
        if (r == null || r.Expires < clock.UtcNow) return false;
        CheckPolicy(next ?? "");
        var u = await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x => x.Id == r.UserId)) ?? throw new DomainException("err.forbidden");
        await RevokeSignInsAsync(u.Id);u.PasswordHash = PasswordHasher.Hash(next!); u.SecurityStamp = Guid.NewGuid().ToString("N"); r.Used = true; audit.Log("Update", "Password", u.Email, "reset by token"); await repo.SaveAsync(); return true;
    }

    private static string HashToken(string token) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token ?? "")));
    public async Task<bool> TokenValidAsync(string token) { token = HashToken(token); return await repo.AnyAsync(repo.Query<PasswordReset>().Where(x => x.Token == token && !x.Used && x.Expires > clock.UtcNow)); }

    public async Task<(AppUser User, string Secret)> BeginTotpAsync()
    {
        var u = await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x => x.Id == me.UserId)) ?? throw new DomainException("err.forbidden");
        if (u.TotpEnabled) throw new DomainException("err.forbidden");
        u.TotpSecret = Totp.NewSecret(); u.TotpEnabled = false; await repo.SaveAsync(); return (u, u.TotpSecret);
    }
    public async Task EnableTotpAsync(string code)
    {
        var u = await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x => x.Id == me.UserId)) ?? throw new DomainException("err.forbidden");
        if (u.TotpSecret == null || !Totp.Verify(u.TotpSecret, code, clock.UtcNow)) throw new DomainException("err.badCode");
        u.TotpEnabled = true; audit.Log("Update", "2FA", u.Email, "enabled"); await repo.SaveAsync();
    }
    public async Task DisableTotpAsync(string password)
    {
        var u = await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x => x.Id == me.UserId)) ?? throw new DomainException("err.forbidden");
        if (!PasswordHasher.Verify(password ?? "", u.PasswordHash)) throw new DomainException("err.wrongPassword");
        await RevokeSignInsAsync(u.Id);u.TotpEnabled = false; u.TotpSecret = null; u.SecurityStamp = Guid.NewGuid().ToString("N"); audit.Log("Update", "2FA", u.Email, "disabled"); await repo.SaveAsync();
    }
    public Task<AppUser?> CurrentAsync() => repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x => x.Id == me.UserId));
    public Task<AppUser?> ByIdAsync(int id) => repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x => x.Id == id && x.IsActive));
}

