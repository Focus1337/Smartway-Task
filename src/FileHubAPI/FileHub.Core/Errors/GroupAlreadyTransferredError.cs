using FluentResults;

namespace FileHub.Core.Errors;

public class GroupAlreadyTransferredError : Error
{
    public GroupAlreadyTransferredError(string message = "Group already transferred.") : base(message)
    {
    }
}