using FileHub.Core.Models;

namespace FileHub.Presentation.Models;

public class UploadedFileGroupDto
{
    public string GroupId { get; init; }
    public List<string> FileIds { get; init; }

    public UploadedFileGroupDto(string groupId, List<string> fileIds)
    {
        FileIds = fileIds;
        GroupId = groupId;
    }

    public static UploadedFileGroupDto FromEntity(UploadedFileGroup uploadedFileGroupDto) =>
        new(uploadedFileGroupDto.GroupId, new List<string>(uploadedFileGroupDto.FileIds));
}