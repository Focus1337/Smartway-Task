using FluentResults;

namespace FileHub.Core.Errors;

public class FailedToUploadError : Error
{
    public FailedToUploadError(string fileName) : base($"Failed to upload file {fileName}")
    {
    }
}