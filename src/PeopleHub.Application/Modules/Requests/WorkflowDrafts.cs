using System.Text.Json;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Requests;

public record DraftStep(ApproverType Approver, string? RoleCode, decimal? Threshold, ApprovalRule Rule, int? RequiredCount);
public record DraftView(WorkflowDraft Draft, List<WorkflowStep> Steps, List<WorkflowService.Change> Changes);

/// <summary>US-PH3-008 draft and publish for approval workflow edits. Edit a draft, compare it with the live cycle, then publish or discard. Publishing replaces the live steps in one transaction and is audited. Requests already routed keep the route they captured. Default (illustrative, owner can change): one person with workflow permission can draft and publish; a second-person publish rule is not built.</summary>
public class WorkflowDrafts(IRepository repo, ICurrentUser me, AuditService audit, AppClock clock, WorkflowService flows)
{
 private void Need(){if(!me.Can(Perm.AdmFlows))throw new DomainException("err.forbidden");}
 static List<WorkflowStep> ToSteps(string json){var l=new List<WorkflowStep>();var i=0;foreach(var d in JsonSerializer.Deserialize<List<DraftStep>>(json)??[])l.Add(new WorkflowStep{Order=++i,Approver=d.Approver,RoleCode=d.RoleCode,Threshold=d.Threshold,Rule=d.Rule,RequiredCount=d.RequiredCount});return l;}
 static string ToJson(IEnumerable<WorkflowStep> steps)=>JsonSerializer.Serialize(steps.OrderBy(x=>x.Order).Select(x=>new DraftStep(x.Approver,x.RoleCode,x.Threshold,x.Rule,x.RequiredCount)));
 static string Def(string kind,IEnumerable<WorkflowStep> steps)=>JsonSerializer.Serialize(new{Kind=kind,Steps=steps.OrderBy(x=>x.Order).Select(x=>new{x.Order,x.Approver,x.RoleCode,x.Threshold,x.Rule,x.RequiredCount})});

 public async Task<List<DraftView>> ListAsync()
 {
  Need();
  var live=await flows.ListAsync();var res=new List<DraftView>();
  foreach(var d in await repo.ToListAsync(repo.Query<WorkflowDraft>().OrderBy(x=>x.Kind)))
  {
   var steps=ToSteps(d.StepsJson);var liveSteps=live.FirstOrDefault(x=>x.Kind==d.Kind)?.Steps??[];
   res.Add(new(d,steps,WorkflowService.Compare(Def(d.Kind,liveSteps),Def(d.Kind,steps))));
  }
  return res;
 }

 private async Task<WorkflowDraft> DraftAsync(string kind)=>await repo.FirstOrDefaultAsync(repo.Query<WorkflowDraft>().Where(x=>x.Kind==kind))??throw new DomainException("err.notFound");
 private void Touch(WorkflowDraft d,List<WorkflowStep> steps){d.StepsJson=ToJson(steps);d.UpdatedBy=me.Email;d.UpdatedAt=clock.UtcNow;}

 public Task StartAsync(string kind)=>repo.AtomicAsync(async()=>
 {
  Need();if(!WorkflowService.Kinds.Contains(kind))throw new DomainException("err.required");
  if(await repo.AnyAsync(repo.Query<WorkflowDraft>().Where(x=>x.Kind==kind)))throw new DomainException("err.required");
  var live=(await flows.ListAsync()).FirstOrDefault(x=>x.Kind==kind)??throw new DomainException("err.notFound");
  repo.Add(new WorkflowDraft{Kind=kind,StepsJson=ToJson(live.Steps),CreatedBy=me.Email,CreatedAt=clock.UtcNow,UpdatedBy=me.Email,UpdatedAt=clock.UtcNow});
  audit.Log("Create","WorkflowDraft",kind,"Draft started from the live cycle");
  await repo.SaveAsync();
 });

 public Task AddStepAsync(string kind,ApproverType type,string[]? roles,decimal? threshold,ApprovalRule rule=ApprovalRule.AnyRole,int? requiredCount=null)=>repo.AtomicAsync(async()=>
 {
  Need();var d=await DraftAsync(kind);var steps=ToSteps(d.StepsJson);
  var known=(await repo.ToListAsync(repo.Query<Role>())).Select(r=>r.Code).ToHashSet();
  steps.Add(WorkflowService.BuildStep(steps,known,type,roles,threshold,rule,requiredCount));
  Touch(d,steps);audit.Log("Update","WorkflowDraft",kind,"Draft: step added");await repo.SaveAsync();
 });

 public Task RemoveStepAsync(string kind,int position)=>repo.AtomicAsync(async()=>
 {
  Need();var d=await DraftAsync(kind);var steps=ToSteps(d.StepsJson);
  if(steps.Count<=1)throw new DomainException("err.minSteps");
  if(position<1||position>steps.Count)throw new DomainException("err.notFound");
  steps.RemoveAt(position-1);Touch(d,steps);audit.Log("Update","WorkflowDraft",kind,"Draft: step removed");await repo.SaveAsync();
 });

 public Task MoveStepAsync(string kind,int position,int dir)=>repo.AtomicAsync(async()=>
 {
  Need();var d=await DraftAsync(kind);var steps=ToSteps(d.StepsJson);
  var i=position-1;var j=i+(dir<0?-1:1);
  if(i<0||i>=steps.Count)throw new DomainException("err.notFound");
  if(j<0||j>=steps.Count)return;
  (steps[i],steps[j])=(steps[j],steps[i]);Touch(d,steps);audit.Log("Update","WorkflowDraft",kind,"Draft: steps reordered");await repo.SaveAsync();
 });

 public Task DiscardAsync(string kind)=>repo.AtomicAsync(async()=>
 {
  Need();var d=await DraftAsync(kind);repo.Remove(d);audit.Log("Delete","WorkflowDraft",kind,"Draft discarded; live cycle unchanged");await repo.SaveAsync();
 });

 /// <summary>Replaces the live steps with the draft, then removes the draft. Needs at least one difference and at least one step.</summary>
 public Task PublishAsync(string kind)=>repo.AtomicAsync(async()=>
 {
  Need();var d=await DraftAsync(kind);var steps=ToSteps(d.StepsJson);
  if(steps.Count==0)throw new DomainException("err.minSteps");
  var live=(await flows.ListAsync()).FirstOrDefault(x=>x.Kind==kind)??throw new DomainException("err.notFound");
  var changes=WorkflowService.Compare(Def(kind,live.Steps),Def(kind,steps));
  if(changes.Count==0)throw new DomainException("err.required");
  foreach(var old in await repo.ToListAsync(repo.Query<WorkflowStep>().Where(x=>x.WorkflowId==live.Id)))repo.Remove(old);
  foreach(var s in steps){s.WorkflowId=live.Id;repo.Add(s);}
  repo.Remove(d);
  audit.Log("Publish","Workflow",live.Id,$"{kind}: draft by {d.CreatedBy} published by {me.Email}; {changes.Count} change(s). Requests already routed keep their captured route");
  await repo.SaveAsync();
 });
}
