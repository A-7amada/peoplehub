using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Talent;

public record SkillView(EmployeeSkill Row, bool Restricted);
public record SkillGap(SkillRequirement Requirement, string Department, int Have, int Missing);

/// <summary>US-PH3-012 skills inventory and gap view. Information only: no automated employment decision. Levels 1-5. Illustrative scale, owner can change taxonomy.</summary>
public class SkillsInventory(IRepository repo, ICurrentUser me, AccessScope scope, AuditService audit, AppClock clock)
{
 public static readonly string[] Sources = ["Profile", "Assessment", "Training"];
 private bool Manager => me.Can(Perm.TalentManage);

 public Task<List<Skill>> SkillsAsync() => repo.ToListAsync(repo.Query<Skill>().OrderBy(x => x.Category).ThenBy(x => x.Name));

 public Task AddSkillAsync(string name, string? nameAr, string category) => repo.AtomicAsync(async () =>
 {
  if (!Manager) throw new DomainException("err.forbidden");
  name = (name ?? "").Trim(); category = (category ?? "").Trim();
  if (name.Length is < 2 or > 100 || category.Length is < 1 or > 100 || (nameAr?.Length ?? 0) > 100) throw new DomainException("err.required");
  if (await repo.AnyAsync(repo.Query<Skill>().Where(x => x.Name == name))) throw new DomainException("err.required");
  repo.Add(new Skill { Name = name, NameAr = (nameAr ?? "").Trim(), Category = category });
  audit.Log("Create", "Skill", name, "Skill added to the taxonomy");
  await repo.SaveAsync();
 });

 public Task SetActiveAsync(int skillId, bool active) => repo.AtomicAsync(async () =>
 {
  if (!Manager) throw new DomainException("err.forbidden");
  var s = await repo.FirstOrDefaultAsync(repo.Query<Skill>().Where(x => x.Id == skillId)) ?? throw new DomainException("err.notFound");
  s.Active = active; audit.Log("Update", "Skill", s.Name, active ? "Activated" : "Retired from new entries; existing records stay");
  await repo.SaveAsync();
 });

 public Task AddRequirementAsync(int skillId, int departmentId, int minLevel, int minPeople) => repo.AtomicAsync(async () =>
 {
  if (!Manager) throw new DomainException("err.forbidden");
  if (minLevel is < 1 or > 5 || minPeople is < 1 or > 1000) throw new DomainException("err.required");
  var s = await repo.FirstOrDefaultAsync(repo.Query<Skill>().Where(x => x.Id == skillId && x.Active)) ?? throw new DomainException("err.notFound");
  if (!await repo.AnyAsync(repo.Query<Department>().Where(x => x.Id == departmentId))) throw new DomainException("err.notFound");
  var existing = await repo.FirstOrDefaultAsync(repo.Query<SkillRequirement>().Where(x => x.SkillId == skillId && x.DepartmentId == departmentId));
  if (existing == null) { existing = new() { SkillId = skillId, DepartmentId = departmentId }; repo.Add(existing); }
  existing.MinLevel = minLevel; existing.MinPeople = minPeople;
  audit.Log("Set", "SkillRequirement", s.Name, $"department {departmentId}: at least {minPeople} people at level {minLevel}+ (information only)");
  await repo.SaveAsync();
 });

