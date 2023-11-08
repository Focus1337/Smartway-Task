using FileHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FileHub.Infrastructure.Repositories;

public class EfRepository<TEntity, TContext> : IRepository<TEntity> where TEntity : class
    where TContext : DbContext
{
    private readonly TContext _dbContext;

    protected EfRepository(TContext dbContext) =>
        _dbContext = dbContext;
    
    public Task<IQueryable<TEntity>> GetAllAsync() =>
        Task.FromResult(_dbContext.Set<TEntity>().AsQueryable());

    public Task CreateAsync(TEntity entity)
    {
        _dbContext.Set<TEntity>().Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TEntity entity)
    {
        _dbContext.Set<TEntity>().Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity)
    {
        _dbContext.Set<TEntity>().Remove(entity);
        return Task.CompletedTask;
    }
}