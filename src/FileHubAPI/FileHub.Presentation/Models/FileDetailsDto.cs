using Amazon.S3.Model;

namespace FileHub.Presentation.Models;

public class FileDetailsDto
{
    public string FileId { get; init; }
    public string GroupId { get; init; }
    public string OwnerId { get; init; }
    public string FileName { get; init; }
    public DateTime LastModified { get; init; }
    public long ContentLength { get; init; }

    public FileDetailsDto(string fileId, string groupId, string ownerId, string fileName, DateTime lastModified,
        long contentLength)
    {
        FileId = fileId;
        GroupId = groupId;
        OwnerId = ownerId;
        FileName = fileName;
        LastModified = lastModified;
        ContentLength = contentLength;
    }

    public static FileDetailsDto ToDto(GetObjectResponse model) =>
        new(model.Metadata["File-Id"], model.Metadata["Group-Id"], model.Metadata["Owner-Id"],
            model.Metadata["File-Name"], model.LastModified, model.ContentLength);
}