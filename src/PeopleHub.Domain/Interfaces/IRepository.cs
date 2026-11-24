namespace PeopleHub.Domain.Interfaces;

/// <summary>Persistence port. Navigation properties are loaded automatically by the infrastructure model.</summary>
public interface IRepository
{
    IQueryable<T> Query<T>() where T : class;
    void Add<T>(T entity) where T : class;
    void AddRange<T>(IEnumerable<T> entities) where T : class;
    void Remove<T>(T entity) where T : class;
    Task<List<T>> ToListAsync<T>(IQueryable<T> q);
    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> q);
    Task<bool> AnyAsync<T>(IQueryable<T> q);
    Task<int> CountAsync<T>(IQueryable<T> q);
    Task SaveAsync();
}
