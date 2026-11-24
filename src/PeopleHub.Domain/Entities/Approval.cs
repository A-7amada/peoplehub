namespace PeopleHub.Domain.Entities;

public enum RequestStatus { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }
/// <summary>Route tokens: "Manager" means the employee's direct manager, anything else is a role code.</summary>
public static class ApprovalStage
{
    public const string Manager = "Manager", HR = Roles.HRManager + "|" + Roles.HRSpecialist, Finance = Roles.FinanceManager;
}

/// <summary>Base for every request that goes through a multi-level approval route.</summary>
public abstract class Approvable
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Route { get; set; } = "";
    public string? WorkflowRevisionHash {get;set;}
    public int StageIndex { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Comment { get; set; }
    public string? AttachmentToken { get; set; }
    public string? AttachmentName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }

    public string[] Stages => Route.Split(',', StringSplitOptions.RemoveEmptyEntries);
    public string? CurrentStage => Status == RequestStatus.Pending && StageIndex < Stages.Length ? Stages[StageIndex] : null;

    /// <summary>Manager stage is skipped for employees without a direct manager; HR is the fallback.</summary>
    public static string BuildRoute(Employee e, params string[] after)
    {
        var s = new List<string>();
        if (e.ManagerId != null) s.Add(ApprovalStage.Manager);
        foreach (var a in after) if (!s.Contains(a)) s.Add(a);
        if (s.Count == 0) s.Add(ApprovalStage.HR);
        return string.Join(',', s);
    }

    /// <summary>Approves the current stage. Returns true when the whole route is complete.</summary>
    public bool ApproveStage(DateTime now)
    {
        if (Status != RequestStatus.Pending) throw new DomainException("err.notPending");
        StageIndex++;
        if (StageIndex < Stages.Length) return false;
        Status = RequestStatus.Approved;
        DecidedAt = now;
        return true;
    }

    public void RejectRequest(string? comment, DateTime now)
    {
        if (Status != RequestStatus.Pending) throw new DomainException("err.notPending");
        if (string.IsNullOrWhiteSpace(comment)) throw new DomainException("err.reasonRequired");
        Status = RequestStatus.Rejected;
        Comment = comment.Trim();
        DecidedAt = now;
    }

    public virtual void CancelRequest(DateTime now)
    {
        if (Status != RequestStatus.Pending) throw new DomainException("err.notPending");
        Status = RequestStatus.Cancelled;
        DecidedAt = now;
    }
}

public class ApprovalAction
{
    public int Id { get; set; }
    public string Kind { get; set; } = "";
    public int RequestId { get; set; }
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Comment { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}
