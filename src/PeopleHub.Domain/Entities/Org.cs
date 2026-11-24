namespace PeopleHub.Domain.Entities;

public class Branch
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string City { get; set; } = "";
}

/// <summary>A department, or a section when ParentId is set.</summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NameAr { get; set; } = "";
    public int? ParentId { get; set; }
    public int? ManagerId { get; set; }
    public string CostCenter { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int TargetHeadcount { get; set; }
}

public class JobTitle
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NameAr { get; set; } = "";
    public int Grade { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public string Description { get; set; } = "";
}
