namespace FileHub.Core.Models;

public class File
{
    public Guid StorageKey { get; set; } = Guid.NewGuid();
    public required string FileName { get; set; }
    public required ApplicationUser User { get; set; }
}