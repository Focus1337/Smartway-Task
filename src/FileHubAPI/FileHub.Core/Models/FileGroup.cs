namespace FileHub.Core.Models;

public class FileGroup
{
    public string GroupId { get; init; }
    public List<string> FileIds { get; init; }

    public FileGroup(string groupId, List<string> fileIds)
    {
        GroupId = groupId;
        FileIds = new List<string>(fileIds);
    }
}