using FileHub.Core.Models;
using FluentResults;

namespace FileHub.Core.Interfaces;

public interface IFileService
{
    Task<Result<FileMeta>> GetFileMetaAsync(Guid userId, Guid groupId, Guid fileId);
    Task<Result<List<FileMeta>>> GetFileGroupAsync(Guid userId, Guid groupId);
    Task<Result<List<FileMeta>>> GetListOfFiles(Guid ownerId);
    Task<Result<List<FileGroup>>> GetListOfGroups(Guid ownerId);
    Task CreateFileGroupAsync(FileGroup fileGroup);
}