using PeopleHub.Domain.Entities;
using PeopleHub.Domain.Interfaces;
using PeopleHub.Domain.Security;

namespace PeopleHub.Application.Services;

public class AuthService(IRepository repo)
{
    public async Task<AppUser?> LoginAsync(string email, string password)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var u = await repo.FirstOrDefaultAsync(repo.Query<AppUser>().Where(x => x.Email == email && x.IsActive));
        return u != null && PasswordHasher.Verify(password ?? "", u.PasswordHash) ? u : null;
    }

    public Task<List<AppUser>> UsersAsync() => repo.ToListAsync(repo.Query<AppUser>().OrderBy(u => u.Email));
}
