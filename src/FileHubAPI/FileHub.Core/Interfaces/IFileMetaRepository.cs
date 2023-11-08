using FileHub.Core.Models;

namespace FileHub.Core.Interfaces;

public interface IFileMetaRepository
{
    Task<List<FileMeta>> GetListOfFilesAsync(Guid userId);
    Task<FileMeta?> GetFileMetaAsync(Guid userId, Guid groupId, Guid fileId);
}