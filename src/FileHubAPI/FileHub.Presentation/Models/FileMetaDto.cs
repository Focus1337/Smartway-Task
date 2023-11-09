using Amazon.S3.Model;
using FileHub.Core.Models;

namespace FileHub.Presentation.Models;

public class FileMetaDto
{
    public string FileId { get; init; }
    public string GroupId { get; init; }
    public string OwnerId { get; init; }
    public string FileName { get; init; }
    public DateTime LastModified { get; init; }

    public FileMetaDto(string fileId, string groupId, string ownerId, string fileName, DateTime lastModified)
    {
        FileId = fileId;
        GroupId = groupId;
        OwnerId = ownerId;
        FileName = fileName;
        LastModified = lastModified;
    }

    public static FileMetaDto ObjectResponseToDto(GetObjectResponse model) =>
        new(model.Metadata["File-Id"], model.Metadata["Group-Id"], model.Metadata["Owner-Id"],
            model.Metadata["File-Name"], model.LastModified);

    public static FileMetaDto EntityToDto(FileMeta model) =>
        new(model.Id.ToString(), model.GroupId.ToString(), model.UserId.ToString(), model.FileName, model.LastModified);
}