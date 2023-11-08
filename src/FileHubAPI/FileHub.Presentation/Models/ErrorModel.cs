using FluentResults;
using Microsoft.AspNetCore.Identity;

namespace FileHub.Presentation.Models;

public class ErrorModel
{
    public string Code { get; init; }
    public string Description { get; init; }

    public ErrorModel(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public static ErrorModel FromError(IError error) =>
        new(error.GetType().Name, error.Message);

    public static List<ErrorModel> FromErrorList(List<IError> errorList) =>
        new(errorList.Select(FromError));

    public static List<ErrorModel> FromIdentityResult(IdentityResult identityResult) =>
        new(identityResult.Errors.Select(error => new ErrorModel(error.Code, error.Description)));
}