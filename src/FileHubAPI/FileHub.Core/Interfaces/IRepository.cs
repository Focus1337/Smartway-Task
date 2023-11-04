namespace FileHub.Core.Interfaces;

public interface IRepository<TEntity> where TEntity : class
{
    Task<IQueryable<TEntity>> GetAllAsync();
    Task CreateAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
}