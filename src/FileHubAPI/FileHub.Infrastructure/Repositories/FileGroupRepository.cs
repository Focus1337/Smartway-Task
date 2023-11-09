using FileHub.Core.Interfaces;
using FileHub.Core.Models;
using FileHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FileHub.Infrastructure.Repositories;

/// <summary>
/// <inheritdoc cref="IFileGroupRepository"/>
/// </summary>
public class FileGroupRepository : IFileGroupRepository
{
    private readonly AppDbContext _dbContext;

    public FileGroupRepository(AppDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<List<FileGroup>> GetListOfGroups(Guid userId) =>
        await _dbContext.FileGroups.Where(fg => fg.UserId == userId)
            .Include(fg => fg.FileMetas)
            .ToListAsync();

    public async Task<FileGroup?> GetFileGroupAsync(Guid userId, Guid groupId) =>
        await _dbContext.FileGroups.Where(fg => fg.UserId == userId)
            .Include(fg => fg.FileMetas)
            .FirstOrDefaultAsync(fm => fm.Id == groupId);

    public async Task CreateFileGroupAsync(FileGroup fileGroup)
    {
        await _dbContext.FileGroups.AddAsync(fileGroup);
        await _dbContext.SaveChangesAsync();
    }
}