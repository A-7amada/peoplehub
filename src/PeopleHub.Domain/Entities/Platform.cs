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
    public string? IdempotencyKey { get; set; }
    public DateTime? ReceivedAt { get; set; }
}

public class EmailOutbox
{
    public int Id { get; set; }
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool Sent { get; set; }
    public int Attempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? Note { get; set; }
}

public class PasswordReset { public int Id { get; set; } public int UserId { get; set; } public string Token { get; set; } = ""; public DateTime Expires { get; set; } public bool Used { get; set; } }

public enum CandidateStage { Applied = 0, Screening = 1, Interview = 2, Offer = 3, Hired = 4, Rejected = 5 }
public class JobOpening
{
    public string Justification {get;set;}="";public string ApprovalStatus {get;set;}="Legacy";public int? RequestedByUserId {get;set;}public int? ApprovedByUserId {get;set;}public string ApprovalReason {get;set;}="";public DateTime? DecidedAt {get;set;}
    public int Id { get; set; }
    public string Title { get; set; } = ""; public string TitleAr { get; set; } = "";
    public int? DepartmentId { get; set; } public Department? Department { get; set; }
    public int Positions { get; set; } = 1;
    public bool IsOpen { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
public class Candidate
{
    public string InterviewStamp {get;set;}="";
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
public class TrainingCourse { public int Id { get; set; } public string Title { get; set; } = ""; public string TitleAr { get; set; } = ""; public int Hours { get; set; } public DateTime Date { get; set; }
 public string Need {get;set;}="";public decimal? PlannedBudget {get;set;}public string Currency {get;set;}="";
 public string ApprovalStatus {get;set;}="Legacy";public int? CreatedByUserId {get;set;}public int? DecidedByUserId {get;set;}public string DecisionReason {get;set;}="";public DateTime? DecidedAt {get;set;}
}
public class TrainingEnrollment { public int Id { get; set; } public int CourseId { get; set; } public TrainingCourse? Course { get; set; } public int EmployeeId { get; set; } public Employee? Employee { get; set; } public bool Completed { get; set; } }

/// <summary>End-of-service settlement: pro-rated salary, leave encashment and a company-policy gratuity (not a statutory amount).</summary>
public static class Settlement
{
    public record Result(decimal ProratedSalary, decimal LeaveEncashment, decimal Gratuity, decimal Total, decimal YearsOfService);
    public static Result Calculate(decimal basic, DateTime hire, DateTime lastDay, decimal unusedLeaveDays, decimal gratuityDaysPerYear)
    {
        if (lastDay.Date < hire.Date) throw new DomainException("err.dates");
        if (basic < 0 || unusedLeaveDays < 0 || gratuityDaysPerYear < 0) throw new DomainException("err.settlementValues");
        var daily = basic / 30m;
        var firstPaidDay = hire.Year == lastDay.Year && hire.Month == lastDay.Month ? Math.Min(hire.Day, 30) : 1;
        var worked = Math.Max(0, Math.Min(lastDay.Day, 30) - firstPaidDay + 1);
        var years = Math.Round((decimal)(lastDay.Date - hire.Date).TotalDays / 365.25m, 2);
        var pro = Math.Round(daily * worked, 2);
        var leave = Math.Round(daily * Math.Max(0, unusedLeaveDays), 2);
        var grat = Math.Round(daily * gratuityDaysPerYear * Math.Max(0, years), 2);
        return new(pro, leave, grat, pro + leave + grat, years);
    }
}

public enum CycleStatus { Open = 0, Calibration = 1, Closed = 2 }
public class PerfCycle { public int Id { get; set; } public string Name { get; set; } = ""; public DateTime Start { get; set; } public DateTime End { get; set; }
    public DateTime? CalibrationOn { get; set; } public DateTime? CloseOn { get; set; }
    public bool AutoPhases { get; set; } public bool Reminders { get; set; } = true;
    public int GoalWeight { get; set; } = 70; public int ManagerWeight { get; set; } = 30; public int FeedbackWeight { get; set; }
    public int? MinimumFeedbackReviewers { get; set; }
    public CycleStatus Status { get; set; } }
public class PerfGoal
{
    public int Id { get; set; } public int AppraisalId { get; set; }
    public string Title { get; set; } = ""; public string Target { get; set; } = "";
    public int Weight { get; set; } public int Progress { get; set; }
    public int? SelfScore { get; set; } public int? ManagerScore { get; set; }
    public bool IsCompetency { get; set; }
}
public class PerfAppraisal
{
    public string ResponseStamp {get;set;}="";
    public int Id { get; set; }
    public int CycleId { get; set; } public PerfCycle? Cycle { get; set; }
    public int EmployeeId { get; set; } public Employee? Employee { get; set; }
    public int? SelfScore { get; set; } public string SelfComment { get; set; } = "";
    public int? ManagerScore { get; set; } public string ManagerComment { get; set; } = "";
    public bool SelfDone { get; set; } public bool ManagerDone { get; set; }
    public int? LegacyReviewId { get; set; }
    public decimal? FinalScore { get; set; } public string CalibrationNote { get; set; } = "";
}
public class DevItem { public int? CourseId { get; set; } public int Id { get; set; } public int AppraisalId { get; set; } public string Action { get; set; } = ""; public DateTime? Due { get; set; } public bool Done { get; set; } }

/// <summary>Scoring rules: goals are weighted (weights should total 100), the manager's overall rating blends 70% goals and 30% overall judgement, and HR may calibrate the final score with a written reason.</summary>
public static class PerfCalc
{
    public static decimal? GoalScore(IEnumerable<PerfGoal> goals)
    {
        var g = goals.Where(x => x.ManagerScore != null && x.Weight > 0).ToList(); if (g.Count == 0) return null;
        return Math.Round(g.Sum(x => x.ManagerScore!.Value * (decimal)x.Weight) / g.Sum(x => (decimal)x.Weight), 2);
    }
    public static decimal? Computed(decimal? goalScore, int? overall) =>
        goalScore == null ? overall : overall == null ? goalScore : Math.Round(goalScore.Value * 0.7m + overall.Value * 0.3m, 2);
    public static string Band(decimal s) => s >= 4.5m ? "Outstanding" : s >= 3.5m ? "Exceeds" : s >= 2.5m ? "Meets" : "NeedsImprovement";
    /// <summary>Advisory only: a person decides on promotion, bonus or an improvement plan.</summary>
    public static string Recommendation(decimal s) => s >= 4.5m ? "PromotionReview+Bonus" : s >= 3.5m ? "Bonus" : s >= 2.5m ? "None" : "ImprovementPlan";
    public static bool WeightsOk(IEnumerable<PerfGoal> goals) => goals.Sum(g => g.Weight) == 100;
}

public class Feedback360
{
    public int Id { get; set; } public int AppraisalId { get; set; } public int ReviewerEmployeeId { get; set; }
    public Employee? Reviewer { get; set; } public int? Score { get; set; } public string Comment { get; set; } = "";
    public DateTime? SubmittedAt { get; set; }
}
public class CompetencyTemplate { public int Id { get; set; } public string Name { get; set; } = ""; }
public class CompetencyItem { public int Id { get; set; } public int TemplateId { get; set; } public string Title { get; set; } = ""; public string Behaviour { get; set; } = ""; public int Weight { get; set; } }
public class PerfReminder { public int Id { get; set; } public int CycleId { get; set; } public int EmployeeId { get; set; } public string Kind { get; set; } = ""; public DateTime Day { get; set; } }
public class PerfBonus
{
    public int Id { get; set; } public int AppraisalId { get; set; } public int EmployeeId { get; set; }
    public decimal Amount { get; set; } public string Reason { get; set; } = ""; public string ProposedBy { get; set; } = "";
    public int? PayrollLineId { get; set; } public string? AppliedBy { get; set; } public DateTime? AppliedAt { get; set; }
}
public static class PerfFormula
{
    public static bool Valid(int goals, int manager, int feedback) => goals >= 0 && manager >= 0 && feedback >= 0 && goals + manager + feedback == 100;
    // Missing components are not invented or silently reweighted.
    public static decimal? Calculate(decimal? goals, int? manager, decimal? feedback, int gw, int mw, int fw)
    {
        if (!Valid(gw, mw, fw) || (gw > 0 && goals == null) || (mw > 0 && manager == null) || (fw > 0 && feedback == null)) return null;
        return Math.Round(((goals ?? 0) * gw + (manager ?? 0) * mw + (feedback ?? 0) * fw) / 100m, 2);
    }
    public static CycleStatus Phase(DateTime today, DateTime calibration, DateTime close) => today >= close.Date ? CycleStatus.Closed : today >= calibration.Date ? CycleStatus.Calibration : CycleStatus.Open;
}
