using FluentResults;

namespace FileHub.Core.Errors;

public class FileNotFoundError : Error
{
    public FileNotFoundError(string message = "File not found.") : base(message)
    {
    }
}