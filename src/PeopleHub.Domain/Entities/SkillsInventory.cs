namespace PeopleHub.Domain.Entities;

/// <summary>US-PH3-012 configurable skill taxonomy entry. Fictional or tenant-defined data only.</summary>
public class Skill
{
 public int Id { get; set; }
 public string Name { get; set; } = "";
 public string NameAr { get; set; } = "";
 public string Category { get; set; } = "";
 public bool Active { get; set; } = true;
}

/// <summary>An employee skill with its source. Self-declared profile skills stay unconfirmed until approved. Assessment-sourced level and evidence are restricted.</summary>
public class EmployeeSkill
{
 public int Id { get; set; }
 public int EmployeeId { get; set; }
 public int SkillId { get; set; }
 public Skill? Skill { get; set; }
 public int Level { get; set; }
 /// <summary>Profile, Assessment or Training.</summary>
 public string Source { get; set; } = "Profile";
 public string Evidence { get; set; } = "";
 public bool Confirmed { get; set; }
 public string RecordedBy { get; set; } = "";
 public string? ConfirmedBy { get; set; }
 public DateTime At { get; set; }
}

/// <summary>Expected capability for a department: at least MinPeople confirmed at MinLevel or above. Used for information only, never for employment decisions.</summary>
public class SkillRequirement
{
 public int Id { get; set; }
 public int SkillId { get; set; }
 public Skill? Skill { get; set; }
 public int DepartmentId { get; set; }
 public int MinLevel { get; set; }
 public int MinPeople { get; set; }
}
