using System.Globalization;
using System.Text;
using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
namespace PeopleHub.Application.Services;
public record EmployeeImportRow(int Row,string Code,string Name,string Email,List<string> Errors);
public record EmployeeImportPreview(List<EmployeeImportRow> Rows,int ValidRows);
/// <summary>Bounded dry-run only. No persistence, employee mutation or import authorization.</summary>
public class EmployeeImportValidation(IRepository repo,ICurrentUser me)
{
 public const int MaxBytes=262144;
 public const string Header="Code,FullName,FullNameAr,Email,HireDate,DepartmentId,BranchId,JobTitleId";
 public async Task<EmployeeImportPreview> PreviewAsync(byte[] data)
 {
  if(!me.Can(Perm.EmpManage))throw new DomainException("err.forbidden");
  if(data.Length==0||data.Length>MaxBytes)throw new DomainException("err.required");
  string text;try{text=new UTF8Encoding(false,true).GetString(data).TrimStart('\uFEFF');}catch(DecoderFallbackException){throw new DomainException("err.required");}
  var rows=Parse(text);if(rows.Count<2||rows.Count>201||!rows[0].SequenceEqual(Header.Split(',')))throw new DomainException("err.required");
  var existing=await repo.ToListAsync(repo.Query<Employee>().Select(x=>new{x.Code,x.Email}));
  var codes=existing.Select(x=>x.Code.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);var emails=existing.Select(x=>x.Email.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
  var departments=(await repo.ToListAsync(repo.Query<Department>().Select(x=>x.Id))).ToHashSet();var branches=(await repo.ToListAsync(repo.Query<Branch>().Select(x=>x.Id))).ToHashSet();var titles=(await repo.ToListAsync(repo.Query<JobTitle>().Select(x=>x.Id))).ToHashSet();
  var normalized=rows.Skip(1).Select(r=>r.Select(x=>x.Trim()).ToArray()).ToList();var duplicateCodes=normalized.Where(r=>r.Length==8&&r[0]!="").GroupBy(r=>r[0],StringComparer.OrdinalIgnoreCase).Where(g=>g.Count()>1).Select(g=>g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);var duplicateEmails=normalized.Where(r=>r.Length==8&&r[3]!="").GroupBy(r=>r[3],StringComparer.OrdinalIgnoreCase).Where(g=>g.Count()>1).Select(g=>g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
  var result=new List<EmployeeImportRow>();
  for(int i=0;i<normalized.Count;i++)
  {
   var r=normalized[i];var errors=new List<string>();if(r.Length!=8){result.Add(new(i+2,"","","",["columns"]));continue;}
   if(r.Any(x=>x.Any(char.IsControl)))errors.Add("controls");
   if(r[0].Length is <1 or >50)errors.Add("code");if(r[1].Length is <1 or >200||r[2].Length>200)errors.Add("name");
   if(r[3].Length is <3 or >254||!System.Net.Mail.MailAddress.TryCreate(r[3],out var address)||address.Address!=r[3])errors.Add("email");
   if(!DateTime.TryParseExact(r[4],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var hire)||hire.Year<1900||hire.Year>9998)errors.Add("hireDate");
   if(!int.TryParse(r[5],NumberStyles.None,CultureInfo.InvariantCulture,out var department)||!departments.Contains(department))errors.Add("department");
   if(!int.TryParse(r[6],NumberStyles.None,CultureInfo.InvariantCulture,out var branch)||!branches.Contains(branch))errors.Add("branch");
   if(!int.TryParse(r[7],NumberStyles.None,CultureInfo.InvariantCulture,out var title)||!titles.Contains(title))errors.Add("jobTitle");
   if(codes.Contains(r[0]))errors.Add("existingCode");if(emails.Contains(r[3]))errors.Add("existingEmail");if(duplicateCodes.Contains(r[0]))errors.Add("duplicateCode");if(duplicateEmails.Contains(r[3]))errors.Add("duplicateEmail");
   result.Add(new(i+2,r[0],r[1],r[3],errors));
  }
  return new(result,result.Count(x=>x.Errors.Count==0));
 }
 private static List<string[]> Parse(string text)
 {
  var rows=new List<string[]>();var fields=new List<string>();var cell=new StringBuilder();bool quoted=false,closed=false;
  void Field(){fields.Add(cell.ToString());cell.Clear();closed=false;if(fields.Count>8)throw new DomainException("err.required");}
  void Row(){Field();rows.Add(fields.ToArray());fields.Clear();if(rows.Count>201)throw new DomainException("err.required");}
  for(int i=0;i<text.Length;i++)
  {
   char c=text[i];if(quoted){if(c=='"'){if(i+1<text.Length&&text[i+1]=='"'){cell.Append('"');i++;}else{quoted=false;closed=true;}}else cell.Append(c);continue;}
   if(c==','){Field();continue;}if(c=='\r'||c=='\n'){if(c=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++;Row();continue;}
   if(closed)throw new DomainException("err.required");if(c=='"'){if(cell.Length!=0)throw new DomainException("err.required");quoted=true;}else cell.Append(c);
  }
  if(quoted)throw new DomainException("err.required");if(cell.Length>0||fields.Count>0||closed)Row();return rows;
 }
}
