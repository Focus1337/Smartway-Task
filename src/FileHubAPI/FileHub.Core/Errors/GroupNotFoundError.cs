using FluentResults;

namespace FileHub.Core.Errors;

public class GroupNotFoundError : Error
{
    public GroupNotFoundError(string message = "Group not found.") : base(message)
    {
    }
}