using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Security;

namespace PeopleHub.Infrastructure.Persistence;

public static class SeedData
{
    public const string HrEmail = "hr@peoplehub.local";
    public const string EmployeeEmail = "employee@peoplehub.local";

    /// <summary>The HR password comes from configuration outside Development; without it the HR account stays locked.</summary>
    public static async Task InitializeAsync(AppDbContext db, string? hrPassword, string employeePassword)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.DepartmentSet.AnyAsync()) return;

        var eng = new Department { Name = "Engineering", NameAr = "الهندسة" };
        var hr = new Department { Name = "Human Resources", NameAr = "الموارد البشرية" };
        var fin = new Department { Name = "Finance", NameAr = "المالية" };
        var sales = new Department { Name = "Sales", NameAr = "المبيعات" };
        db.DepartmentSet.AddRange(eng, hr, fin, sales);

        Employee E(string n, string ar, string mail, string title, Department d, int years, decimal sal, int bal = 21) => new()
        {
            FullName = n, FullNameAr = ar, Email = mail, JobTitle = title, Department = d,
            HireDate = DateTime.UtcNow.Date.AddYears(-years).AddDays(-30), MonthlySalary = sal, AnnualLeaveBalance = bal
        };

        var sara = E("Sara Mahmoud", "سارة محمود", "sara@peoplehub.local", "HR Manager", hr, 6, 42000);
        var omar = E("Omar Khaled", "عمر خالد", employeeEmailLocal, "Senior Software Engineer", eng, 4, 55000, 18);
        var nour = E("Nour Hassan", "نور حسن", "nour@peoplehub.local", "Frontend Engineer", eng, 2, 36000);
        var youssef = E("Youssef Adel", "يوسف عادل", "youssef@peoplehub.local", "Accountant", fin, 3, 30000);
        var mona = E("Mona Fathy", "منى فتحي", "mona@peoplehub.local", "Sales Lead", sales, 5, 38000);
        var karim = E("Karim Samir", "كريم سمير", "karim@peoplehub.local", "QA Engineer", eng, 1, 26000);
        karim.Status = EmployeeStatus.OnLeave;
        db.EmployeeSet.AddRange(sara, omar, nour, youssef, mona, karim);
        await db.SaveChangesAsync();

        db.UserSet.Add(new AppUser
        {
            Email = HrEmail, Role = Roles.HR, EmployeeId = sara.Id,
            PasswordHash = PasswordHasher.Hash(string.IsNullOrEmpty(hrPassword) ? Guid.NewGuid().ToString("N") : hrPassword),
            IsActive = !string.IsNullOrEmpty(hrPassword)
        });
        db.UserSet.Add(new AppUser { Email = EmployeeEmail, Role = Roles.Employee, EmployeeId = omar.Id, PasswordHash = PasswordHasher.Hash(employeePassword) });

        var next = NextWorkday(DateTime.UtcNow.Date.AddDays(7));
        db.LeaveSet.Add(new LeaveRequest { EmployeeId = nour.Id, Type = LeaveType.Annual, StartDate = next, EndDate = next.AddDays(3), Days = LeaveRequest.CountWorkingDays(next, next.AddDays(3)), Reason = "Family trip" });
        db.LeaveSet.Add(new LeaveRequest { EmployeeId = youssef.Id, Type = LeaveType.Sick, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date, Days = LeaveRequest.CountWorkingDays(DateTime.UtcNow.Date, DateTime.UtcNow.Date), Reason = "Doctor appointment" });
        await db.SaveChangesAsync();
    }

    private const string employeeEmailLocal = EmployeeEmail;

    private static DateTime NextWorkday(DateTime d)
    {
        while (d.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday) d = d.AddDays(1);
        return d;
    }
}
