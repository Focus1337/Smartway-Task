using FileHub.Core.Models;

namespace FileHub.Presentation.Models;

public class FileGroupDto
{
    public string GroupId { get; init; }
    public List<string> FileIds { get; init; }

    public FileGroupDto(string groupId, List<string> fileIds)
    {
        GroupId = groupId;
        FileIds = new List<string>(fileIds);
    }

    public static FileGroupDto FromEntity(FileGroup fileGroup) =>
        new(fileGroup.GroupId, new List<string>(fileGroup.FileIds));
}