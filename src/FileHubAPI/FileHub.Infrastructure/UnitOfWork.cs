using FileHub.Core.Interfaces;
using FileHub.Infrastructure.Data;
using FileHub.Infrastructure.Repositories;

namespace FileHub.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private IFileMetaRepository? _fileMetaRepository;
    private IFileGroupRepository? _fileGroupRepository;

    private readonly AppDbContext _dbContext;

    public UnitOfWork(AppDbContext dbContext) =>
        _dbContext = dbContext;

    public IFileMetaRepository FileMetaRepository => _fileMetaRepository ??= new FileMetaRepository(_dbContext);
    public IFileGroupRepository FileGroupRepository => _fileGroupRepository ??= new FileGroupRepository(_dbContext);

    public async Task SaveChangesAsync() =>
        await _dbContext.SaveChangesAsync();
}