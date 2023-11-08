using FileHub.Core.Models;

namespace FileHub.Core.Interfaces;

public interface IFileGroupRepository
{
    Task<FileGroup?> GetFileGroupAsync(Guid userId, Guid groupId);
    Task CreateFileGroupAsync(FileGroup fileGroup);
    Task<List<FileGroup>> GetListOfGroups(Guid ownerId);
}