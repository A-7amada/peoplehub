namespace PeopleHub.Domain.Entities;

public enum AttendanceStatus { Present = 0, Late = 1, Absent = 2 }
public enum AttendanceSource { Manual = 0, Fingerprint = 1, MobileApp = 2 }

public class Shift
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NameAr { get; set; } = "";
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public int GraceMinutes { get; set; } = 10;
    /// <summary>Arriving later than this many minutes counts as an absence.</summary>
    public int MaxLateMinutes { get; set; } = 120;
    public bool Flexible {get;set;}
    public TimeSpan? LatestStart {get;set;}
    public int? RequiredMinutes {get;set;}
    public bool IsNight => End <= Start;
    public double ScheduledHours => (End > Start ? End - Start : End + TimeSpan.FromHours(24) - Start).TotalHours;
}

public record AttendanceResult(int LateMinutes, int EarlyLeaveMinutes, int WorkedMinutes, int OvertimeMinutes, AttendanceStatus Status);

public static class AttendanceCalculator
{
    public const int OvertimeThresholdMinutes = 30;

    public static bool IsWeekend(DateTime d) => d.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;

    public static int CountWorkingDays(DateTime start, DateTime end, IEnumerable<DateTime>? holidays = null)
    {
        var h = new HashSet<DateTime>((holidays ?? []).Select(x => x.Date));
        var n = 0;
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            if (!IsWeekend(d) && !h.Contains(d)) n++;
        return n;
    }

    public static AttendanceResult Calculate(Shift shift, DateTime date, DateTime? checkIn, DateTime? checkOut, int? graceOverride = null)
    {
        if (checkIn is null) return new(0, 0, 0, 0, AttendanceStatus.Absent);
        var start = date.Date + shift.Start;
        var end = date.Date + shift.End;
        if (shift.IsNight) end = end.AddDays(1);

        if(shift.Flexible){if(shift.LatestStart==null||shift.RequiredMinutes is not >0||shift.LatestStart<shift.Start||shift.LatestStart.Value.TotalMinutes+shift.RequiredMinutes>1440||shift.IsNight)throw new DomainException("err.required");var latest=date.Date+shift.LatestStart.Value;end=(checkIn.Value<start?start:checkIn.Value).AddMinutes(shift.RequiredMinutes.Value);start=latest;}
        var late = (int)Math.Max(0, Math.Floor((checkIn.Value - start).TotalMinutes));
        if (late <= (graceOverride ?? shift.GraceMinutes)) late = 0;
        if (late > shift.MaxLateMinutes) return new(late, 0, 0, 0, AttendanceStatus.Absent);

        var early = 0; var worked = 0; var overtime = 0;
        if (checkOut is { } co)
        {
            if (co <= checkIn) throw new DomainException("err.checkoutBeforeIn");
            worked = (int)(co - checkIn.Value).TotalMinutes;
            early = (int)Math.Max(0, Math.Floor((end - co).TotalMinutes));
            var extra = (int)Math.Max(0, Math.Floor((co - end).TotalMinutes));
            overtime = extra >= OvertimeThresholdMinutes ? extra : 0;
        }
        return new(late, early, worked, overtime, late > 0 ? AttendanceStatus.Late : AttendanceStatus.Present);
    }
}

public class AttendanceRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public AttendanceSource Source { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public AttendanceStatus Status { get; set; }
    public bool IsExcused { get; set; }
    public string? ExcuseNote { get; set; }

    public void Recompute(Shift shift, int? graceOverride = null)
    {
        var r = AttendanceCalculator.Calculate(shift, Date, CheckIn, CheckOut, graceOverride);
        LateMinutes = r.LateMinutes; EarlyLeaveMinutes = r.EarlyLeaveMinutes; WorkedMinutes = r.WorkedMinutes;
        OvertimeMinutes = r.OvertimeMinutes; Status = r.Status;
    }
}

/// <summary>US-PH2-008 configurable overtime rules per employment type. Defaults equal the previous hard-coded values (8 h/day, 1.5x, 2x, no monthly cap). Illustrative, not legally validated.</summary>
public record OvertimeRules(decimal MaxDailyHours = 8m, decimal? MaxMonthlyHours = null, decimal WeekdayRate = 1.5m, decimal RestDayRate = 2m);

public class OvertimeRequest : Approvable
{
    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
    public string Reason { get; set; } = "";
    public decimal Multiplier { get; set; } = 1.5m;

    public static OvertimeRequest Create(Employee e, DateTime date, decimal hours, string? reason, DateTime today, OvertimeRules? rules = null)
    {
        rules ??= new OvertimeRules();
        if (hours <= 0 || hours > rules.MaxDailyHours) throw new DomainException("err.overtimeHours");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("err.reasonRequired");
        if (date.Date > today.Date) throw new DomainException("err.future");
        // Weekend and holiday overtime pays double.
        var mult = AttendanceCalculator.IsWeekend(date) ? rules.RestDayRate : rules.WeekdayRate;
        return new OvertimeRequest { EmployeeId = e.Id, Date = date.Date, Hours = hours, Reason = reason.Trim(), Multiplier = mult, Route = BuildRoute(e) };
    }
}

public class AttendanceCorrection : Approvable
{
    public DateTime Date { get; set; }
    public DateTime? OldCheckIn { get; set; }
    public DateTime? OldCheckOut { get; set; }
    public DateTime? NewCheckIn { get; set; }
    public DateTime? NewCheckOut { get; set; }
    public string Reason { get; set; } = "";

    public static AttendanceCorrection Create(Employee e, DateTime date, DateTime? oldIn, DateTime? oldOut,
        DateTime? newIn, DateTime? newOut, string? reason, DateTime today)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("err.reasonRequired");
        if (date.Date > today.Date) throw new DomainException("err.future");
        if (newIn is null) throw new DomainException("err.checkinRequired");
        if (newOut is not null && newOut <= newIn) throw new DomainException("err.checkoutBeforeIn");
        return new AttendanceCorrection { EmployeeId = e.Id, Date = date.Date, OldCheckIn = oldIn, OldCheckOut = oldOut,
            NewCheckIn = newIn, NewCheckOut = newOut, Reason = reason.Trim(), Route = BuildRoute(e, ApprovalStage.HR) };
    }
}
