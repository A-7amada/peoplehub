using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

namespace PeopleHub.Application.Services;

public record AttendanceSummary(int WorkingDays, int PresentDays, int LateCount, int LateMinutes, int AbsentDays,
    int ExcusedAbsences, int LeaveDays, int UnpaidLeaveDays, decimal OvertimeHours, decimal OvertimeWeightedHours)
{
    /// <summary>Days deducted from pay: unexcused absences plus unpaid leave.</summary>
    public int DeductibleDays => AbsentDays + UnpaidLeaveDays;
}

public record DayRow(DateTime Date, bool IsWorkingDay, string? HolidayName, string? HolidayNameAr, AttendanceRecord? Record, LeaveRequest? Leave, bool IsAbsent);
public record DailyRow(Employee Employee, AttendanceRecord? Record, bool OnLeave);

public class AttendanceService(IRepository repo, AccessScope scope, ICurrentUser me, AuditService audit, NotificationService notify, AppClock clock)
{
    private const string Officers = $"{Roles.SystemAdmin},{Roles.HRManager},{Roles.HRSpecialist},{Roles.AttendanceOfficer}";

    public async Task<Shift> ShiftOfAsync(Employee e)
    {
        var shifts = await repo.ToListAsync(repo.Query<Shift>().OrderBy(s => s.Id));
        return shifts.FirstOrDefault(s => s.Id == e.ShiftId) ?? shifts.FirstOrDefault() ?? new Shift { Name = "Default", Start = new(9, 0, 0), End = new(17, 0, 0) };
    }

