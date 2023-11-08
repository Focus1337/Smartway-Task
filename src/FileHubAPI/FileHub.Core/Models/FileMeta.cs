namespace FileHub.Core.Models;

public class FileMeta
{
    public Guid Id { get; set; }
    public required Guid GroupId { get; set; }
    public required Guid UserId { get; set; }
    public required string FileName { get; init; }
    public required DateTime LastModified { get; init; }
}