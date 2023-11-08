using FluentResults;

namespace FileHub.Core.Errors;

public class FileAlreadyTransferredError: Error
{
    public FileAlreadyTransferredError(string message = "File already transferred.") : base(message)
    {
    }
}