 public Task RemoveRequirementAsync(int id) => repo.AtomicAsync(async () =>
 {
  if (!Manager) throw new DomainException("err.forbidden");
  var r = await repo.FirstOrDefaultAsync(repo.Query<SkillRequirement>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
  repo.Remove(r); audit.Log("Delete", "SkillRequirement", id.ToString(), "Requirement removed");
  await repo.SaveAsync();
 });

 /// <summary>Own profile skills are proposed (unconfirmed). Assessment and Training entries need TalentManage and are confirmed on entry.</summary>
 public Task RecordAsync(int employeeId, int skillId, int level, string source, string? evidence) => repo.AtomicAsync(async () =>
 {
  if (!Sources.Contains(source) || level is < 1 or > 5 || (evidence?.Length ?? 0) > 1000) throw new DomainException("err.required");
  var self = me.EmployeeId == employeeId && source == "Profile";
  if (!self && !Manager) throw new DomainException("err.forbidden");
  if (!await repo.AnyAsync(repo.Query<Employee>().Where(x => x.Id == employeeId))) throw new DomainException("err.notFound");
  if (!await repo.AnyAsync(repo.Query<Skill>().Where(x => x.Id == skillId && x.Active))) throw new DomainException("err.notFound");
  var row = await repo.FirstOrDefaultAsync(repo.Query<EmployeeSkill>().Where(x => x.EmployeeId == employeeId && x.SkillId == skillId));
  if (row == null) { row = new() { EmployeeId = employeeId, SkillId = skillId }; repo.Add(row); }
  row.Level = level; row.Source = source; row.Evidence = (evidence ?? "").Trim(); row.RecordedBy = me.Email; row.At = clock.UtcNow;
  row.Confirmed = !self && Manager; row.ConfirmedBy = row.Confirmed ? me.Email : null;
  audit.Log("Record", "EmployeeSkill", employeeId.ToString(), $"skill {skillId}, level {level}, source {source}, {(row.Confirmed ? "confirmed" : "awaiting approval")}");
  await repo.SaveAsync();
 });

 /// <summary>Approver is TalentManage or the employee's manager within scope; never the employee themselves.</summary>
 public Task ConfirmAsync(int id) => repo.AtomicAsync(async () =>
 {
  var row = await repo.FirstOrDefaultAsync(repo.Query<EmployeeSkill>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
  var emp = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(x => x.Id == row.EmployeeId)) ?? throw new DomainException("err.notFound");
  var isManager = me.EmployeeId != null && emp.ManagerId == me.EmployeeId && await scope.CanSeeAsync(emp.Id);
  if (me.EmployeeId == row.EmployeeId || !(Manager || isManager)) throw new DomainException("err.forbidden");
  row.Confirmed = true; row.ConfirmedBy = me.Email;
  audit.Log("Confirm", "EmployeeSkill", row.EmployeeId.ToString(), $"skill {row.SkillId} level {row.Level} approved");
  await repo.SaveAsync();
 });

 /// <summary>Skills of one employee. Assessment level and evidence are hidden from everyone without TalentManage, including the employee, because they are assessment data.</summary>
 public async Task<List<SkillView>> ForEmployeeAsync(int employeeId)
 {
  if (me.EmployeeId != employeeId && !await scope.CanSeeAsync(employeeId)) throw new DomainException("err.forbidden");
  var rows = await repo.ToListAsync(repo.Query<EmployeeSkill>().Where(x => x.EmployeeId == employeeId));
  var skills = await SkillsAsync();
  return rows.Select(r =>
  {
   r.Skill = skills.FirstOrDefault(s => s.Id == r.SkillId);
   var restricted = r.Source == "Assessment" && !Manager;
   return new SkillView(restricted ? new EmployeeSkill { Id = r.Id, EmployeeId = r.EmployeeId, SkillId = r.SkillId, Skill = r.Skill, Source = r.Source, Confirmed = r.Confirmed, Level = 0, Evidence = "" } : r, restricted);
  }).ToList();
 }

 /// <summary>Unconfirmed profile skills of people the viewer may approve.</summary>
 public async Task<List<EmployeeSkill>> PendingAsync()
 {
  var rows = await repo.ToListAsync(repo.Query<EmployeeSkill>().Where(x => !x.Confirmed));
  var emps = await repo.ToListAsync(repo.Query<Employee>());var skills = await SkillsAsync();var result = new List<EmployeeSkill>();
  foreach (var r in rows)
  {
   var e = emps.FirstOrDefault(x => x.Id == r.EmployeeId); if (e == null || r.EmployeeId == me.EmployeeId) continue;
   if (Manager || (me.EmployeeId != null && e.ManagerId == me.EmployeeId && await scope.CanSeeAsync(e.Id))) { r.Skill = skills.FirstOrDefault(s => s.Id == r.SkillId); result.Add(r); }
  }
  return result;
 }

 public Task<List<SkillRequirement>> RequirementsAsync() => repo.ToListAsync(repo.Query<SkillRequirement>());

 /// <summary>Coverage across the employees the viewer can see (team scope or company scope). Counts confirmed skills only, including assessment-sourced ones as aggregates.</summary>
 public async Task<List<SkillGap>> GapsAsync()
 {
  if (!me.Can(Perm.ScopeTeam) && !me.Can(Perm.ScopeCompany) && !Manager) throw new DomainException("err.forbidden");
  var visible = await scope.VisibleEmployeeIdsAsync();
  var emps = (await repo.ToListAsync(repo.Query<Employee>())).Where(e => e.IsWorking).ToList();
  if (visible != null) emps = emps.Where(e => visible.Contains(e.Id)).ToList();
  var reqs = await repo.ToListAsync(repo.Query<SkillRequirement>());var skills = await SkillsAsync();var depts = await repo.ToListAsync(repo.Query<Department>());
  var confirmed = await repo.ToListAsync(repo.Query<EmployeeSkill>().Where(x => x.Confirmed));
  var gaps = new List<SkillGap>();
  foreach (var r in reqs)
  {
   var inDept = emps.Where(e => e.DepartmentId == r.DepartmentId).Select(e => e.Id).ToHashSet();
   if (visible != null && inDept.Count == 0) continue;
   var have = confirmed.Count(c => c.SkillId == r.SkillId && c.Level >= r.MinLevel && inDept.Contains(c.EmployeeId));
   r.Skill = skills.FirstOrDefault(s => s.Id == r.SkillId);
   gaps.Add(new SkillGap(r, depts.FirstOrDefault(d => d.Id == r.DepartmentId)?.Name ?? "", have, Math.Max(0, r.MinPeople - have)));
  }
  return gaps;
 }
}
