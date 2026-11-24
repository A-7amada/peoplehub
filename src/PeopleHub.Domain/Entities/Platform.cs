namespace PeopleHub.Domain.Entities;

public class AppSetting { public int Id { get; set; } public string Key { get; set; } = ""; public string Value { get; set; } = ""; }

/// <summary>A raw punch read from the attendance device. DeviceUserId is the employee code enrolled on the machine.</summary>
public class DevicePunch
{
    public int Id { get; set; }
    public string DeviceUserId { get; set; } = "";
    public DateTime Time { get; set; }
    public int? EmployeeId { get; set; }
    public bool Matched { get; set; }
}

public class EmailOutbox
{
    public int Id { get; set; }
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool Sent { get; set; }
    public string? Note { get; set; }
}

public class PasswordReset { public int Id { get; set; } public int UserId { get; set; } public string Token { get; set; } = ""; public DateTime Expires { get; set; } public bool Used { get; set; } }

public enum CandidateStage { Applied = 0, Screening = 1, Interview = 2, Offer = 3, Hired = 4, Rejected = 5 }
public class JobOpening
{
    public int Id { get; set; }
    public string Title { get; set; } = ""; public string TitleAr { get; set; } = "";
    public int? DepartmentId { get; set; } public Department? Department { get; set; }
    public int Positions { get; set; } = 1;
    public bool IsOpen { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
public class Candidate
{
    public int Id { get; set; }
    public int JobOpeningId { get; set; } public JobOpening? Job { get; set; }
    public string Name { get; set; } = ""; public string Email { get; set; } = ""; public string Note { get; set; } = "";
    public CandidateStage Stage { get; set; }
    public DateTime CreatedAt { get; set; }
    public static CandidateStage? Next(CandidateStage s) => s is CandidateStage.Hired or CandidateStage.Rejected ? null : s + 1;
}
public class PerformanceReview
{
    public int Id { get; set; }
    public int EmployeeId { get; set; } public Employee? Employee { get; set; }
    public string Period { get; set; } = ""; public string Goals { get; set; } = ""; public string Comment { get; set; } = "";
    public int Score { get; set; }
    public string Reviewer { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public static bool ValidScore(int s) => s is >= 1 and <= 5;
}
public class TrainingCourse { public int Id { get; set; } public string Title { get; set; } = ""; public string TitleAr { get; set; } = ""; public int Hours { get; set; } public DateTime Date { get; set; } }
public class TrainingEnrollment { public int Id { get; set; } public int CourseId { get; set; } public TrainingCourse? Course { get; set; } public int EmployeeId { get; set; } public Employee? Employee { get; set; } public bool Completed { get; set; } }

/// <summary>End-of-service settlement: pro-rated salary, leave encashment and a company-policy gratuity (not a statutory amount).</summary>
public static class Settlement
{
    public record Result(decimal ProratedSalary, decimal LeaveEncashment, decimal Gratuity, decimal Total, decimal YearsOfService);
    public static Result Calculate(decimal basic, DateTime hire, DateTime lastDay, decimal unusedLeaveDays, decimal gratuityDaysPerYear)
    {
        var daily = basic / 30m;
        var worked = Math.Min(lastDay.Day, 30);
        var years = Math.Round((decimal)(lastDay.Date - hire.Date).TotalDays / 365.25m, 2);
        var pro = Math.Round(daily * worked, 2);
        var leave = Math.Round(daily * Math.Max(0, unusedLeaveDays), 2);
        var grat = Math.Round(daily * gratuityDaysPerYear * Math.Max(0, years), 2);
        return new(pro, leave, grat, pro + leave + grat, years);
    }
}
