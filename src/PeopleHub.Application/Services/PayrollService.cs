using System.Globalization;
using System.Text;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

namespace PeopleHub.Application.Services;

public class PayrollService(IRepository repo, ICurrentUser me, AuditService audit, NotificationService notify, AttendanceService attendance, AppClock clock)
{
    private void Require(string perm) { if (!me.Can(perm)) throw new DomainException("err.forbidden"); }

    public async Task<List<PayrollRun>> RunsAsync()
    {
        Require(Perm.PayView);
        return await repo.ToListAsync(repo.Query<PayrollRun>().OrderByDescending(r => r.Year).ThenByDescending(r => r.Month));
    }

    public async Task<(PayrollRun Run, List<PayrollLine> Lines, List<ApprovalAction> History)> RunAsync(int id)
    {
        Require(Perm.PayView);
        var run = await repo.FirstOrDefaultAsync(repo.Query<PayrollRun>().Where(r => r.Id == id)) ?? throw new DomainException("err.notFound");
        var lines = await repo.ToListAsync(repo.Query<PayrollLine>().Where(l => l.RunId == id).OrderBy(l => l.EmployeeId));
        var hist = await repo.ToListAsync(repo.Query<ApprovalAction>().Where(a => a.Kind == "Payroll" && a.RequestId == id).OrderBy(a => a.At));
        return (run, lines.OrderBy(l => l.Employee?.Code).ToList(), hist);
    }

    public async Task<int> CreateAsync(int year, int month)
    {
        Require(Perm.PayPrepare);
        if (month is < 1 or > 12 || year < 2000 || year > 9998) throw new DomainException("err.dates");
        if (await repo.AnyAsync(repo.Query<PayrollRun>().Where(r => r.Year == year && r.Month == month))) throw new DomainException("err.runExists");
        var run = new PayrollRun { Year = year, Month = month, CreatedBy = me.Email, CreatedAt = clock.UtcNow };
        repo.Add(run);
        await repo.SaveAsync();
        await BuildLinesAsync(run, new Dictionary<int, (decimal Bonus, decimal Other, string? Note)>());
        repo.Add(new ApprovalAction { Kind = "Payroll", RequestId = run.Id, Actor = me.Email, Action = "Created", At = clock.UtcNow });
        audit.Log("Create", "PayrollRun", $"{year}-{month:00}", "run created from active employees, attendance and approved requests");
        await repo.SaveAsync();
        return run.Id;
    }

    private async Task BuildLinesAsync(PayrollRun run, Dictionary<int, (decimal Bonus, decimal Other, string? Note)> manual)
    {
        var monthEnd = new DateTime(run.Year, run.Month, 1).AddMonths(1).AddDays(-1);
        var emps = await repo.ToListAsync(repo.Query<Employee>().Where(e => (e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave) && e.HireDate <= monthEnd));
        var summaries = await attendance.SummariesAsync(emps, run.Year, run.Month);
        var comps = await repo.ToListAsync(repo.Query<EmployeeSalaryComponent>());
        var advances = await repo.ToListAsync(repo.Query<AdvanceRequest>().Where(a => a.Status == RequestStatus.Approved));
        foreach (var e in emps)
        {
            var s = summaries[e.Id];
            var mine = comps.Where(c => c.EmployeeId == e.Id && c.Component != null).ToList();
            var allowances = mine.Where(c => c.Component!.Type == ComponentType.Earning).Sum(c => c.Amount);
            var fixedDed = mine.Where(c => c.Component!.Type == ComponentType.Deduction).Sum(c => c.Amount);
            var inst = advances.Where(a => a.EmployeeId == e.Id && a.DueIn(run.Year, run.Month)).Sum(a => a.InstallmentFor());
            manual.TryGetValue(e.Id, out var m);
            var line = new PayrollLine { RunId = run.Id, EmployeeId = e.Id, Employee = e, OvertimeHours = s.OvertimeWeightedHours, AdjustmentNote = m.Note };
            PayrollCalculator.Apply(line, new PayrollInput(e.BasicSalary, allowances, 0, m.Bonus, s.DeductibleDays, s.LateMinutes, s.OvertimeWeightedHours, inst, fixedDed + m.Other));
            repo.Add(line);
        }
    }

