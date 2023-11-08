using FileHub.Core.Errors;
using FileHub.Core.Interfaces;
using FileHub.Core.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace FileHub.Core.Services;

public class FileService : IFileService
{
    private readonly IUnitOfWork _uow;

    public FileService(IUnitOfWork uow) =>
        _uow = uow;

    public async Task<Result<FileMeta>> GetFileMetaAsync(Guid userId, Guid groupId, Guid fileId)
    {
        var queryable = await _uow.FileMetaRepository.GetAllAsync();
        var result = await queryable.Where(fm => fm.UserId == userId)
            .Where(fm => fm.GroupId == groupId)
            .FirstOrDefaultAsync(fm => fm.Id == fileId);
        return result is null ? Result.Fail<FileMeta>(new FileNotFoundError()) : Result.Ok(result);
    }

    public async Task<Result<List<FileMeta>>> GetFileGroupAsync(Guid userId, Guid groupId)
    {
        var queryable = await _uow.FileGroupRepository.GetAllAsync();
        var result = await queryable.Where(fg => fg.UserId == userId)
            .Include(fg => fg.FileMetas)
            .FirstOrDefaultAsync(fm => fm.Id == groupId);
        return result is null ? Result.Fail<List<FileMeta>>(new GroupNotFoundError()) : Result.Ok(result.FileMetas);
    }

    public async Task<Result<List<FileMeta>>> GetListOfFiles(Guid ownerId)
    {
        var queryable = await _uow.FileMetaRepository.GetAllAsync();
        return Result.Ok(await queryable.Where(fm => fm.UserId == ownerId).ToListAsync());
    }

    public async Task<Result<List<FileGroup>>> GetListOfGroups(Guid ownerId)
    {
        var queryable = await _uow.FileGroupRepository.GetAllAsync();
        return Result.Ok(await queryable.Where(fg => fg.UserId == ownerId)
            .Include(fg => fg.FileMetas)
            .ToListAsync());
    }

    public async Task CreateFileGroupAsync(FileGroup fileGroup)
    {
        await _uow.FileGroupRepository.CreateAsync(fileGroup);
        await _uow.SaveChangesAsync();
    }
}