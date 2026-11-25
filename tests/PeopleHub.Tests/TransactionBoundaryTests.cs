using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Web.Controllers;
using PeopleHub.Web.Services;
namespace PeopleHub.Tests;
/// <summary>ARC-003 and SEC-010 guards. They do not prove PostgreSQL behaviour; they stop regressions in structure.</summary>
public class TransactionBoundaryTests
{
 static string ServicesDir()
 {
  var d=new DirectoryInfo(AppContext.BaseDirectory);
  while(d!=null&&!Directory.Exists(Path.Combine(d.FullName,"src","PeopleHub.Application","Modules")))d=d.Parent;
  Assert.NotNull(d);return Path.Combine(d!.FullName,"src","PeopleHub.Application","Modules");
 }
 static readonly Regex Decl=new(@"^\s*(public|internal|private|protected)\s[^=;]*?\b(\w+)\s*(<[^>]*>)?\s*\(",RegexOptions.Compiled);
 /// <summary>Any service method that saves more than once must run inside AtomicAsync, directly or because it is called from one in the same file.</summary>
 [Fact]public void MultiSaveServiceMethodsAreAtomic()
 {
  var offenders=new List<string>();var checkedMethods=0;
  foreach(var f in Directory.GetFiles(ServicesDir(),"*.cs",SearchOption.AllDirectories))
  {
   var lines=File.ReadAllLines(f);var text=string.Join("\n",lines);
   var starts=new List<(int line,string name)>();
   for(int i=0;i<lines.Length;i++){var m=Decl.Match(lines[i]);if(m.Success&&!lines[i].Contains(" class ")&&(lines[i].Contains("Task")||lines[i].Contains("async")))starts.Add((i,m.Groups[2].Value));}
   for(int s=0;s<starts.Count;s++)
   {
    var end=s+1<starts.Count?starts[s+1].line:lines.Length;
    var body=string.Join("\n",lines[starts[s].line..end]);
    var name=starts[s].name;
    var firstBrace=body.IndexOf('{');var afterDecl=firstBrace>=0?body[firstBrace..]:body;
    var saves=Regex.Matches(afterDecl,@"\.SaveAsync\(\)").Count;
    if(saves<2)continue;checkedMethods++;
    var inline=body.Contains("AtomicAsync");
    var wrapped=Regex.IsMatch(text,@"AtomicAsync\([^;]*\b"+Regex.Escape(name)+@"\b");
    // Separate branches that each save once and then return are not a multi-write transaction.
    var exclusive=name is "LoginAsync";
    if(!inline&&!wrapped&&!exclusive)offenders.Add(Path.GetFileName(f)+":"+name);
   }
  }
  Assert.True(checkedMethods>=8,"scanner found too few multi-save methods; the guard is not scanning");
  Assert.Empty(offenders);
 }
 static readonly (Type type,string action)[] Sensitive=
 {
  (typeof(EmployeesController),"Status"),(typeof(EmployeesController),"SetComponent"),(typeof(EmployeesController),"AdjustBalance"),(typeof(AttendanceController),"Lock"),(typeof(ImportValidationController),"Commit"),(typeof(ImportValidationController),"Rollback")
 };
 [Fact]public void SensitiveEmployeeAndPeriodActionsRequireStepUp()
 {
  foreach(var (type,action) in Sensitive)
  {
   var ms=type.GetMethods(BindingFlags.Public|BindingFlags.Instance).Where(m=>m.Name==action&&m.GetCustomAttributes<HttpPostAttribute>().Any()).ToList();
   Assert.NotEmpty(ms);
   foreach(var m in ms)Assert.True(m.GetCustomAttributes<RequireStepUpAttribute>().Any(),type.Name+"."+action+" lacks RequireStepUp");
  }
 }
}