    public async Task RecalculateAsync(int runId)
    {
        Require(Perm.PayPrepare);
        var run = await repo.FirstOrDefaultAsync(repo.Query<PayrollRun>().Where(r => r.Id == runId)) ?? throw new DomainException("err.notFound");
        if (run.Status != PayrollStatus.Draft) throw new DomainException("err.payrollState");
        var old = await repo.ToListAsync(repo.Query<PayrollLine>().Where(l => l.RunId == runId));
        var comps = await repo.ToListAsync(repo.Query<EmployeeSalaryComponent>());
        var manual = old.ToDictionary(l => l.EmployeeId, l => (l.Bonus, l.OtherDeduction - comps.Where(c => c.EmployeeId == l.EmployeeId && c.Component?.Type == ComponentType.Deduction).Sum(c => c.Amount), l.AdjustmentNote));
        foreach (var l in old) repo.Remove(l);
        await repo.SaveAsync();
        await BuildLinesAsync(run, manual);
        audit.Log("Recalculate", "PayrollRun", $"{run.Year}-{run.Month:00}", "lines rebuilt, manual adjustments kept");
        await repo.SaveAsync();
    }

    public async Task AdjustLineAsync(int lineId, decimal bonus, decimal otherDeduction, string? note)
    {
        Require(Perm.PayPrepare);
        var l = await repo.FirstOrDefaultAsync(repo.Query<PayrollLine>().Where(x => x.Id == lineId)) ?? throw new DomainException("err.notFound");
        var run = await repo.FirstOrDefaultAsync(repo.Query<PayrollRun>().Where(r => r.Id == l.RunId)) ?? throw new DomainException("err.notFound");
        if (run.IsLocked || run.Status != PayrollStatus.Draft) throw new DomainException("err.payrollLocked");
        if (bonus < 0 || otherDeduction < 0) throw new DomainException("err.salary");
        if (string.IsNullOrWhiteSpace(note)) throw new DomainException("err.reasonRequired");
        var fixedDed = (await repo.ToListAsync(repo.Query<EmployeeSalaryComponent>().Where(c => c.EmployeeId == l.EmployeeId))).Where(c => c.Component?.Type == ComponentType.Deduction).Sum(c => c.Amount);
        var before = $"bonus {l.Bonus}, other {l.OtherDeduction}, net {l.Net}";
        PayrollCalculator.Apply(l, new PayrollInput(l.Basic, l.Allowances, 0, bonus, l.AbsentDays, l.LateMinutes, l.OvertimeHours, l.AdvanceInstallment, fixedDed + otherDeduction));
        l.AdjustmentNote = note.Trim();
        audit.Log("Adjust", "PayrollLine", lineId, $"{before} -> bonus {bonus}, other {otherDeduction}, net {l.Net}; {note}");
        await repo.SaveAsync();
    }

    private async Task<PayrollRun> LoadRunAsync(int id) =>
        await repo.FirstOrDefaultAsync(repo.Query<PayrollRun>().Where(r => r.Id == id)) ?? throw new DomainException("err.notFound");

    private void Trail(PayrollRun r, string action, string? comment = null)
    {
        repo.Add(new ApprovalAction { Kind = "Payroll", RequestId = r.Id, Actor = me.Email, Action = action, Comment = comment, At = clock.UtcNow });
        audit.Log(action, "PayrollRun", $"{r.Year}-{r.Month:00}", comment ?? "");
    }

    public async Task ReviewAsync(int id)
    {
        Require(Perm.PayHr);
        var r = await LoadRunAsync(id); r.Review(); Trail(r, "HRReviewed");
        await notify.NotifyPermAsync(Perm.PayFinance, "notif.payrollPending", $"{r.Year}-{r.Month:00}", $"/Payroll/Run/{id}");
        await repo.SaveAsync();
    }

    public async Task FinanceApproveAsync(int id)
    {
        Require(Perm.PayFinance);
        var r = await LoadRunAsync(id); r.FinanceApprove(); Trail(r, "FinanceApproved");
        await notify.NotifyPermAsync(Perm.PayFinalize, "notif.payrollPending", $"{r.Year}-{r.Month:00}", $"/Payroll/Run/{id}");
        await repo.SaveAsync();
    }

