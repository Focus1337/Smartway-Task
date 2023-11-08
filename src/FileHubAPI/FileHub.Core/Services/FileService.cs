using FileHub.Core.Errors;
using FileHub.Core.Interfaces;
using FileHub.Core.Models;
using FluentResults;

namespace FileHub.Core.Services;

public class FileService : IFileService
{
    private readonly IFileGroupRepository _groupRepository;
    private readonly IFileMetaRepository _metaRepository;

    public FileService(IFileGroupRepository groupRepository, IFileMetaRepository metaRepository)
    {
        _groupRepository = groupRepository;
        _metaRepository = metaRepository;
    }

    public async Task<Result<FileMeta>> GetFileMetaAsync(Guid userId, Guid groupId, Guid fileId)
    {
        var result = await _metaRepository.GetFileMetaAsync(userId, groupId, fileId);
        return result is null ? Result.Fail<FileMeta>(new FileNotFoundError()) : Result.Ok(result);
    }

    public async Task<Result<List<FileMeta>>> GetFileGroupAsync(Guid userId, Guid groupId)
    {
        var result = await _groupRepository.GetFileGroupAsync(userId, groupId);
        return result is null ? Result.Fail<List<FileMeta>>(new GroupNotFoundError()) : Result.Ok(result.FileMetas);
    }

    public async Task<Result<List<FileMeta>>> GetListOfFiles(Guid ownerId) =>
        Result.Ok(await _metaRepository.GetListOfFilesAsync(ownerId));

    public async Task<Result<List<FileGroup>>> GetListOfGroups(Guid ownerId) =>
        Result.Ok(await _groupRepository.GetListOfGroups(ownerId));

    public async Task CreateFileGroupAsync(FileGroup fileGroup) =>
        await _groupRepository.CreateFileGroupAsync(fileGroup);
}