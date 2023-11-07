using FluentResults;

namespace FileHub.Core.Errors;

public class FileNotFoundError : Error
{
    public FileNotFoundError(string message = "File Not Found") : base(message)
    {
    }
}