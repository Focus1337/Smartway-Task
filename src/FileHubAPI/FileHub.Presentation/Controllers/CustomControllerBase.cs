using FileHub.Presentation.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileHub.Presentation.Controllers;

public class CustomControllerBase : ControllerBase
{
    internal BadRequestObjectResult UserNotFoundBadRequest() =>
        BadRequest(new List<ErrorModel> { new("UserNotFound", "User Not Found") });
}