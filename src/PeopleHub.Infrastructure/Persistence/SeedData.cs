using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Security;

namespace PeopleHub.Infrastructure.Persistence;

/// <summary>All data is fictional. Names, IBANs, national IDs and numbers are made up for the demo.</summary>
public static class SeedData
{
    public const string HrEmail = "hr@peoplehub.local";
    public const string EmployeeEmail = "employee@peoplehub.local";
    public static readonly (string Email, string Role, string Code)[] DemoUsers =
    [
        ("hrspecialist@peoplehub.local", Roles.HRSpecialist, "PH-009"),
        ("attendance@peoplehub.local", Roles.AttendanceOfficer, "PH-015"),
        ("payroll@peoplehub.local", Roles.PayrollOfficer, "PH-011"),
        ("finance@peoplehub.local", Roles.FinanceManager, "PH-010"),
        ("manager@peoplehub.local", Roles.DepartmentManager, "PH-004"),
        (EmployeeEmail, Roles.Employee, "PH-006"),
        ("ceo@peoplehub.local", Roles.TopManagement, "PH-001"),
    ];

    /// <summary>The HR password comes from configuration outside Development; without it the HR account stays locked.</summary>
    public static async Task InitializeAsync(AppDbContext db, string? hrPassword, string demoPassword, DateTime today)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Set<Branch>().AnyAsync()) return;
        today = today.Date;

        var branches = new[] { new Branch { Name = "Cairo HQ", NameAr = "المقر الرئيسي - القاهرة", City = "Cairo" },
            new Branch { Name = "Alexandria", NameAr = "فرع الإسكندرية", City = "Alexandria" },
            new Branch { Name = "10th of Ramadan", NameAr = "فرع العاشر من رمضان", City = "Sharqia" } };
        db.AddRange(branches);

        var jt = new Dictionary<string, JobTitle>();
        JobTitle T(string key, string en, string ar, int grade, decimal min, decimal max) => jt[key] = new JobTitle { Name = en, NameAr = ar, Grade = grade, MinSalary = min, MaxSalary = max, Description = en };
        db.AddRange(T("ceo", "Chief Executive Officer", "الرئيس التنفيذي", 10, 90000, 150000), T("cto", "Chief Technology Officer", "رئيس قطاع التقنية", 9, 80000, 130000),
            T("mgr", "Department Manager", "مدير إدارة", 7, 45000, 75000), T("sr", "Senior Specialist", "أخصائي أول", 5, 28000, 50000),
            T("spec", "Specialist", "أخصائي", 4, 16000, 32000), T("jr", "Junior Specialist", "أخصائي مبتدئ", 2, 9000, 18000),
            T("off", "Officer", "مسؤول", 3, 12000, 26000));
        await db.SaveChangesAsync();

        var dep = new Dictionary<string, Department>();
        Department D(string key, string en, string ar, string cc, int target, string? parent = null) =>
            dep[key] = new Department { Name = en, NameAr = ar, CostCenter = cc, TargetHeadcount = target, ParentId = parent == null ? null : dep[parent].Id };
        foreach (var (k, en, ar, cc, tg, p) in new (string, string, string, string, int, string?)[] {
            ("exec", "Executive Office", "المكتب التنفيذي", "CC-100", 2, null), ("tech", "Technology", "التقنية", "CC-200", 6, "exec"),
            ("eng", "Engineering", "الهندسة", "CC-210", 5, "tech"), ("hr", "Human Resources", "الموارد البشرية", "CC-300", 3, "exec"),
            ("fin", "Finance", "المالية", "CC-400", 3, "exec"), ("sales", "Sales & Marketing", "المبيعات والتسويق", "CC-500", 4, "exec"),
            ("ops", "Operations", "العمليات", "CC-600", 2, "exec") })
        { var d = D(k, en, ar, cc, tg, p); db.Add(d); await db.SaveChangesAsync(); }

        var shifts = new[] { new Shift { Name = "Day shift 09:00-17:00", NameAr = "الوردية الصباحية 9-5", Start = new(9, 0, 0), End = new(17, 0, 0), GraceMinutes = 10, MaxLateMinutes = 120 },
            new Shift { Name = "Early shift 08:00-16:00", NameAr = "الوردية المبكرة 8-4", Start = new(8, 0, 0), End = new(16, 0, 0), GraceMinutes = 10, MaxLateMinutes = 120 } };
        db.AddRange(shifts);

        foreach (var y in new[] { today.Year, today.Year + 1 })
            foreach (var (m, d, en, ar) in new[] { (1, 7, "Coptic Christmas", "عيد الميلاد المجيد"), (1, 25, "Revolution Day", "عيد الثورة والشرطة"), (4, 25, "Sinai Liberation Day", "عيد تحرير سيناء"),
                (5, 1, "Labour Day", "عيد العمال"), (7, 23, "July 23 Revolution", "ثورة 23 يوليو"), (10, 6, "Armed Forces Day", "عيد القوات المسلحة") })
                db.Add(new Holiday { Date = new DateTime(y, m, d), Name = en, NameAr = ar });

        var types = new[] {
            new LeaveType { Code = "ANN", Name = "Annual leave", NameAr = "إجازة سنوية", DefaultDays = 21, MaxCarryOver = 7 },
            new LeaveType { Code = "SICK", Name = "Sick leave", NameAr = "إجازة مرضية", DefaultDays = 15, RequiresDocument = true, AllowBackdated = true },
            new LeaveType { Code = "CAS", Name = "Casual leave", NameAr = "إجازة عارضة", DefaultDays = 6, AllowBackdated = true },
            new LeaveType { Code = "UNP", Name = "Unpaid leave", NameAr = "إجازة بدون أجر", DefaultDays = 0, IsPaid = false, DeductsBalance = false } };
        db.AddRange(types);

        var comps = new[] {
            new SalaryComponent { Code = "TRN", Name = "Transport allowance", NameAr = "بدل انتقال", Type = ComponentType.Earning },
            new SalaryComponent { Code = "HOU", Name = "Housing allowance", NameAr = "بدل سكن", Type = ComponentType.Earning },
            new SalaryComponent { Code = "MED", Name = "Medical plan contribution", NameAr = "اشتراك التأمين الطبي", Type = ComponentType.Deduction },
            new SalaryComponent { Code = "UNI", Name = "Social club fee", NameAr = "اشتراك النادي الاجتماعي", Type = ComponentType.Deduction } };
        db.AddRange(comps);
        await db.SaveChangesAsync();

        // code, en, ar, dept, title, manager code, salary, years of service, branch idx, shift idx, gender
        var rows = new (string c, string en, string ar, string d, string t, string? m, decimal sal, double yrs, int br, int sh, Gender g)[] {
            ("PH-001", "Nadia Fouad", "نادية فؤاد", "exec", "ceo", null, 120000, 7, 0, 0, Gender.Female),
            ("PH-002", "Karim Mansour", "كريم منصور", "tech", "cto", "PH-001", 95000, 6, 0, 0, Gender.Male),
            ("PH-003", "Salma Adel", "سلمى عادل", "eng", "sr", "PH-004", 42000, 4, 0, 0, Gender.Female),
            ("PH-004", "Omar Sherif", "عمر شريف", "eng", "mgr", "PH-002", 62000, 5, 0, 0, Gender.Male),
            ("PH-005", "Laila Hassan", "ليلى حسن", "eng", "spec", "PH-004", 30000, 2.5, 0, 0, Gender.Female),
            ("PH-006", "Youssef Ibrahim", "يوسف إبراهيم", "eng", "spec", "PH-004", 34000, 3, 0, 0, Gender.Male),
            ("PH-007", "Mariam Khaled", "مريم خالد", "eng", "jr", "PH-004", 15000, 0.8, 0, 0, Gender.Female),
            ("PH-008", "Hana Tarek", "هناء طارق", "hr", "mgr", "PH-001", 58000, 5, 0, 0, Gender.Female),
            ("PH-009", "Mostafa Reda", "مصطفى رضا", "hr", "spec", "PH-008", 24000, 2, 0, 0, Gender.Male),
            ("PH-010", "Dina Samir", "دينا سمير", "fin", "mgr", "PH-001", 64000, 6, 0, 0, Gender.Female),
            ("PH-011", "Amr Zaki", "عمرو زكي", "fin", "sr", "PH-010", 33000, 3.5, 0, 0, Gender.Male),
            ("PH-012", "Rania Abbas", "رانيا عباس", "sales", "mgr", "PH-001", 55000, 4, 1, 0, Gender.Female),
            ("PH-013", "Tamer Nabil", "تامر نبيل", "sales", "spec", "PH-012", 22000, 1.5, 1, 0, Gender.Male),
            ("PH-014", "Farida Ali", "فريدة علي", "sales", "spec", "PH-012", 20000, 1, 1, 0, Gender.Female),
            ("PH-015", "Hossam Gamal", "حسام جمال", "ops", "off", "PH-001", 19000, 3, 2, 1, Gender.Male),
            ("PH-016", "Sherif Lotfy", "شريف لطفي", "ops", "jr", "PH-015", 13000, 2, 2, 1, Gender.Male) };

        var emps = new Dictionary<string, Employee>();
        var rnd = new Random(2026);
        var i = 0;
        foreach (var r in rows)
        {
            i++;
            var e = new Employee
            {
                Code = r.c, FullName = r.en, FullNameAr = r.ar, Gender = r.g, MaritalStatus = i % 2 == 0 ? "Married" : "Single",
                NationalId = $"29{rnd.Next(10, 99)}0{rnd.Next(1, 9)}{rnd.Next(10, 28)}01{rnd.Next(1000, 9999)}{rnd.Next(1, 9)}",
                BirthDate = today.AddYears(-(26 + i)).AddDays(i * 17), Address = "Fictional address " + i, Phone = $"+2010{rnd.Next(10000000, 99999999)}",
                Email = $"{r.en.Split(' ')[0].ToLower()}@peoplehub.local", HireDate = today.AddDays(-(int)(r.yrs * 365)),
                JobTitleId = jt[r.t].Id, DepartmentId = dep[r.d].Id, BranchId = branches[r.br].Id, ShiftId = shifts[r.sh].Id,
                BankName = i % 2 == 0 ? "Demo Bank of Cairo" : "Nile Sample Bank", Iban = $"EG{rnd.Next(10, 99)}0000{rnd.Next(10000000, 99999999)}{rnd.Next(10000000, 99999999)}",
                InsuranceNumber = $"INS-{rnd.Next(100000, 999999)}", BasicSalary = r.sal
            };
            if (r.c == "PH-016") e.Status = EmployeeStatus.Resigned;
            emps[r.c] = e; db.Add(e);
        }
        await db.SaveChangesAsync();
        foreach (var r in rows) if (r.m != null) emps[r.c].ManagerId = emps[r.m].Id;
        dep["exec"].ManagerId = emps["PH-001"].Id; dep["tech"].ManagerId = emps["PH-002"].Id; dep["eng"].ManagerId = emps["PH-004"].Id;
        dep["hr"].ManagerId = emps["PH-008"].Id; dep["fin"].ManagerId = emps["PH-010"].Id; dep["sales"].ManagerId = emps["PH-012"].Id; dep["ops"].ManagerId = emps["PH-015"].Id;
        emps["PH-016"].Status = EmployeeStatus.Resigned;
        await db.SaveChangesAsync();

        // users
        db.Add(new AppUser { Email = HrEmail, RoleList = $"{Roles.SystemAdmin},{Roles.HRManager}", EmployeeId = emps["PH-008"].Id,
            PasswordHash = PasswordHasher.Hash(string.IsNullOrEmpty(hrPassword) ? Guid.NewGuid().ToString("N") : hrPassword), IsActive = !string.IsNullOrEmpty(hrPassword) });
        foreach (var (mail, role, code) in DemoUsers)
            db.Add(new AppUser { Email = mail, RoleList = role == Roles.DepartmentManager ? $"{Roles.DepartmentManager},{Roles.Employee}" : role, EmployeeId = emps[code].Id, PasswordHash = PasswordHasher.Hash(demoPassword) });

        // contracts: permanent for most, fixed term for a few (one expiring soon so the dashboard has an alert)
        foreach (var e in emps.Values)
        {
            var fixedTerm = e.Code is "PH-007" or "PH-014" or "PH-013";
            var end = e.Code == "PH-007" ? today.AddDays(26) : e.Code == "PH-014" ? today.AddDays(48) : today.AddDays(210);
            db.Add(fixedTerm ? Contract.Create(e.Id, ContractType.FixedTerm, today.AddYears(-1).AddDays(-30), end, "Annual contract") : Contract.Create(e.Id, ContractType.Permanent, e.HireDate, null, ""));
            db.Add(new CareerEvent { EmployeeId = e.Id, Type = CareerEventType.TitleChange, Date = e.HireDate, Details = "Joined as " + e.JobTitle?.Name, RecordedBy = "system" });
        }
        db.Add(new CareerEvent { EmployeeId = emps["PH-003"].Id, Type = CareerEventType.Promotion, Date = today.AddMonths(-8), Details = "Promoted to Senior Specialist", RecordedBy = HrEmail });
        db.Add(new CareerEvent { EmployeeId = emps["PH-006"].Id, Type = CareerEventType.SalaryChange, Date = today.AddMonths(-5), Details = "Annual increase 8%", RecordedBy = HrEmail });
        db.Add(new StatusHistory { EmployeeId = emps["PH-016"].Id, From = EmployeeStatus.Active, To = EmployeeStatus.Resigned, At = today.AddDays(-40), ChangedBy = HrEmail, Reason = "Resigned for relocation" });

        // salary components
        foreach (var e in emps.Values.Where(e => e.IsWorking))
        {
            db.Add(new EmployeeSalaryComponent { EmployeeId = e.Id, ComponentId = comps[0].Id, Amount = e.BasicSalary > 50000 ? 1500 : 800 });
            if (e.BasicSalary >= 40000) db.Add(new EmployeeSalaryComponent { EmployeeId = e.Id, ComponentId = comps[1].Id, Amount = 2500 });
            db.Add(new EmployeeSalaryComponent { EmployeeId = e.Id, ComponentId = comps[2].Id, Amount = 150 });
        }
        await db.SaveChangesAsync();

        // approved leave in the past/current months (attendance skips those days)
        LeaveRequest Leave(string code, LeaveType t, DateTime s, DateTime e, RequestStatus st, string route, int stage, string? reason = null, string? doc = null)
        {
            var emp = emps[code];
            var l = new LeaveRequest { EmployeeId = emp.Id, LeaveTypeId = t.Id, StartDate = s.Date, EndDate = e.Date, Days = AttendanceCalculator.CountWorkingDays(s, e, db.Set<Holiday>().Local.Select(h => h.Date)),
                Reason = reason, DocumentName = doc, Route = route, StageIndex = stage, Status = st, CreatedAt = s.AddDays(-5).ToUniversalTime(), DecidedAt = st == RequestStatus.Pending ? null : s.AddDays(-3).ToUniversalTime() };
            db.Add(l); return l;
        }
        string M(string c) => ApprovalStage.Manager.ToString();
        string MH(string c) => $"{ApprovalStage.Manager},{ApprovalStage.HR}";
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var prev1 = monthStart.AddMonths(-1);
        DateTime NextWork(DateTime d) { while (AttendanceCalculator.IsWeekend(d)) d = d.AddDays(1); return d; }
        var approvedLeaves = new List<LeaveRequest>
        {
            Leave("PH-005", types[0], prev1.AddDays(8), prev1.AddDays(11), RequestStatus.Approved, MH(""), 2, "Family event"),
            Leave("PH-014", types[1], prev1.AddDays(19), prev1.AddDays(20), RequestStatus.Approved, MH(""), 2, "Flu", "medical-note.pdf"),
            Leave("PH-009", types[3], prev1.AddDays(14), prev1.AddDays(15), RequestStatus.Approved, MH(""), 2, "Personal"),
            Leave("PH-003", types[0], monthStart.AddDays(3), monthStart.AddDays(5), RequestStatus.Approved, MH(""), 2, "Short trip"),
        };
        // pending requests so approval inboxes are not empty
        var nextWork = NextWork(today.AddDays(5));
        Leave("PH-006", types[0], nextWork, nextWork.AddDays(4), RequestStatus.Pending, MH(""), 0, "Visiting family");
        Leave("PH-007", types[2], NextWork(today.AddDays(2)), NextWork(today.AddDays(2)), RequestStatus.Pending, M(""), 0, "Personal errand");
        Leave("PH-013", types[0], NextWork(today.AddDays(12)), NextWork(today.AddDays(12)).AddDays(3), RequestStatus.Pending, MH(""), 1, "Wedding");
        Leave("PH-011", types[0], NextWork(today.AddDays(9)), NextWork(today.AddDays(9)), RequestStatus.Rejected, M(""), 0, "Needed at month end");
        await db.SaveChangesAsync();

        // leave balances for current and previous year
        foreach (var e in emps.Values.Where(e => e.IsWorking))
            foreach (var t in types.Where(t => t.DeductsBalance))
            {
                var used = approvedLeaves.Where(l => l.EmployeeId == e.Id && l.LeaveTypeId == t.Id && l.StartDate.Year == today.Year).Sum(l => l.Days);
                var extra = t.Code == "ANN" ? (e.Id % 5) * 2 : t.Code == "CAS" ? e.Id % 3 : 0;
                db.Add(new LeaveBalance { EmployeeId = e.Id, LeaveTypeId = t.Id, Year = today.Year, Entitled = t.DefaultDays, CarriedOver = t.Code == "ANN" ? Math.Min(t.MaxCarryOver, e.Id % 4 + 2) : 0, Used = used + extra });
            }

        // attendance for the last 3 months up to yesterday
        var from = monthStart.AddMonths(-2);
        var holidays = db.Set<Holiday>().Local.Select(h => h.Date.Date).ToHashSet();
        var records = new List<AttendanceRecord>();
        foreach (var e in emps.Values)
        {
            var shift = shifts[rows.First(r => r.c == e.Code).sh];
            var end = e.Status == EmployeeStatus.Resigned ? today.AddDays(-40) : today.AddDays(-1);
            for (var d = from; d <= end; d = d.AddDays(1))
            {
                if (AttendanceCalculator.IsWeekend(d) || holidays.Contains(d) || d < e.HireDate) continue;
                if (approvedLeaves.Any(l => l.EmployeeId == e.Id && l.StartDate <= d && l.EndDate >= d)) continue;
                var roll = rnd.NextDouble();
                if (roll < 0.015) continue; // unexcused absence
                var late = roll < 0.10 ? rnd.Next(14, 55) : 0;
                var cin = d + shift.Start + TimeSpan.FromMinutes(late > 0 ? late : rnd.Next(-12, 8));
                var otPlus = rnd.NextDouble() < 0.07 ? rnd.Next(40, 130) : rnd.Next(-5, 25);
                var cout = d + shift.End + TimeSpan.FromMinutes(Math.Max(-4, otPlus));
                var rec = new AttendanceRecord { EmployeeId = e.Id, Date = d, CheckIn = cin, CheckOut = cout, Source = rnd.NextDouble() < 0.6 ? AttendanceSource.Fingerprint : AttendanceSource.MobileApp };
                rec.Recompute(shift); records.Add(rec);
            }
        }
        db.AddRange(records);

        // approved overtime from the previous month
        var otDays = records.Where(r => r.OvertimeMinutes > 0 && r.Date >= prev1 && r.Date < monthStart).GroupBy(r => r.EmployeeId).Select(g => g.First()).Take(4).ToList();
        foreach (var r in otDays)
        {
            var hrs = Math.Round(r.OvertimeMinutes / 60m * 2, MidpointRounding.AwayFromZero) / 2;
            if (hrs <= 0) hrs = 1;
            db.Add(new OvertimeRequest { EmployeeId = r.EmployeeId, Date = r.Date, Hours = hrs, Reason = "Release support", Multiplier = 1.5m, Route = ApprovalStage.Manager.ToString(), StageIndex = 1, Status = RequestStatus.Approved, DecidedAt = r.Date.AddDays(2).ToUniversalTime() });
        }
        var recent = records.Where(r => r.OvertimeMinutes > 0 && r.Date >= today.AddDays(-10)).Take(1).ToList();
        foreach (var r in recent)
            db.Add(new OvertimeRequest { EmployeeId = r.EmployeeId, Date = r.Date, Hours = 1.5m, Reason = "Production issue", Route = ApprovalStage.Manager.ToString() });
        await db.SaveChangesAsync();
        // fall back so there is always a pending overtime request
        if (!recent.Any())
            db.Add(new OvertimeRequest { EmployeeId = emps["PH-005"].Id, Date = today.AddDays(-2), Hours = 2, Reason = "Production issue", Route = ApprovalStage.Manager.ToString() });

        // a pending attendance correction (manager first, then HR) and a pending advance
        db.Add(new AttendanceCorrection { EmployeeId = emps["PH-013"].Id, Date = NextWorkBack(today.AddDays(-3)), OldCheckIn = null, NewCheckIn = NextWorkBack(today.AddDays(-3)) + new TimeSpan(9, 5, 0),
            NewCheckOut = NextWorkBack(today.AddDays(-3)) + new TimeSpan(17, 10, 0), Reason = "Forgot to check in, was at a client meeting", Route = $"{ApprovalStage.Manager},{ApprovalStage.HR}" });
        db.Add(new AdvanceRequest { EmployeeId = emps["PH-006"].Id, Amount = 12000, Installments = 4, StartYear = today.AddMonths(1).Year, StartMonth = today.AddMonths(1).Month, Reason = "Family expense", Route = $"{ApprovalStage.HR},{ApprovalStage.Finance}" });
        db.Add(new AdvanceRequest { EmployeeId = emps["PH-014"].Id, Amount = 6000, Installments = 3, StartYear = prev1.Year, StartMonth = prev1.Month, Paid = 4000, Reason = "Rent", Route = $"{ApprovalStage.HR},{ApprovalStage.Finance}", StageIndex = 2, Status = RequestStatus.Approved, DecidedAt = prev1.AddDays(-3).ToUniversalTime() });
        await db.SaveChangesAsync();

        // approval history for the seeded decided requests
        foreach (var l in db.Set<LeaveRequest>().Local.Where(l => l.Status != RequestStatus.Pending))
            db.Add(new ApprovalAction { Kind = "Leave", RequestId = l.Id, Actor = l.Status == RequestStatus.Rejected ? "omar@peoplehub.local" : "omar@peoplehub.local", Action = l.Status == RequestStatus.Rejected ? "Rejected:Manager" : "Approved:Manager", Comment = l.Status == RequestStatus.Rejected ? "Needed at month end" : null, At = l.DecidedAt ?? DateTime.UtcNow });

        // payroll for the two previous months
        var allLines = new List<PayrollLine>();
        foreach (var (pm, status) in new[] { (monthStart.AddMonths(-2), PayrollStatus.Paid), (prev1, PayrollStatus.Finalized) })
        {
            var run = new PayrollRun { Year = pm.Year, Month = pm.Month, Status = status, CreatedBy = "payroll@peoplehub.local", CreatedAt = pm.AddMonths(1).AddDays(-2).ToUniversalTime(), PaidAt = status == PayrollStatus.Paid ? pm.AddMonths(1).AddDays(1).ToUniversalTime() : null };
            db.Add(run); await db.SaveChangesAsync();
            var last = pm.AddMonths(1).AddDays(-1);
            foreach (var e in emps.Values.Where(e => e.HireDate <= last && (e.Status != EmployeeStatus.Resigned || pm < monthStart.AddMonths(-1))))
            {
                var mine = records.Where(r => r.EmployeeId == e.Id && r.Date >= pm && r.Date <= last).ToList();
                var lateMin = mine.Sum(r => r.LateMinutes);
                var workDays = AttendanceCalculator.CountWorkingDays(pm, last, holidays);
                var leaveDays = approvedLeaves.Where(l => l.EmployeeId == e.Id && l.StartDate >= pm && l.StartDate <= last).Sum(l => l.Days);
                var present = mine.Count(r => r.Status != AttendanceStatus.Absent);
                var absent = Math.Max(0, workDays - leaveDays - present);
                var unpaid = approvedLeaves.Where(l => l.EmployeeId == e.Id && l.LeaveTypeId == types[3].Id && l.StartDate >= pm && l.StartDate <= last).Sum(l => l.Days);
                var ot = db.Set<OvertimeRequest>().Local.Where(o => o.EmployeeId == e.Id && o.Status == RequestStatus.Approved && o.Date >= pm && o.Date <= last).Sum(o => o.Hours * o.Multiplier);
                var earn = e.BasicSalary > 50000 ? 1500 + 2500 : e.BasicSalary >= 40000 ? 800 + 2500 : 800;
                var inst = e.Code == "PH-014" ? 2000m : 0m;
                var line = new PayrollLine { RunId = run.Id, EmployeeId = e.Id };
                PayrollCalculator.Apply(line, new PayrollInput(e.BasicSalary, earn, 0, 0, absent + unpaid, lateMin, ot, pm == prev1 ? inst : 0, 150));
                allLines.Add(line);
            }
            db.AddRange(allLines.Where(l => l.Id == 0));
            await db.SaveChangesAsync();
            db.Add(new ApprovalAction { Kind = "Payroll", RequestId = run.Id, Actor = "payroll@peoplehub.local", Action = "Created", At = run.CreatedAt });
            db.Add(new ApprovalAction { Kind = "Payroll", RequestId = run.Id, Actor = HrEmail, Action = "HRReviewed", At = run.CreatedAt.AddDays(1) });
            db.Add(new ApprovalAction { Kind = "Payroll", RequestId = run.Id, Actor = "finance@peoplehub.local", Action = "FinanceApproved", At = run.CreatedAt.AddDays(1.5) });
            db.Add(new ApprovalAction { Kind = "Payroll", RequestId = run.Id, Actor = "ceo@peoplehub.local", Action = "Finalized", At = run.CreatedAt.AddDays(2) });
            if (status == PayrollStatus.Paid) db.Add(new ApprovalAction { Kind = "Payroll", RequestId = run.Id, Actor = "payroll@peoplehub.local", Action = "Paid", At = run.PaidAt!.Value });
        }
        // welcome notifications + sample audit entries
        var users = await db.Set<AppUser>().ToListAsync();
        foreach (var u in users)
            db.Add(new Notification { UserId = u.Id, TitleKey = "notif.welcome", Details = "PeopleHub demo", CreatedAt = DateTime.UtcNow });
        db.Add(new AuditLog { At = DateTime.UtcNow.AddDays(-1), User = HrEmail, Action = "Update", Entity = "Employee", EntityId = "PH-006", Details = "BasicSalary: 31500 -> 34000; reason: annual increase" });
        db.Add(new AuditLog { At = DateTime.UtcNow.AddDays(-2), User = "finance@peoplehub.local", Action = "FinanceApproved", Entity = "PayrollRun", EntityId = $"{prev1.Year}-{prev1.Month:00}", Details = "" });
        await db.SaveChangesAsync();

        DateTime NextWorkBack(DateTime d) { while (AttendanceCalculator.IsWeekend(d) || holidays.Contains(d.Date)) d = d.AddDays(-1); return d.Date; }
    }
}
