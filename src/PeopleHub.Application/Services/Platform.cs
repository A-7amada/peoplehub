using System.Collections.Concurrent;
using System.Net.Sockets;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Domain.Security;

namespace PeopleHub.Application.Services;

/// <summary>Key/value settings stored in the database, with a process-wide cache for synchronous reads.</summary>
public class SettingsService(IRepository repo, ICurrentUser me, AuditService audit)
{
    public static readonly ConcurrentDictionary<string, string> Cache = new();
    public static readonly Dictionary<string, string> Defaults = new()
    {
        ["att.mode"] = "System", ["zk.ip"] = "192.168.1.201", ["zk.port"] = "4370", ["zk.simulate"] = "true", ["zk.apiKey"] = "demo-device-key",
        ["email.notify"] = "true", ["smtp.host"] = "", ["smtp.port"] = "587", ["smtp.user"] = "", ["smtp.from"] = "hr@peoplehub.local",
        ["eosb.days"] = "15", ["pwd.minLength"] = "8"
    };
    public static string Get(string key) => Cache.TryGetValue(key, out var v) ? v : Defaults.GetValueOrDefault(key, "");
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

public class TalentService(IRepository repo, ICurrentUser me, AccessScope scope, AppClock clock, AuditService audit, NotificationService notify)
{
    private void Need() { if (!me.Can(Perm.TalentManage)) throw new DomainException("err.forbidden"); }

