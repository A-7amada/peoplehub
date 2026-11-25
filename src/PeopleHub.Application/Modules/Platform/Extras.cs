using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;

using PeopleHub.Application.Common;
namespace PeopleHub.Application.Modules.Platform;

public class AttachmentService(IRepository repo, AccessScope scope, ICurrentUser me, AppClock clock)
{
    public const int MaxBytes = 2 * 1024 * 1024;
    private static readonly Dictionary<string, string> Types = new() { [".pdf"] = "application/pdf", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png" };

    /// <summary>Stores the file (when one was sent) and returns its token and display name.</summary>
    public (string? Token, string? Name) Save(string? fileName, byte[]? data)
    {
        if (data == null || data.Length == 0 || string.IsNullOrWhiteSpace(fileName)) return (null, null);
        var (name,ct)=Validate(fileName,data);
        var a = new Attachment { FileName = name, ContentType = ct, Data = data, OwnerEmployeeId = me.EmployeeId ?? 0, UploadedBy = me.Email, At = clock.UtcNow };
        repo.Add(a);
        return (a.Token, name);
    }

    /// <summary>Checks supported format and signature only, not malware or authenticity.</summary>
    public static (string Name,string ContentType) Validate(string fileName,byte[] data)
    {
        if(string.IsNullOrWhiteSpace(fileName)||data.Length==0)throw new DomainException("err.required");
        var name = Path.GetFileName(fileName);
        var ext = Path.GetExtension(name).ToLowerInvariant();
        if (!Types.TryGetValue(ext, out var ct)) throw new DomainException("err.fileType");
        if (data.Length > MaxBytes) throw new DomainException("err.fileSize");
        bool signature=ext switch {".pdf"=>data.AsSpan().StartsWith("%PDF-"u8),".png"=>data.AsSpan().StartsWith(new byte[]{137,80,78,71,13,10,26,10}),".jpg" or ".jpeg"=>data.Length>=3 && data[0]==255 && data[1]==216 && data[2]==255,_=>false};
        if(!signature)throw new DomainException("err.fileType");
        if(name.Length>255)throw new DomainException("err.required");
        return (name,ct);
    }

    public async Task<Attachment> GetAsync(string token)
    {
        var a = await repo.FirstOrDefaultAsync(repo.Query<Attachment>().Where(x => x.Token == token)) ?? throw new DomainException("err.notFound");
        var leave=await repo.FirstOrDefaultAsync(repo.Query<LeaveRequest>().Where(x=>x.AttachmentToken==token));
        if(leave!=null&&leave.EmployeeId!=me.EmployeeId&&!me.Can(Perm.LeaveEvidence))throw new DomainException("err.forbidden");
        var certificate=await repo.FirstOrDefaultAsync(repo.Query<TrainingCertificate>().Where(x=>x.AttachmentToken==token));
        if(certificate!=null&&certificate.EmployeeId!=me.EmployeeId&&!me.Can(Perm.TalentManage))throw new DomainException("err.forbidden");
        var document=await repo.FirstOrDefaultAsync(repo.Query<EmployeeDocument>().Where(x=>x.AttachmentToken==token));
        if(document!=null && document.EmployeeId!=me.EmployeeId && !me.Can(Perm.EmpManage))throw new DomainException("err.forbidden");
        if(certificate!=null)return a;
        if (a.OwnerEmployeeId != me.EmployeeId && !await scope.CanSeeAsync(a.OwnerEmployeeId)) throw new DomainException("err.forbidden");
        return a;
    }
}

public class DelegationService(IRepository repo, ICurrentUser me, AuditService audit, AppClock clock)
{
    public async Task<List<(Delegation D, Employee From, Employee To)>> MineAsync()
    {
        var id = me.EmployeeId ?? -1;
        var list = await repo.ToListAsync(repo.Query<Delegation>().Where(d => !d.Revoked && (d.FromEmployeeId == id || d.ToEmployeeId == id)).OrderByDescending(d => d.StartDate));
        var emps = await repo.ToListAsync(repo.Query<Employee>());
        Employee NameOnly(int employeeId){var e=emps.First(x=>x.Id==employeeId);return new Employee{Id=e.Id,Code=e.Code,FullName=e.FullName,FullNameAr=e.FullNameAr};}
        return list.Select(d => (new Delegation{Id=d.Id,FromEmployeeId=d.FromEmployeeId,ToEmployeeId=d.ToEmployeeId,StartDate=d.StartDate,EndDate=d.EndDate,Revoked=d.Revoked},NameOnly(d.FromEmployeeId),NameOnly(d.ToEmployeeId))).ToList();
    }

    public async Task AddAsync(int toEmployeeId, DateTime start, DateTime end)
    {
        var from = me.EmployeeId ?? throw new DomainException("err.forbidden");
        if (toEmployeeId == from) throw new DomainException("err.delegateSelf");
        if (end < start || end.Date < clock.Today) throw new DomainException("err.dates");
        var to = await repo.FirstOrDefaultAsync(repo.Query<Employee>().Where(e => e.Id == toEmployeeId)) ?? throw new DomainException("err.notFound");
        if (!to.IsWorking) throw new DomainException("err.terminated");
        repo.Add(new Delegation { FromEmployeeId = from, ToEmployeeId = toEmployeeId, StartDate = start.Date, EndDate = end.Date });
        audit.Log("Create", "Delegation", from, $"{me.Email} -> employee {toEmployeeId}, {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");
        await repo.SaveAsync();
    }

    public async Task RevokeAsync(int id)
    {
        var d = await repo.FirstOrDefaultAsync(repo.Query<Delegation>().Where(x => x.Id == id)) ?? throw new DomainException("err.notFound");
        if (d.FromEmployeeId != me.EmployeeId) throw new DomainException("err.forbidden");
        d.Revoked = true;
        audit.Log("Update", "Delegation", id, "revoked");
        await repo.SaveAsync();
    }

    /// <summary>Delegations that currently give the signed-in user someone else's approval rights, with the delegator's roles.</summary>
    public async Task<List<(int FromEmployeeId, string[] Roles)>> ActiveToMeAsync()
    {
        if (me.EmployeeId is not { } id) return [];
        var today = clock.Today;
        var ds = (await repo.ToListAsync(repo.Query<Delegation>().Where(d => d.ToEmployeeId == id && !d.Revoked))).Where(d => d.IsActiveOn(today)).ToList();
        if (ds.Count == 0) return [];
        var users = await repo.ToListAsync(repo.Query<AppUser>().Where(u => u.IsActive));
        return ds.Select(d => (d.FromEmployeeId, users.Where(u => u.EmployeeId == d.FromEmployeeId).SelectMany(u => u.RoleArray).Distinct().ToArray())).ToList();
    }
}