    private async Task<Employee> MeAsync() =>
        await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e => e.Id == (me.EmployeeId ?? -1))) ?? throw new DomainException("err.forbidden");

    public async Task<AttendanceRecord?> TodayRecordAsync()
    {
        var e = await MeAsync();
        var d = clock.Today;
        return await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == e.Id && r.Date == d));
    }

    public async Task CheckInAsync()
    {
        var e = await MeAsync();
        if (!e.IsWorking) throw new DomainException("err.terminated");
        var today = clock.Today;
        if (await repo.AnyAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == e.Id && r.Date == today && r.CheckIn != null)))
            throw new DomainException("err.duplicateCheckIn");
        var existing = await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == e.Id && r.Date == today));
        var rec = existing ?? new AttendanceRecord { EmployeeId = e.Id, Date = today };
        rec.CheckIn = clock.Now; rec.Source = AttendanceSource.MobileApp;
        rec.Recompute(await ShiftOfAsync(e));
        if (existing == null) repo.Add(rec);
        await repo.SaveAsync();
    }

    public async Task CheckOutAsync()
    {
        var e = await MeAsync();
        var rec = await TodayRecordAsync();
        if (rec?.CheckIn == null) throw new DomainException("err.noCheckIn");
        if (rec.CheckOut != null) throw new DomainException("err.duplicateCheckOut");
        rec.CheckOut = clock.Now;
        if (rec.CheckOut <= rec.CheckIn) rec.CheckOut = rec.CheckIn.Value.AddMinutes(1);
        rec.Recompute(await ShiftOfAsync(e));
        await repo.SaveAsync();
    }

    public async Task AddManualAsync(int employeeId, DateTime date, DateTime? checkIn, DateTime? checkOut, AttendanceSource source)
    {
        if (!me.InAny(Officers)) throw new DomainException("err.forbidden");
        var e = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x => x.Id == employeeId)) ?? throw new DomainException("err.notFound");
        if (date.Date > clock.Today) throw new DomainException("err.future");
        if (checkIn == null) throw new DomainException("err.checkinRequired");
        var rec = await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == employeeId && r.Date == date.Date));
        if (rec is { CheckIn: not null }) throw new DomainException("err.duplicateCheckIn");
        var isNew = rec == null;
        rec ??= new AttendanceRecord { EmployeeId = employeeId, Date = date.Date };
        rec.CheckIn = checkIn; rec.CheckOut = checkOut; rec.Source = source; rec.IsExcused = false; rec.ExcuseNote = null;
        rec.Recompute(await ShiftOfAsync(e));
        if (isNew) repo.Add(rec);
        audit.Log("Create", "Attendance", $"{e.Code}/{date:yyyy-MM-dd}", $"{source} in={checkIn:HH:mm} out={checkOut:HH:mm}");
        await repo.SaveAsync();
    }

    /// <summary>Marks an absence as excused (with a note) so it is not deducted.</summary>
    public async Task ExcuseAbsenceAsync(int employeeId, DateTime date, string? note)
    {
        var e = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x => x.Id == employeeId)) ?? throw new DomainException("err.notFound");
        var manager = e.ManagerId != null && e.ManagerId == me.EmployeeId;
        if (!manager && !me.InAny(Officers)) throw new DomainException("err.forbidden");
        if (string.IsNullOrWhiteSpace(note)) throw new DomainException("err.reasonRequired");
        var rec = await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == employeeId && r.Date == date.Date));
        if (rec != null && rec.Status != AttendanceStatus.Absent) throw new DomainException("err.notAbsent");
        if (rec == null) { rec = new AttendanceRecord { EmployeeId = employeeId, Date = date.Date, Status = AttendanceStatus.Absent }; repo.Add(rec); }
        rec.IsExcused = true; rec.ExcuseNote = note.Trim();
        audit.Log("Excuse", "Attendance", $"{e.Code}/{date:yyyy-MM-dd}", note);
        await repo.SaveAsync();
    }

    public async Task<List<DailyRow>> DailyAsync(DateTime date, int? departmentId)
    {
        var ids = await scope.VisibleEmployeeIdsAsync();
        var emps = await repo.ToListAsync(repo.Query<Employee>().Where(e => (e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave) && e.HireDate <= date
            && (ids == null || ids.Contains(e.Id)) && (departmentId == null || e.DepartmentId == departmentId)).OrderBy(e => e.Code));
        var recs = (await repo.ToListAsync(repo.Query<AttendanceRecord>().Where(r => r.Date == date.Date))).ToDictionary(r => r.EmployeeId);
        var leaves = await repo.ToListAsync(repo.Query<LeaveRequest>().Where(l => l.Status == RequestStatus.Approved && l.StartDate <= date && l.EndDate >= date));
        return emps.Select(e => new DailyRow(e, recs.GetValueOrDefault(e.Id), leaves.Any(l => l.EmployeeId == e.Id))).ToList();
    }

    public async Task<List<DayRow>> MonthAsync(int employeeId, int year, int month)
    {
        if (!await scope.CanSeeAsync(employeeId)) throw new DomainException("err.forbidden");
        var first = new DateTime(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var holidays = await repo.ToListAsync(repo.Query<Holiday>().Where(h => h.Date >= first && h.Date <= last));
        var recs = (await repo.ToListAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == employeeId && r.Date >= first && r.Date <= last))).ToDictionary(r => r.Date);
        var leaves = await repo.ToListAsync(repo.Query<LeaveRequest>().Where(l => l.EmployeeId == employeeId && l.Status == RequestStatus.Approved && l.StartDate <= last && l.EndDate >= first));
        var emp = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e => e.Id == employeeId));
        var today = clock.Today;
        var rows = new List<DayRow>();
        for (var d = first; d <= last; d = d.AddDays(1))
        {
            var h = holidays.FirstOrDefault(x => x.Date.Date == d);
            var working = !AttendanceCalculator.IsWeekend(d) && h == null && emp != null && d >= emp.HireDate.Date;
            recs.TryGetValue(d, out var rec);
            var leave = leaves.FirstOrDefault(l => l.StartDate <= d && l.EndDate >= d);
            var absent = working && leave == null && d < today && (rec == null || rec.Status == AttendanceStatus.Absent);
            rows.Add(new DayRow(d, working, h?.Name, h?.NameAr, rec, leave, absent));
        }
        return rows;
    }

    /// <summary>Monthly totals for many employees at once (used by payroll, reports and the dashboard).</summary>
    public async Task<Dictionary<int, AttendanceSummary>> SummariesAsync(IReadOnlyCollection<Employee> employees, int year, int month)
    {
        var first = new DateTime(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var today = clock.Today;
        var ids = employees.Select(e => e.Id).ToList();
        var holidays = (await repo.ToListAsync(repo.Query<Holiday>().Where(h => h.Date >= first && h.Date <= last))).Select(h => h.Date.Date).ToHashSet();
        var recs = (await repo.ToListAsync(repo.Query<AttendanceRecord>().Where(r => r.Date >= first && r.Date <= last && ids.Contains(r.EmployeeId)))).ToLookup(r => r.EmployeeId);
        var leaves = (await repo.ToListAsync(repo.Query<LeaveRequest>().Where(l => l.Status == RequestStatus.Approved && l.StartDate <= last && l.EndDate >= first && ids.Contains(l.EmployeeId)))).ToLookup(l => l.EmployeeId);
        var ots = (await repo.ToListAsync(repo.Query<OvertimeRequest>().Where(o => o.Status == RequestStatus.Approved && o.Date >= first && o.Date <= last && ids.Contains(o.EmployeeId)))).ToLookup(o => o.EmployeeId);

        var result = new Dictionary<int, AttendanceSummary>();
        foreach (var e in employees)
        {
            var byDate = recs[e.Id].ToDictionary(r => r.Date.Date);
            int working = 0, present = 0, lateCount = 0, lateMin = 0, absent = 0, excused = 0, leaveDays = 0, unpaid = 0;
            for (var d = first; d <= last; d = d.AddDays(1))
            {
                if (AttendanceCalculator.IsWeekend(d) || holidays.Contains(d) || d < e.HireDate.Date) continue;
                working++;
                var lv = leaves[e.Id].FirstOrDefault(l => l.StartDate <= d && l.EndDate >= d);
                if (lv != null) { leaveDays++; if (lv.LeaveType is { IsPaid: false }) unpaid++; continue; }
                byDate.TryGetValue(d, out var r);
                if (r != null && r.Status != AttendanceStatus.Absent) { present++; if (r.LateMinutes > 0) { lateCount++; lateMin += r.LateMinutes; } }
                else if (r is { IsExcused: true }) excused++;
                else if (d < today) absent++;
            }
            var otHours = ots[e.Id].Sum(o => o.Hours);
            var otWeighted = ots[e.Id].Sum(o => o.Hours * o.Multiplier);
            result[e.Id] = new AttendanceSummary(working, present, lateCount, lateMin, absent, excused, leaveDays, unpaid, otHours, otWeighted);
        }
        return result;
    }

    public async Task RequestOvertimeAsync(DateTime date, decimal hours, string? reason)
    {
        var e = await MeAsync();
        var o = OvertimeRequest.Create(e, date, hours, reason, clock.Today);
        if (await repo.AnyAsync(repo.Query<OvertimeRequest>().Where(x => x.EmployeeId == e.Id && x.Date == date.Date && (x.Status == RequestStatus.Pending || x.Status == RequestStatus.Approved))))
            throw new DomainException("err.overtimeExists");
        o.Employee = e;
        repo.Add(o);
        await notify.NotifyStageAsync(o, $"{e.FullName}: {hours}h");
        await repo.SaveAsync();
    }

    public async Task RequestCorrectionAsync(DateTime date, DateTime? newIn, DateTime? newOut, string? reason)
    {
        var e = await MeAsync();
        var rec = await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == e.Id && r.Date == date.Date));
        var c = AttendanceCorrection.Create(e, date, rec?.CheckIn, rec?.CheckOut, newIn, newOut, reason, clock.Today);
        c.Employee = e;
        repo.Add(c);
        await notify.NotifyStageAsync(c, $"{e.FullName}: {date:yyyy-MM-dd}");
        await repo.SaveAsync();
    }
}
