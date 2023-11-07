using FluentResults;

namespace FileHub.Presentation.Errors;

public class FileNotFoundError : Error
{
    public FileNotFoundError(string message = "File Not Found") : base(message)
    {
    }
}