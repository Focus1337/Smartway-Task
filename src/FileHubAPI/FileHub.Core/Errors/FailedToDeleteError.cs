using FluentResults;

namespace FileHub.Core.Errors;

public class FailedToDeleteError : Error
{
    public FailedToDeleteError(string fileId) : base($"Failed to delete file with ID {fileId}")
    {
    }
}