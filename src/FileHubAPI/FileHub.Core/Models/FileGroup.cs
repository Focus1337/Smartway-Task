namespace FileHub.Core.Models;

public class FileGroup
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required List<FileMeta> FileMetas { get; set; }

    public FileGroup(Guid id, Guid userId)
    {
        Id = id;
        UserId = userId;
    }
}