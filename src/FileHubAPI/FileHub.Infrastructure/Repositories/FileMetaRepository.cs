using FileHub.Core.Interfaces;
using FileHub.Core.Models;
using FileHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FileHub.Infrastructure.Repositories;

/// <summary>
/// <inheritdoc cref="IFileMetaRepository"/>
/// </summary>
public class FileMetaRepository : IFileMetaRepository
{
    private readonly AppDbContext _dbContext;

    public FileMetaRepository(AppDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<FileMeta?> GetFileMetaAsync(Guid userId, Guid groupId, Guid fileId) =>
        await _dbContext.FileMetas.Where(fm => fm.UserId == userId)
            .Where(fm => fm.GroupId == groupId)
            .FirstOrDefaultAsync(fm => fm.Id == fileId);

    public async Task<List<FileMeta>> GetListOfFilesAsync(Guid userId) =>
        await _dbContext.FileMetas.Where(fm => fm.UserId == userId).ToListAsync();

    public async Task CreateFileMetaAsync(FileMeta fileGroup)
    {
        await _dbContext.FileMetas.AddAsync(fileGroup);
        await _dbContext.SaveChangesAsync();
    }
}