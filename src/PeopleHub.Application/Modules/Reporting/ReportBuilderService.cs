using System.Collections.Concurrent;
using System.Net.Sockets;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Domain.Security;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Reporting;

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