    public async Task FinalizeAsync(int id)
    {
        Require(Perm.PayFinalize);
        var r = await LoadRunAsync(id);
        r.Finalize();
        var lines = await repo.ToListAsync(repo.Query<PayrollLine>().Where(l => l.RunId == id));
        var advances = await repo.ToListAsync(repo.Query<AdvanceRequest>().Where(a => a.Status == RequestStatus.Approved));
        foreach (var l in lines.Where(l => l.AdvanceInstallment > 0))
        {
            var left = l.AdvanceInstallment;
            foreach (var a in advances.Where(a => a.EmployeeId == l.EmployeeId && a.DueIn(r.Year, r.Month)).OrderBy(a => a.CreatedAt))
            {
                var take = Math.Min(a.InstallmentFor(), left);
                a.Paid += take; left -= take;
                if (left <= 0) break;
            }
        }
        foreach (var l in lines) await notify.NotifyEmployeeAsync(l.EmployeeId, "notif.payslip", $"{r.Year}-{r.Month:00}", "/Payroll/MySlips");
        Trail(r, "Finalized", "run closed and locked");
        await repo.SaveAsync();
    }

    public async Task MarkPaidAsync(int id)
    {
        Require(Perm.PayPrepare);
        var r = await LoadRunAsync(id); r.MarkPaid(clock.UtcNow); Trail(r, "Paid");
        await repo.SaveAsync();
    }

    public async Task SendBackAsync(int id, string? reason)
    {
        Require(Perm.PaySendBack);
        var r = await LoadRunAsync(id); r.SendBack(reason); Trail(r, "SentBack", reason);
        await repo.SaveAsync();
    }

    public async Task ReopenAsync(int id, string? reason)
    {
        Require(Perm.PayReopen);
        var r = await LoadRunAsync(id); r.Reopen(reason); Trail(r, "Reopened", reason);
        await repo.SaveAsync();
    }

    public async Task<string> BankFileAsync(int id)
    {
        Require(Perm.PayExport);
        var (run, lines, _) = await RunAsync(id);
        if (!run.IsLocked) throw new DomainException("err.payrollState");
        var sb = new StringBuilder("Code,Name,Bank,IBAN,Net,Currency\n");
        foreach (var l in lines)
            sb.AppendLine(string.Join(',', Csv(l.Employee!.Code), Csv(l.Employee.FullName), Csv(l.Employee.BankName), Csv(l.Employee.Iban), l.Net.ToString("0.00", CultureInfo.InvariantCulture), "EGP"));
        audit.Log("Export", "PayrollRun", $"{run.Year}-{run.Month:00}", "bank transfer file generated");
        await repo.SaveAsync();
        return sb.ToString();
    }

    public static string Csv(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";

    public async Task<List<(PayrollRun Run, PayrollLine Line)>> MySlipsAsync()
    {
        var me2 = me.EmployeeId ?? -1;
        var lines = await repo.ToListAsync(repo.Query<PayrollLine>().Where(l => l.EmployeeId == me2));
        var runs = (await repo.ToListAsync(repo.Query<PayrollRun>().Where(r => r.Status == PayrollStatus.Finalized || r.Status == PayrollStatus.Paid))).ToDictionary(r => r.Id);
        return lines.Where(l => runs.ContainsKey(l.RunId)).Select(l => (runs[l.RunId], l)).OrderByDescending(x => x.Item1.Year).ThenByDescending(x => x.Item1.Month).ToList();
    }

    public async Task<(PayrollRun Run, PayrollLine Line)> SlipAsync(int lineId)
    {
        var l = await repo.FirstOrDefaultAsync(repo.Query<PayrollLine>().Where(x => x.Id == lineId)) ?? throw new DomainException("err.notFound");
        var r = await LoadRunAsync(l.RunId);
        var own = l.EmployeeId == me.EmployeeId;
        if (!own && !me.Can(Perm.PaySlips)) throw new DomainException("err.forbidden");
        if (own && !r.IsLocked) throw new DomainException("err.forbidden");
        return (r, l);
    }
}