    public Task<List<JobOpening>> JobsAsync() { Need(); return repo.ToListAsync(repo.Query<JobOpening>().OrderByDescending(j => j.IsOpen).ThenByDescending(j => j.CreatedAt)); }
    public Task<List<Candidate>> CandidatesAsync() { Need(); return repo.ToListAsync(repo.Query<Candidate>().OrderBy(c => c.Stage).ThenByDescending(c => c.CreatedAt)); }
    public async Task AddJobAsync(string title,string? titleAr,int? dept,int positions,string? justification=null)
    {
        Need();if(string.IsNullOrWhiteSpace(title)||title.Length>200||positions<1||positions>1000||string.IsNullOrWhiteSpace(justification)||justification.Length>2000)throw new DomainException("err.required");if(dept!=null&&!await repo.AnyAsync(repo.Query<Department>().Where(x=>x.Id==dept)))throw new DomainException("err.notFound");
        repo.Add(new JobOpening{Title=title.Trim(),TitleAr=titleAr?.Trim()??"",DepartmentId=dept,Positions=positions,Justification=justification.Trim(),ApprovalStatus="Pending",RequestedByUserId=me.UserId,IsOpen=false,CreatedAt=clock.UtcNow});audit.Log("Requisition","Job",title,"Pending recruitment approval, not published externally");await repo.SaveAsync();
    }
    public Task DecideJobAsync(int id,bool approve,string reason)=>repo.AtomicAsync(async()=>
    {
        Need();var j=await repo.FirstOrDefaultAsync(repo.Query<JobOpening>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");if(j.ApprovalStatus!="Pending"||j.RequestedByUserId==me.UserId)throw new DomainException("err.forbidden");if(string.IsNullOrWhiteSpace(reason)||reason.Length>2000)throw new DomainException("err.reasonRequired");j.ApprovalStatus=approve?"Approved":"Rejected";j.ApprovedByUserId=me.UserId;j.ApprovalReason=reason.Trim();j.DecidedAt=clock.UtcNow;j.IsOpen=approve;audit.Log("DecideRequisition","Job",id,$"{j.ApprovalStatus}: {reason.Trim()}");await repo.SaveAsync();
    });
    public async Task CloseJobAsync(int id){Need();var j=await repo.FirstOrDefaultAsync(repo.Query<JobOpening>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");if(j.ApprovalStatus is not("Approved" or "Legacy"))throw new DomainException("err.forbidden");j.IsOpen=!j.IsOpen;audit.Log("ToggleOpening","Job",id,j.IsOpen?"Open":"Closed");await repo.SaveAsync();}
    public async Task AddCandidateAsync(int jobId, string name, string? email, string? note)
    {
        Need(); if (string.IsNullOrWhiteSpace(name)) throw new DomainException("err.required");
        if (!await repo.AnyAsync(repo.Query<JobOpening>().Where(x => x.Id == jobId && x.IsOpen && (x.ApprovalStatus=="Approved"||x.ApprovalStatus=="Legacy")))) throw new DomainException("err.notFound");
        repo.Add(new Candidate { JobOpeningId = jobId, Name = name.Trim(), Email = email?.Trim() ?? "", Note = note?.Trim() ?? "", CreatedAt = clock.UtcNow });
        await repo.SaveAsync();
    }
    public Task MoveCandidateAsync(int id, bool reject)=>repo.AtomicAsync(async()=>
    {
        Need(); var c = await repo.FirstOrDefaultAsync(repo.Query<Candidate>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
        if(c.Stage is CandidateStage.Hired or CandidateStage.Rejected)throw new DomainException("err.forbidden");
        if(!reject&&c.Stage==CandidateStage.Offer){var offers=await repo.ToListAsync(repo.Query<CandidateOffer>().Where(x=>x.CandidateId==c.Id));var latest=offers.OrderBy(x=>x.Id).LastOrDefault();if(latest==null||!await repo.AnyAsync(repo.Query<CandidateOfferEvent>().Where(x=>x.OfferId==latest.Id&&x.Kind=="Accepted")))throw new DomainException("err.forbidden");}
        c.InterviewStamp=Guid.NewGuid().ToString("N");
        if(reject){var offers=await repo.ToListAsync(repo.Query<CandidateOffer>().Where(x=>x.CandidateId==c.Id));var ids=offers.Select(x=>x.Id).ToList();var events=await repo.ToListAsync(repo.Query<CandidateOfferEvent>().Where(x=>ids.Contains(x.OfferId)).OrderBy(x=>x.Id));foreach(var offer in offers){var state=events.LastOrDefault(x=>x.OfferId==offer.Id)?.Kind??"Proposed";if(state is "Proposed" or "Approved")repo.Add(new CandidateOfferEvent{OfferId=offer.Id,Kind="Withdrawn",Note="Candidate moved to rejected stage",ActorUserId=me.UserId,Actor=me.Email,At=clock.UtcNow});}}
        if (reject) c.Stage = CandidateStage.Rejected; else c.Stage = Candidate.Next(c.Stage) ?? c.Stage;
        repo.Add(new RecruitmentEvent{CandidateId=c.Id,Kind="StageChanged",Note=c.Stage.ToString(),Actor=me.Email,At=clock.UtcNow});
        audit.Log("Update", "Candidate", c.Name, c.Stage.ToString()); await repo.SaveAsync();
    });

    public async Task<List<PerformanceReview>> ReviewsAsync()
    {
        var ids = await scope.VisibleEmployeeIdsAsync();
        var q = repo.Query<PerformanceReview>().OrderByDescending(r => r.CreatedAt);
        var all = await repo.ToListAsync(q);
        return all.Where(r => ids == null || ids.Contains(r.EmployeeId)).ToList();
    }
    public Task AddReviewAsync(int employeeId, string period, string? goals, string? comment, int score) => throw new DomainException("err.usePerf");

    public Task<List<TrainingCourse>> CoursesAsync() => repo.ToListAsync(repo.Query<TrainingCourse>().OrderBy(c => c.Date));
    public async Task<List<TrainingEnrollment>> EnrollmentsAsync() {
        var ids = me.Can(Perm.TalentManage) ? null : await scope.VisibleEmployeeIdsAsync();
        return await repo.ToListAsync(repo.Query<TrainingEnrollment>().Where(e => ids == null || ids.Contains(e.EmployeeId)));
    }
    public async Task AddCourseAsync(string title, string? titleAr, int hours, DateTime date,string? need=null,decimal? budget=null,string? currency=null)
    { Need(); if (string.IsNullOrWhiteSpace(title)||title.Length>200||hours<=0||hours>1000||date.Date<clock.Today||date.Year>9998||string.IsNullOrWhiteSpace(need)||need.Length>2000||budget<0||budget>1000000000||budget!=null&&(currency==null||currency.Length!=3||!currency.All(x=>x>='A'&&x<='Z'))) throw new DomainException("err.required"); repo.Add(new TrainingCourse { Title=title.Trim(),TitleAr=titleAr?.Trim()??"",Hours=hours,Date=date.Date,Need=need.Trim(),PlannedBudget=budget,Currency=budget==null?"":currency!,ApprovalStatus="Pending",CreatedByUserId=me.UserId });audit.Log("ProposeCourse","Training",title,"Pending course approval, planning only, no spending");await repo.SaveAsync(); }
    public Task DecideCourseAsync(int id,bool approve,string reason)=>repo.AtomicAsync(async()=>
    {Need();var course=await repo.FirstOrDefaultAsync(repo.Query<TrainingCourse>().Where(x=>x.Id==id))??throw new DomainException("err.notFound");if(course.ApprovalStatus!="Pending"||course.CreatedByUserId==me.UserId)throw new DomainException("err.forbidden");if(string.IsNullOrWhiteSpace(reason)||reason.Length>2000)throw new DomainException("err.reasonRequired");course.ApprovalStatus=approve?"Approved":"Rejected";course.DecidedByUserId=me.UserId;course.DecidedAt=clock.UtcNow;course.DecisionReason=reason.Trim();audit.Log("DecideCourse","Training",id,$"{course.ApprovalStatus}: {reason.Trim()}");await repo.SaveAsync();});
    public Task EnrollAsync(int courseId, int employeeId)=>repo.AtomicAsync(async()=>
    {
        if (employeeId != me.EmployeeId) Need();
        if (!await repo.AnyAsync(repo.Query<TrainingCourse>().Where(x => x.Id == courseId && (x.ApprovalStatus=="Approved"||x.ApprovalStatus=="Legacy"))) || !await repo.AnyAsync(repo.Query<Employee>().Where(x => x.Id == employeeId))) throw new DomainException("err.notFound");
        var employee=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x=>x.Id==employeeId))??throw new DomainException("err.notFound");
        if (await repo.AnyAsync(repo.Query<TrainingEnrollment>().Where(x => x.CourseId == courseId && x.EmployeeId == employeeId))) throw new DomainException("err.alreadyEnrolled");
        employee.LeaveRequestStamp=Guid.NewGuid().ToString("N");
        repo.Add(new TrainingEnrollment { CourseId = courseId, EmployeeId = employeeId });audit.Log("Enroll","Training",courseId,$"employee #{employeeId}"); await repo.SaveAsync();
    });
    public async Task CompleteAsync(int id) { Need(); var e = await repo.FirstOrDefaultAsync(repo.Query<TrainingEnrollment>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound"); e.Completed = true; await repo.SaveAsync(); }
}

/// <summary>Custom report builder: pick a dataset, tick columns, filter by department.</summary>
public class ReportBuilderService(IRepository repo, AccessScope scope, ICurrentUser me)
{
    public static readonly string[] Datasets = ["employees", "leave", "reviews", "training"];
    public static readonly Dictionary<string, string[]> Columns = new()
    {
        ["employees"] = ["code", "name", "nameAr", "department", "jobTitle", "status", "hireDate", "type", "phone", "email", "salary"],
        ["leave"] = ["code", "name", "department", "type", "from", "to", "days", "status"],
        ["reviews"] = ["name", "department", "period", "score", "reviewer"],
        ["training"] = ["name", "department", "course", "hours", "completed"],
    };

    public async Task<ReportTable> RunAsync(string dataset, string[]? cols, int? dept)
    {
        if (!me.Can(Perm.Reports) || !Columns.ContainsKey(dataset)) throw new DomainException("err.forbidden");
        var chosen = (cols is { Length: > 0 } ? cols : Columns[dataset]).Where(Columns[dataset].Contains).ToList();
        if (!me.Can(Perm.EmpSalary)) chosen.Remove("salary");
        var ids = await scope.VisibleEmployeeIdsAsync();
        var emps = (await repo.ToListAsync(repo.Query<Employee>())).Where(e => (ids == null || ids.Contains(e.Id)) && (dept == null || e.DepartmentId == dept)).ToDictionary(e => e.Id);
        var rows = new List<Dictionary<string, string>>();
        string D(Employee e) => e.Department?.Name ?? "";
        switch (dataset)
        {
            case "employees":
                rows = emps.Values.OrderBy(e => e.Code).Select(e => new Dictionary<string, string> { ["code"] = e.Code, ["name"] = e.FullName, ["nameAr"] = e.FullNameAr, ["department"] = D(e), ["jobTitle"] = e.JobTitle?.Name ?? "", ["status"] = e.Status.ToString(), ["hireDate"] = e.HireDate.ToString("yyyy-MM-dd"), ["type"] = e.EmploymentType.ToString(), ["phone"] = e.Phone, ["email"] = e.Email, ["salary"] = e.BasicSalary.ToString("N2", System.Globalization.CultureInfo.InvariantCulture) }).ToList(); break;
            case "leave":
                rows = (await repo.ToListAsync(repo.Query<LeaveRequest>().OrderByDescending(l => l.StartDate))).Where(l => emps.ContainsKey(l.EmployeeId)).Select(l => { var e = emps[l.EmployeeId]; return new Dictionary<string, string> { ["code"] = e.Code, ["name"] = e.FullName, ["department"] = D(e), ["type"] = me.EmployeeId==l.EmployeeId||me.Can(Perm.LeaveEvidence)?l.LeaveType?.Name??"":"Private leave", ["from"] = l.StartDate.ToString("yyyy-MM-dd"), ["to"] = l.EndDate.ToString("yyyy-MM-dd"), ["days"] = l.Days.ToString("0.#"), ["status"] = l.Status.ToString() }; }).ToList(); break;
            case "reviews":
                rows = (await repo.ToListAsync(repo.Query<PerformanceReview>().OrderByDescending(r => r.CreatedAt))).Where(r => emps.ContainsKey(r.EmployeeId)).Select(r => new Dictionary<string, string> { ["name"] = emps[r.EmployeeId].FullName, ["department"] = D(emps[r.EmployeeId]), ["period"] = r.Period, ["score"] = r.Score.ToString(), ["reviewer"] = r.Reviewer }).ToList(); break;
            case "training":
                rows = (await repo.ToListAsync(repo.Query<TrainingEnrollment>())).Where(t => emps.ContainsKey(t.EmployeeId)).Select(t => new Dictionary<string, string> { ["name"] = emps[t.EmployeeId].FullName, ["department"] = D(emps[t.EmployeeId]), ["course"] = t.Course?.Title ?? "", ["hours"] = (t.Course?.Hours ?? 0).ToString(), ["completed"] = t.Completed ? "yes" : "no" }).ToList(); break;
        }
        return new ReportTable("custom-" + dataset, chosen, rows.Select(r => chosen.Select(c => r.GetValueOrDefault(c, "")).ToList()).ToList());
    }
}

public record AppraisalView(PerfAppraisal A, List<PerfGoal> Goals, List<DevItem> Dev, decimal? GoalScore, decimal? Computed, decimal? Final, bool CanSelf, bool CanManage, bool CanCalibrate, bool Visible);

public partial class PerfService(IRepository repo, ICurrentUser me, AccessScope scope, AppClock clock, AuditService audit, NotificationService notify)
{
    private bool Hr => me.Can(Perm.TalentManage);
    public Task<List<PerfCycle>> CyclesAsync() => repo.ToListAsync(repo.Query<PerfCycle>().OrderBy(c => c.Name.StartsWith("Imported:")).ThenByDescending(c => c.Id));

    public async Task<PerfCycle> CycleAsync(int id) => await repo.FirstOrDefaultAsync(repo.Query<PerfCycle>().Where(c => c.Id == id)) ?? throw new DomainException("err.notFound");

    public Task CreateCycleAsync(string name, DateTime start, DateTime end)=>repo.AtomicAsync(async()=>
    {
        if (!Hr) throw new DomainException("err.forbidden");
        if (string.IsNullOrWhiteSpace(name) || start.Year < 2000 || end.Year < 2000 || end < start) throw new DomainException("err.required");
        var c = new PerfCycle { Name = name.Trim(), Start = start, End = end, Status = CycleStatus.Open }; repo.Add(c); await repo.SaveAsync();
        foreach (var e in (await repo.ToListAsync(repo.Query<Employee>())).Where(e => e.IsWorking && e.HireDate <= end.Date))
            repo.Add(new PerfAppraisal { CycleId = c.Id, EmployeeId = e.Id });
        audit.Log("Create", "PerfCycle", name, ""); await repo.SaveAsync();
        await notify.NotifyPermAsync(Perm.ScopeTeam, "notif.perfCycle", name, "/Perf"); await repo.SaveAsync();
    });

    public Task AdvanceCycleAsync(int id)=>repo.AtomicAsync(async()=>
    {
        if (!Hr) throw new DomainException("err.forbidden"); var c = await CycleAsync(id);
        if (c.Status == CycleStatus.Open) c.Status = CycleStatus.Calibration;
        else if (c.Status == CycleStatus.Calibration)
        {
            c.Status = CycleStatus.Closed;
            foreach (var a in await repo.ToListAsync(repo.Query<PerfAppraisal>().Where(a => a.CycleId == id)))
            { await SealAsync(a); await notify.NotifyEmployeeAsync(a.EmployeeId, "notif.review", c.Name, "/Perf"); }
        }
        audit.Log("Update", "PerfCycle", c.Name, c.Status.ToString()); await repo.SaveAsync();
    });

    private async Task SealAsync(PerfAppraisal a)
    {
        var goals = await repo.ToListAsync(repo.Query<PerfGoal>().Where(g => g.AppraisalId == a.Id));
        if (a.ManagerDone) a.FinalScore ??= await ScoreAsync(a, goals);
    }

    public async Task<List<AppraisalView>> ListAsync(int cycleId)
    {
        var ids = await scope.VisibleEmployeeIdsAsync();
        var apps = (await repo.ToListAsync(repo.Query<PerfAppraisal>().Where(a => a.CycleId == cycleId))).Where(a => ids == null || ids.Contains(a.EmployeeId) || a.EmployeeId == me.EmployeeId).ToList();
        var res = new List<AppraisalView>(); foreach (var a in apps) { var v = await BuildAsync(a); if (v.Visible) res.Add(v); } return res;
    }

    public async Task<AppraisalView> GetAsync(int id)
    {
        var a = await repo.FirstOrDefaultAsync(repo.Query<PerfAppraisal>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
        var v = await BuildAsync(a); if (!v.Visible) throw new DomainException("err.forbidden"); return v;
    }

    private async Task<AppraisalView> BuildAsync(PerfAppraisal a)
    {
        var goals = await repo.ToListAsync(repo.Query<PerfGoal>().Where(g => g.AppraisalId == a.Id).OrderBy(g => g.Id));
        var dev = await repo.ToListAsync(repo.Query<DevItem>().Where(d => d.AppraisalId == a.Id).OrderBy(d => d.Id));
        var ids = await scope.VisibleEmployeeIdsAsync();
        var cycle = a.Cycle ?? await CycleAsync(a.CycleId); var open = cycle.Status == CycleStatus.Open && (!cycle.AutoPhases || clock.Today >= cycle.Start.Date);
        bool self = a.EmployeeId == me.EmployeeId, mgr = me.EmployeeId!=null && a.Employee?.ManagerId == me.EmployeeId && !self;
        bool visible = Hr || self || mgr;
        var gs = PerfCalc.GoalScore(goals); var comp = await ScoreAsync(a, goals);
        // before the cycle closes an employee sees only their own input, never the manager's score
        return new(a, goals, dev, gs, comp, a.FinalScore ?? (cycle.Status == CycleStatus.Closed ? comp : null),
            self && open && !a.SelfDone, (mgr || Hr) && !self && open, Hr && cycle.Status == CycleStatus.Calibration && !self, visible);
    }

    private async Task<(PerfAppraisal A, PerfCycle C)> LoadAsync(int id)
    {
        var a = await repo.FirstOrDefaultAsync(repo.Query<PerfAppraisal>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
        return (a, await CycleAsync(a.CycleId));
    }

    public async Task AddGoalAsync(int appraisalId, string title, string? target, int weight)
    {
        var v = await GetAsync(appraisalId); if (!v.CanManage) throw new DomainException("err.forbidden");
        if (string.IsNullOrWhiteSpace(title) || weight is < 1 or > 100) throw new DomainException("err.required");
        if (v.A.ManagerDone) throw new DomainException("err.forbidden");
        if (v.Goals.Sum(g => g.Weight) + weight > 100) throw new DomainException("err.weights");
        repo.Add(new PerfGoal { AppraisalId = appraisalId, Title = title.Trim(), Target = target?.Trim() ?? "", Weight = weight });
        await notify.NotifyEmployeeAsync(v.A.EmployeeId, "notif.goal", title, $"/Perf/Appraisal/{appraisalId}"); await repo.SaveAsync();
    }

    public async Task UpdateProgressAsync(int goalId, int progress, int? selfScore)
    {
        var g = await repo.FirstOrDefaultAsync(repo.Query<PerfGoal>().Where(x => x.Id == goalId)) ?? throw new DomainException("err.notFound");
        var v = await GetAsync(g.AppraisalId); if (!v.CanSelf && !v.CanManage) throw new DomainException("err.forbidden");
        g.Progress = Math.Clamp(progress, 0, 100); if (v.CanSelf && selfScore is >= 1 and <= 5) g.SelfScore = selfScore; await repo.SaveAsync();
    }

    public async Task SubmitSelfAsync(int id, int score, string? comment)
    {
        var v = await GetAsync(id); if (!v.CanSelf) throw new DomainException("err.forbidden");
        if (!PerformanceReview.ValidScore(score)) throw new DomainException("err.required");
        v.A.SelfScore = score; v.A.SelfComment = comment?.Trim() ?? ""; v.A.SelfDone = true; await repo.SaveAsync();
    }

    public async Task SubmitManagerAsync(int id, int score, string? comment, Dictionary<int, int> goalScores)
    {
        var v = await GetAsync(id); if (!v.CanManage) throw new DomainException("err.forbidden");
        if (!PerformanceReview.ValidScore(score)) throw new DomainException("err.required");
        if (v.Goals.Count > 0 && !PerfCalc.WeightsOk(v.Goals)) throw new DomainException("err.weights100");
        if (v.Goals.Any(g => !goalScores.TryGetValue(g.Id, out var s) || !PerformanceReview.ValidScore(s))) throw new DomainException("err.required");
        foreach (var g in v.Goals) g.ManagerScore = goalScores[g.Id];
        v.A.ManagerScore = score; v.A.ManagerComment = comment?.Trim() ?? ""; v.A.ManagerDone = true;
        audit.Log("Update", "Appraisal", v.A.Id.ToString(), $"manager score {score}"); await repo.SaveAsync();
    }

    public async Task CalibrateAsync(int id, decimal score, string? note)
    {
        var v = await GetAsync(id); if (!v.CanCalibrate) throw new DomainException("err.forbidden");
        if (!v.A.ManagerDone) throw new DomainException("err.managerFirst");
        if (score < 1 || score > 5 || string.IsNullOrWhiteSpace(note)) throw new DomainException("err.calibNote");
        v.A.FinalScore = Math.Round(score, 2); v.A.CalibrationNote = note.Trim();
        audit.Log("Update", "Appraisal", v.A.Id.ToString(), $"calibrated to {score}: {note}"); await repo.SaveAsync();
    }

    public async Task AddDevAsync(int id, string action, DateTime? due)
    {
        var v = await GetAsync(id); if (!(v.CanManage || Hr || v.A.Employee?.ManagerId == me.EmployeeId || v.A.EmployeeId == me.EmployeeId)) throw new DomainException("err.forbidden");
        if (string.IsNullOrWhiteSpace(action)) throw new DomainException("err.required");
        repo.Add(new DevItem { AppraisalId = id, Action = action.Trim(), Due = due }); await repo.SaveAsync();
    }
    public async Task ToggleDevAsync(int itemId)
    {
        var d = await repo.FirstOrDefaultAsync(repo.Query<DevItem>().Where(x => x.Id == itemId)) ?? throw new DomainException("err.notFound");
        if (d.CourseId != null) throw new DomainException("err.courseManaged");
        var v = await GetAsync(d.AppraisalId);
        if (!(Hr || v.A.EmployeeId == me.EmployeeId || v.A.Employee?.ManagerId == me.EmployeeId)) throw new DomainException("err.forbidden");
        d.Done = !d.Done; await repo.SaveAsync();
    }
}
