namespace FileHub.Core.Models;

public class UploadedFileGroup
{
    public string GroupId { get; init; }
    public List<string> FileIds { get; init; }

    public UploadedFileGroup(string groupId, List<string> fileIds)
    {
        GroupId = groupId;
        FileIds = new List<string>(fileIds);
    }
}