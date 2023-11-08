namespace FileHub.Core.Interfaces;

public interface IUnitOfWork
{
    IFileMetaRepository FileMetaRepository { get; }
    IFileGroupRepository FileGroupRepository { get; }
    Task SaveChangesAsync();
}