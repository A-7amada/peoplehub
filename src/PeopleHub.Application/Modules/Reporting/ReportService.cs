using System.Globalization;
using System.Text;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

using PeopleHub.Application.Common;
using PeopleHub.Application.Modules.Attendance;
using PeopleHub.Application.Modules.Employees;
using PeopleHub.Application.Modules.Leave;
using PeopleHub.Application.Modules.Payroll;
using PeopleHub.Application.Modules.Requests;
namespace PeopleHub.Application.Modules.Reporting;

public record Kpi(string Key, string Value, string? Sub = null, string Tone = "");
public record ChartItem(string Label, string LabelAr, decimal Value);
public record DashboardData(List<Kpi> Kpis, List<ChartItem> ByDepartment, List<ChartItem> Attendance7Days, List<ChartItem> PayrollTrend,
    List<ChartItem> ByStatus, List<(Contract Contract, Employee Employee)> Expiring, int Pending, List<Employee> NewHires, bool CompanyWide);

public record ReportTable(string Key, List<string> Columns, List<List<string>> Rows);

public class ReportService(IRepository repo, AccessScope scope, ICurrentUser me, AttendanceService attendance, ApprovalService approvals,
    EmployeeService employees, LeaveService leave, AppClock clock)
{
    private const string Money = "N2";
    private static string F(decimal v) => v.ToString(Money, CultureInfo.InvariantCulture);

    public async Task<DashboardData> DashboardAsync()
    {
        var ids = await scope.VisibleEmployeeIdsAsync();
        var all = await repo.ToListAsync(repo.Query<Employee>());
        var vis = all.Where(e => ids == null || ids.Contains(e.Id)).ToList();
        var working = vis.Where(e => e.IsWorking).ToList();
        var today = clock.Today;
        var onLeave = (await leave.OnLeaveAsync(today)).Count(x => ids == null || ids.Contains(x.Employee.Id));
        var recsToday = await repo.ToListAsync(repo.Query<AttendanceRecord>().Where(r => r.Date == today));
        var present = recsToday.Count(r => r.Status != AttendanceStatus.Absent && (ids == null || ids.Contains(r.EmployeeId)));
        var late = recsToday.Count(r => r.Status == AttendanceStatus.Late && (ids == null || ids.Contains(r.EmployeeId)));
        var pending = await approvals.PendingForMeAsync();
        var company = ids == null;

        var kpis = new List<Kpi>
        {
            new("dash.headcount", working.Count.ToString(), $"{vis.Count - working.Count}", "blue"),
            new("dash.presentToday", AttendanceCalculator.IsWeekend(today) ? "–" : present.ToString(), late > 0 ? $"{late}" : null, "green"),
            new("dash.onLeave", onLeave.ToString(), null, "amber"),
            new("dash.pending", pending.ToString(), null, "violet"),
        };

        var depts = await repo.ToListAsync(repo.Query<Department>());
        var byDept = working.GroupBy(e => e.DepartmentId).Select(g => { var d = depts.First(x => x.Id == g.Key); return new ChartItem(d.Name, d.NameAr, g.Count()); })
            .OrderByDescending(c => c.Value).Take(8).ToList();
        var byStatus = vis.GroupBy(e => e.Status).Select(g => new ChartItem(g.Key.ToString(), g.Key.ToString(), g.Count())).ToList();

        // attendance over the last 7 working days, % present
        var trend = new List<ChartItem>();
        var days = Enumerable.Range(0, 14).Select(i => today.AddDays(-i)).Where(d => !AttendanceCalculator.IsWeekend(d) && d < today).Take(7).Reverse().ToList();
        var recs = await repo.ToListAsync(repo.Query<AttendanceRecord>().Where(r => r.Date >= days.First() && r.Date < today));
        foreach (var d in days)
        {
            var n = working.Count(e => e.HireDate <= d);
            var p = recs.Count(r => r.Date == d && r.Status != AttendanceStatus.Absent && (ids == null || ids.Contains(r.EmployeeId)));
            trend.Add(new ChartItem(d.ToString("dd/MM"), d.ToString("dd/MM"), n == 0 ? 0 : Math.Round(100m * p / n)));
        }

        var payTrend = new List<ChartItem>();
        if (me.Can(Perm.ReportsFin))
        {
            var runs = (await repo.ToListAsync(repo.Query<PayrollRun>())).OrderBy(r => r.Year).ThenBy(r => r.Month).TakeLast(6).ToList();
            var lines = await repo.ToListAsync(repo.Query<PayrollLine>());
            foreach (var r in runs) payTrend.Add(new ChartItem($"{r.Year}-{r.Month:00}", $"{r.Year}-{r.Month:00}", lines.Where(l => l.RunId == r.Id).Sum(l => l.Gross)));
            var total = working.Sum(e => e.BasicSalary);
            kpis.Add(new("dash.payroll", total.ToString("N0", CultureInfo.InvariantCulture), null, "rose"));
        }

        var expiring = me.Can(Perm.EmpManage) ? await employees.ExpiringContractsAsync(60) : [];
        var hires = working.OrderByDescending(e => e.HireDate).Take(4).Select(e=>new Employee{Id=e.Id,Code=e.Code,FullName=e.FullName,FullNameAr=e.FullNameAr,HireDate=e.HireDate,JobTitleId=e.JobTitleId,JobTitle=e.JobTitle==null?null:new JobTitle{Id=e.JobTitle.Id,Name=e.JobTitle.Name,NameAr=e.JobTitle.NameAr}}).ToList();
        return new DashboardData(kpis, byDept, trend, payTrend, byStatus, expiring, pending, hires, company);
    }

    public static readonly string[] ReportKeys = ["employees", "attendance", "leave", "payroll", "turnover"];

    public async Task<ReportTable> BuildAsync(string key, int year, int month)
    {
        if (!me.Can(Perm.Reports)) throw new DomainException("err.forbidden");
        if (!ReportKeys.Contains(key) || year < 1900 || year > 9998 || month < 1 || month > 12) throw new DomainException("err.required");
        var ids = await scope.VisibleEmployeeIdsAsync();
        var emps = (await repo.ToListAsync(repo.Query<Employee>().OrderBy(e => e.Code))).Where(e => ids == null || ids.Contains(e.Id)).ToList();
        switch (key)
        {
            case "employees":
                return new("employees", ["Code", "Name", "Department", "Title", "Branch", "Status", "Hire date", "Manager"],
                    emps.Select(e => new List<string> { e.Code, e.FullName, e.Department?.Name ?? "", e.JobTitle?.Name ?? "", e.Branch?.Name ?? "", e.Status.ToString(),
                        e.HireDate.ToString("yyyy-MM-dd"), emps.FirstOrDefault(m => m.Id == e.ManagerId)?.FullName ?? "" }).ToList());
            case "attendance":
                var w = emps.Where(e => e.IsWorking).ToList();
                var s = await attendance.SummariesAsync(w, year, month);
                return new("attendance", ["Code", "Name", "Working days", "Present", "Late (count)", "Late (min)", "Absent", "Excused", "Leave days", "Overtime h"],
                    w.Select(e => { var x = s[e.Id]; return new List<string> { e.Code, e.FullName, x.WorkingDays.ToString(), x.PresentDays.ToString(), x.LateCount.ToString(), x.LateMinutes.ToString(),
                        x.AbsentDays.ToString(), x.ExcusedAbsences.ToString(), x.LeaveDays.ToString(), x.OvertimeHours.ToString("0.#", CultureInfo.InvariantCulture) }; }).ToList());
            case "leave":
                var bals = (await repo.ToListAsync(repo.Query<LeaveBalance>().Where(b => b.Year == year))).Where(b => emps.Any(e => e.Id == b.EmployeeId)).Where(b=>me.EmployeeId==b.EmployeeId||me.Can(Perm.LeaveEvidence)).ToList();
                return new("leave", ["Code", "Name", "Leave type", "Entitled", "Carried over", "Used", "Remaining"],
                    bals.OrderBy(b => b.EmployeeId).Select(b => { var e = emps.First(x => x.Id == b.EmployeeId); return new List<string> { e.Code, e.FullName, me.EmployeeId==e.Id||me.Can(Perm.LeaveEvidence)?b.LeaveType?.Name??"":"Private leave", b.Entitled.ToString(), b.CarriedOver.ToString(), b.Used.ToString(), b.Remaining.ToString() }; }).ToList());
            case "payroll":
                if (!me.Can(Perm.ReportsFin)) throw new DomainException("err.forbidden");
                var run = await repo.FirstOrDefaultAsync(repo.Query<PayrollRun>().Where(r => r.Year == year && r.Month == month));
                var lines = run == null ? [] : await repo.ToListAsync(repo.Query<PayrollLine>().Where(l => l.RunId == run.Id));
                var byDept = lines.GroupBy(l => string.IsNullOrEmpty(l.EmployeeCodeSnapshot)?l.Employee!.Department?.Name ?? "":l.DepartmentSnapshot).OrderBy(g => g.Key);
                return new("payroll", ["Department", "Employees", "Gross", "Deductions", "Net"],
                    byDept.Select(g => new List<string> { g.Key, g.Count().ToString(), F(g.Sum(l => l.Gross)), F(g.Sum(l => l.TotalDeductions)), F(g.Sum(l => l.Net)) }).ToList());
            default: // turnover
                var left = emps.Where(e => !e.IsWorking).ToList();
                var hist = await repo.ToListAsync(repo.Query<StatusHistory>());
                return new("turnover", ["Code", "Name", "Department", "Status", "Date", "Reason"],
                    left.Select(e => { var h = hist.Where(x => x.EmployeeId == e.Id).OrderByDescending(x => x.At).FirstOrDefault(); return new List<string> { e.Code, e.FullName, e.Department?.Name ?? "", e.Status.ToString(), h?.At.ToString("yyyy-MM-dd") ?? "", me.EmployeeId==e.Id||me.Can(Perm.EmpIdentity)?h?.Reason??"":"" }; }).ToList());
        }
    }

    // Quoting alone does not stop spreadsheet formula interpretation. Keep this
    // separate from the bank interchange serializer, whose values must stay exact.
    public static string SpreadsheetCell(string? value)
    {
        value??="";var trimmed=value.TrimStart();
        if((trimmed.Length>0&&"=+-@".Contains(trimmed[0]))||value.Any(c=>c is '\t' or '\r' or '\n'))value="'"+value;
        return PayrollService.Csv(value);
    }
    public static string ToCsv(ReportTable t)
    {
        var sb = new StringBuilder("\uFEFF").AppendLine(string.Join(',', t.Columns.Select(SpreadsheetCell)));
        foreach (var r in t.Rows) sb.AppendLine(string.Join(',', r.Select(SpreadsheetCell)));
        return sb.ToString();
    }
}
