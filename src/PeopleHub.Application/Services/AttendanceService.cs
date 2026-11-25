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

public class AttendanceService(IRepository repo, AccessScope scope, ICurrentUser me, AuditService audit, NotificationService notify, AppClock clock, WorkflowService flows)
{

    public async Task<bool> IsLockedAsync(int employeeId,int year,int month)=>await repo.AnyAsync(repo.Query<AttendancePeriodLock>().Where(x=>x.EmployeeId==employeeId && x.Year==year && x.Month==month));
    public Task LockAsync(int employeeId,int year,int month,string reason)=>repo.AtomicAsync(()=>LockCoreAsync(employeeId,year,month,reason));
    private async Task LockCoreAsync(int employeeId,int year,int month,string reason)
    {
        if(!me.Can(Perm.AttManage))throw new DomainException("err.forbidden");
        if(year<2000 || year>9998 || month<1 || month>12 || string.IsNullOrWhiteSpace(reason))throw new DomainException("err.required");
        if(new DateTime(year,month,1).AddMonths(1)>clock.Today)throw new DomainException("err.periodOpen");
        if(!await repo.AnyAsync(repo.Query<Employee>().Where(e=>e.Id==employeeId)))throw new DomainException("err.notFound");
        if(await IsLockedAsync(employeeId,year,month))throw new DomainException("err.attendanceLocked");
        if(await repo.AnyAsync(repo.Query<AttendanceCorrection>().Where(c=>c.EmployeeId==employeeId && c.Date.Year==year && c.Date.Month==month && c.Status==RequestStatus.Pending)))throw new DomainException("err.pendingCorrection");
        var emp=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e=>e.Id==employeeId));emp!.LeaveRequestStamp=Guid.NewGuid().ToString("N");
        repo.Add(new AttendancePeriodLock{EmployeeId=employeeId,Year=year,Month=month,At=clock.UtcNow,Actor=me.Email,Reason=reason.Trim()});audit.Log("Lock","AttendancePeriod",employeeId,$"{year}-{month:00}: {reason.Trim()}");await repo.SaveAsync();
    }
    private async Task RequireUnlockedAsync(int employeeId,DateTime day){var emp=await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e=>e.Id==employeeId))??throw new DomainException("err.notFound");if(await IsLockedAsync(employeeId,day.Year,day.Month))throw new DomainException("err.attendanceLocked");emp.LeaveRequestStamp=Guid.NewGuid().ToString("N");}
    public async Task<List<AttendanceEvidence>> EvidenceAsync(int employeeId,int year,int month)
    {
        if(!await scope.CanSeeAsync(employeeId)) throw new DomainException("err.forbidden");
        if(year<1900 || year>9998 || month<1 || month>12) throw new DomainException("err.required");
        var start=new DateTime(year,month,1);var end=start.AddMonths(1);
        return await repo.ToListAsync(repo.Query<AttendanceEvidence>().Where(x=>x.EmployeeId==employeeId && x.Date>=start && x.Date<end).OrderByDescending(x=>x.Id));
    }
    public async Task<Shift> ShiftOfAsync(Employee e,DateTime? day=null)
    {
        var shifts = await repo.ToListAsync(repo.Query<Shift>().OrderBy(s => s.Id));
        var history=await repo.ToListAsync(repo.Query<EmployeeAssignment>().Where(x=>x.EmployeeId==e.Id));
        var overrides=await repo.ToListAsync(repo.Query<EmployeeShiftDay>().Where(x=>x.EmployeeId==e.Id));
        var shiftId=ShiftCalendar.ShiftId(e,day??clock.Today,history,overrides);
        return shifts.FirstOrDefault(s => s.Id == shiftId) ?? shifts.FirstOrDefault() ?? new Shift { Name = "Default", Start = new(9, 0, 0), End = new(17, 0, 0) };
    }

    private async Task<Employee> MeAsync() =>
        await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e => e.Id == (me.EmployeeId ?? -1))) ?? throw new DomainException("err.forbidden");

    public async Task<AttendanceRecord?> TodayRecordAsync()
    {
        var e = await MeAsync();
        var d = clock.Today;
        var prev=d.AddDays(-1);var shift=await ShiftOfAsync(e,prev);
        if(shift.IsNight && clock.Now.TimeOfDay<=shift.End){var night=await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r=>r.EmployeeId==e.Id&&r.Date==prev&&r.CheckIn!=null));if(night!=null)return night;}
        return await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == e.Id && r.Date == d));
    }

    public Task CheckInAsync() => repo.AtomicAsync(()=>CheckInCoreAsync());
    private async Task CheckInCoreAsync()
    {
        if (SettingsService.DeviceMode) throw new DomainException("err.deviceMode");
        var e = await MeAsync();
        if (!e.IsWorking) throw new DomainException("err.terminated");
        var today = ShiftCalendar.WorkDate(clock.Now,await ShiftOfAsync(e,clock.Today.AddDays(-1)));
        await RequireUnlockedAsync(e.Id,today);
        if (await repo.AnyAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == e.Id && r.Date == today && r.CheckIn != null)))
            throw new DomainException("err.duplicateCheckIn");
        var existing = await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == e.Id && r.Date == today));
        var rec = existing ?? new AttendanceRecord { EmployeeId = e.Id, Date = today };
        rec.CheckIn = clock.Now; rec.Source = AttendanceSource.MobileApp;
        rec.Recompute(await ShiftOfAsync(e,rec.Date));
        if (existing == null) repo.Add(rec);
        repo.Add(AttendanceEvidence.Capture(rec,"CheckIn",me.Email,clock.UtcNow));
        await repo.SaveAsync();
    }

    public Task CheckOutAsync() => repo.AtomicAsync(()=>CheckOutCoreAsync());
    private async Task CheckOutCoreAsync()
    {
        if (SettingsService.DeviceMode) throw new DomainException("err.deviceMode");
        var e = await MeAsync();
        var rec = await TodayRecordAsync();
        await RequireUnlockedAsync(e.Id,rec?.Date??clock.Today);
        if (rec?.CheckIn == null) throw new DomainException("err.noCheckIn");
        if (rec.CheckOut != null) throw new DomainException("err.duplicateCheckOut");
        var beforeIn=rec.CheckIn;var beforeOut=rec.CheckOut;
        rec.CheckOut = clock.Now;
        if (rec.CheckOut <= rec.CheckIn) rec.CheckOut = rec.CheckIn.Value.AddMinutes(1);
        rec.Recompute(await ShiftOfAsync(e,rec.Date));
        repo.Add(AttendanceEvidence.Capture(rec,"CheckOut",me.Email,clock.UtcNow,"",beforeIn,beforeOut));
        await repo.SaveAsync();
    }

    public Task AddManualAsync(int employeeId, DateTime date, DateTime? checkIn, DateTime? checkOut, AttendanceSource source,string? reason=null) => repo.AtomicAsync(()=>AddManualCoreAsync(employeeId,date,checkIn,checkOut,source,reason));
    private async Task AddManualCoreAsync(int employeeId, DateTime date, DateTime? checkIn, DateTime? checkOut, AttendanceSource source,string? reason)
    {
        if (!me.Can(Perm.AttManage)) throw new DomainException("err.forbidden");
        var e = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x => x.Id == employeeId)) ?? throw new DomainException("err.notFound");
        await RequireUnlockedAsync(employeeId,date);
        if(string.IsNullOrWhiteSpace(reason))throw new DomainException("err.reasonRequired");
        if (date.Date > clock.Today) throw new DomainException("err.future");
        if (checkIn == null) throw new DomainException("err.checkinRequired");
        var rec = await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == employeeId && r.Date == date.Date));
        if (rec is { CheckIn: not null }) throw new DomainException("err.duplicateCheckIn");
        var isNew = rec == null;
        rec ??= new AttendanceRecord { EmployeeId = employeeId, Date = date.Date };
        var manualShift=await ShiftOfAsync(e,date);
        checkOut=ShiftCalendar.OvernightOut(date,checkIn,checkOut,manualShift);
        rec.CheckIn = checkIn; rec.CheckOut = checkOut; rec.Source = AttendanceSource.Manual; rec.IsExcused = false; rec.ExcuseNote = null;
        rec.Recompute(await ShiftOfAsync(e,rec.Date));
        if (isNew) repo.Add(rec);
        repo.Add(AttendanceEvidence.Capture(rec,"ManualEntry",me.Email,clock.UtcNow,reason.Trim()));
        audit.Log("Create", "Attendance", $"{e.Code}/{date:yyyy-MM-dd}", $"{source} in={checkIn:HH:mm} out={checkOut:HH:mm}");
        await repo.SaveAsync();
    }

    /// <summary>Marks an absence as excused (with a note) so it is not deducted.</summary>
    public Task ExcuseAbsenceAsync(int employeeId, DateTime date, string? note) => repo.AtomicAsync(()=>ExcuseAbsenceCoreAsync(employeeId,date,note));
    private async Task ExcuseAbsenceCoreAsync(int employeeId, DateTime date, string? note)
    {
        await RequireUnlockedAsync(employeeId,date);
        var e = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x => x.Id == employeeId)) ?? throw new DomainException("err.notFound");
        var manager = e.ManagerId != null && e.ManagerId == me.EmployeeId;
        if (!manager && !me.Can(Perm.AttManage)) throw new DomainException("err.forbidden");
        if (string.IsNullOrWhiteSpace(note)) throw new DomainException("err.reasonRequired");
        var rec = await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == employeeId && r.Date == date.Date));
        if (rec != null && rec.Status != AttendanceStatus.Absent) throw new DomainException("err.notAbsent");
        if (rec == null) { rec = new AttendanceRecord { EmployeeId = employeeId, Date = date.Date, Status = AttendanceStatus.Absent }; repo.Add(rec); }
        repo.Add(AttendanceEvidence.Capture(rec,rec.IsExcused?"ExcuseUpdated":"Excused",me.Email,clock.UtcNow,note.Trim()));
        rec.IsExcused = true; rec.ExcuseNote = note.Trim();
        audit.Log("Excuse", "Attendance", $"{e.Code}/{date:yyyy-MM-dd}", note);
        await repo.SaveAsync();
    }

    private static AttendanceRecord? VisibleRecord(AttendanceRecord? r)=>r==null?null:new AttendanceRecord{Id=r.Id,EmployeeId=r.EmployeeId,Date=r.Date,CheckIn=r.CheckIn,CheckOut=r.CheckOut,Source=r.Source,LateMinutes=r.LateMinutes,EarlyLeaveMinutes=r.EarlyLeaveMinutes,WorkedMinutes=r.WorkedMinutes,OvertimeMinutes=r.OvertimeMinutes,Status=r.Status,IsExcused=r.IsExcused};

    public async Task<List<DailyRow>> DailyAsync(DateTime date, int? departmentId)
    {
        var ids = await scope.VisibleEmployeeIdsAsync();
        var emps = await repo.ToListAsync(repo.Query<Employee>().Where(e => (e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave) && e.HireDate <= date
            && (ids == null || ids.Contains(e.Id)) && (departmentId == null || e.DepartmentId == departmentId)).OrderBy(e => e.Code));
        var recs = (await repo.ToListAsync(repo.Query<AttendanceRecord>().Where(r => r.Date == date.Date))).ToDictionary(r => r.EmployeeId);
        var leaves = await repo.ToListAsync(repo.Query<LeaveRequest>().Where(l => l.Status == RequestStatus.Approved && l.StartDate <= date && l.EndDate >= date));
        return emps.Select(e =>
        {
            var visible=new Employee{Id=e.Id,Code=e.Code,FullName=e.FullName,FullNameAr=e.FullNameAr,DepartmentId=e.DepartmentId,Department=e.Department==null?null:new Department{Id=e.Department.Id,Name=e.Department.Name,NameAr=e.Department.NameAr}};
            var r=recs.GetValueOrDefault(e.Id);
            var record=VisibleRecord(r);
            return new DailyRow(visible,record,leaves.Any(l=>l.EmployeeId==e.Id));
        }).ToList();
    }

    public async Task<List<DayRow>> MonthAsync(int employeeId, int year, int month)
    {
        if (!await scope.CanSeeAsync(employeeId)) throw new DomainException("err.forbidden");
        if (year < 1900 || year > 9998 || month < 1 || month > 12) throw new DomainException("err.required");
        var first = new DateTime(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var holidays = await repo.ToListAsync(repo.Query<Holiday>().Where(h => h.Date >= first && h.Date <= last));
        var recs = (await repo.ToListAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == employeeId && r.Date >= first && r.Date <= last))).ToDictionary(r => r.Date);
        var leaves = await repo.ToListAsync(repo.Query<LeaveRequest>().Where(l => l.EmployeeId == employeeId && l.Status == RequestStatus.Approved && l.StartDate <= last && l.EndDate >= first));
        var emp = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e => e.Id == employeeId));
        var assignments=await repo.ToListAsync(repo.Query<EmployeeAssignment>().Where(x=>x.EmployeeId==employeeId));
        var today = clock.Today;
        var rows = new List<DayRow>();
        for (var d = first; d <= last; d = d.AddDays(1))
        {
            var h = holidays.FirstOrDefault(x => x.Date.Date == d && emp!=null && HolidayCalendar.Applies(x,emp,d,assignments));
            var working = !AttendanceCalculator.IsWeekend(d) && h == null && emp != null && d >= emp.HireDate.Date;
            recs.TryGetValue(d, out var rec);
            var leave = leaves.FirstOrDefault(l => l.StartDate <= d && l.EndDate >= d);
            var absent = working && leave == null && d < today && (rec == null || rec.Status == AttendanceStatus.Absent);
            // Attendance visibility does not grant access to private leave types or medical evidence.
            LeaveRequest? visibleLeave = leave == null ? null : new LeaveRequest
            {
                Id=leave.Id,EmployeeId=leave.EmployeeId,StartDate=leave.StartDate,EndDate=leave.EndDate,Days=leave.Days,Status=leave.Status,
                LeaveType=new LeaveType { Name=me.EmployeeId==employeeId||me.Can(Perm.LeaveEvidence)?leave.LeaveType?.Name??"":"Private leave", NameAr=me.EmployeeId==employeeId||me.Can(Perm.LeaveEvidence)?leave.LeaveType?.NameAr??"":"إجازة خاصة" }
            };
            rows.Add(new DayRow(d, working, h?.Name, h?.NameAr, VisibleRecord(rec), visibleLeave, absent));
        }
        return rows;
    }

    /// <summary>Monthly totals for many employees at once (used by payroll, reports and the dashboard).</summary>
    public async Task<Dictionary<int, AttendanceSummary>> SummariesAsync(IReadOnlyCollection<Employee> employees, int year, int month)
    {
        if (year < 1900 || year > 9998 || month < 1 || month > 12) throw new DomainException("err.required");
        var first = new DateTime(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var today = clock.Today;
        var ids = employees.Select(e => e.Id).ToList();
        var holidays = await repo.ToListAsync(repo.Query<Holiday>().Where(h => h.Date >= first && h.Date <= last));
        var assignments=await repo.ToListAsync(repo.Query<EmployeeAssignment>().Where(x=>ids.Contains(x.EmployeeId)));
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
                if (AttendanceCalculator.IsWeekend(d) || holidays.Any(h=>h.Date.Date==d && HolidayCalendar.Applies(h,e,d,assignments)) || d < e.HireDate.Date) continue;
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

    public Task RequestOvertimeAsync(DateTime date,decimal hours,string? reason)=>repo.AtomicAsync(()=>RequestOvertimeCoreAsync(date,hours,reason));
    private async Task RequestOvertimeCoreAsync(DateTime date, decimal hours, string? reason)
    {
        var e = await MeAsync();
        var o = OvertimeRequest.Create(e, date, hours, reason, clock.Today);
        await flows.RouteRequestAsync("Overtime",o,e,hours);
        if (await repo.AnyAsync(repo.Query<OvertimeRequest>().Where(x => x.EmployeeId == e.Id && x.Date == date.Date && (x.Status == RequestStatus.Pending || x.Status == RequestStatus.Approved))))
            throw new DomainException("err.overtimeExists");
        e.LeaveRequestStamp=Guid.NewGuid().ToString("N");
        o.Employee = e;
        repo.Add(o);
        await notify.NotifyStageAsync(o, $"{e.FullName}: {hours}h");
        await repo.SaveAsync();
    }

    public Task RequestCorrectionAsync(DateTime date,DateTime? newIn,DateTime? newOut,string? reason,string? attToken=null,string? attName=null)=>repo.AtomicAsync(()=>RequestCorrectionCoreAsync(date,newIn,newOut,reason,attToken,attName));
    private async Task RequestCorrectionCoreAsync(DateTime date, DateTime? newIn, DateTime? newOut, string? reason, string? attToken, string? attName)
    {
        var e = await MeAsync();
        var rec = await repo.FirstOrDefaultAsync(repo.Query<AttendanceRecord>().Where(r => r.EmployeeId == e.Id && r.Date == date.Date));
        await RequireUnlockedAsync(e.Id,date);
        newOut=ShiftCalendar.OvernightOut(date,newIn,newOut,await ShiftOfAsync(e,date));
        var c = AttendanceCorrection.Create(e, date, rec?.CheckIn, rec?.CheckOut, newIn, newOut, reason, clock.Today);
        await flows.RouteRequestAsync("Correction",c,e,0);
        c.Employee = e; c.AttachmentToken = attToken; c.AttachmentName = attName;
        repo.Add(c);
        await notify.NotifyStageAsync(c, $"{e.FullName}: {date:yyyy-MM-dd}");
        await repo.SaveAsync();
    }
}
