using System.Net.Mime;
using FileHub.Core.Interfaces;
using FileHub.Presentation.Attributes;
using FileHub.Presentation.Models;
using FileHub.Presentation.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileHub.Presentation.Controllers;

[OpenIddictAuthorize]
[Route("api/[controller]")]
[ApiController]
public class FilesController : CustomControllerBase
{
    private readonly IFileService _fileService;
    private readonly ApplicationUserService _userService;

    public FilesController(IFileService fileService, ApplicationUserService userService)
    {
        _fileService = fileService;
        _userService = userService;
    }

    [HttpGet("{groupId:guid}/{fileId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(FileDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.GetFileByIdAsync(user.Id, groupId, fileId);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        var value = result.Value;
        return Ok(FileDetailsDto.ToDto(value));
    }

    [HttpGet("{groupId:guid}")]
    [ProducesResponseType(typeof(List<FileDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupOfFilesAsync([FromRoute] Guid groupId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.GetGroupByIdAsync(user.Id, groupId);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        var response = result.Value.Select(FileDetailsDto.ToDto).ToList();
        return Ok(response);
    }

    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(List<FileDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllFilesAsync()
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.GetAllFiles(user.Id);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        var response = result.Value.Select(FileDetailsDto.ToDto).ToList();
        return Ok(response);
    }

    [HttpGet("{groupId:guid}/{fileId:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFileAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.GetFileByIdAsync(user.Id, groupId, fileId);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        var value = result.Value;
        return File(value.ResponseStream, value.Headers.ContentType);
    }

    [HttpGet("{groupId:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task DownloadGroupOfFilesAsync([FromRoute] Guid groupId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new List<ErrorModel> { new("UserNotFound", "User Not Found") });
            return;
        }

        var result = await _fileService.GetGroupByIdAsync(user.Id, groupId);
        if (result.IsFailed)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(ErrorModel.FromErrorList(result.Errors));
            return;
        }

        Response.ContentType = "application/octet-stream";
        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"files{DateTime.Now:HHmmss-ddMMyy}.zip\"");
        await _fileService.ZipFiles(Response.BodyWriter.AsStream(), result.Value);
    }

    [HttpPost("{groupId:guid}/{fileId:guid}/share")]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ShareFileAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.ShareFileAsync(user.Id, groupId, fileId);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        return Ok(result.Value);
    }

    [HttpPost("{groupId:guid}/share")]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ShareGroupOfFilesAsync([FromRoute] Guid groupId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.ShareGroupAsync(user.Id, groupId);
        if (result.IsFailed)
            return BadRequest(result.Errors[0].Message);

        return Ok(result.Value);
    }

    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(FileGroupDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFilesAsync([FromForm] List<IFormFile> files)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var groupId = Guid.NewGuid();
        var result = await _fileService.UploadGroupAsync(user.Id, groupId, files);
        if (result.IsFailed)
            return BadRequest(ErrorModel.FromErrorList(result.Errors));

        return Ok(FileGroupDto.FromEntity(result.Value));
    }

    [HttpGet("{groupId:guid}/{fileId:guid}/progress")]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileProgressAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        // if (await _fileService.GetByFileIdAsync(user.Id, id) is null)
        //     return BadRequest("File not found");

        // if (!_fileService.ProgressTrackingDict.ContainsKey(id))
        //     return Ok("Already transferred");
        //
        // var progress = _fileService.ProgressTrackingDict.GetValueOrDefault(id);

        // return Ok(progress);
        return Ok();
    }

    [HttpGet("{groupId:guid}/progress")]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupOfFilesProgressAsync([FromRoute] Guid groupId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.GetGroupByIdAsync(user.Id, groupId);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        // var taskList = ids.Select(id => GetUploadProgress(user.Id, id)).ToList();
        // var taskCount = taskList.Count;
        // var progressSum = 0;
        //
        // while (taskList.Any())
        // {
        //     var finishedTask = await Task.WhenAny(taskList);
        //     taskList.Remove(finishedTask);
        //     var task = await finishedTask;
        //     if (task.IsSuccess)
        //         progressSum += task.Value;
        // }
        //
        // async Task<Result<int>> GetUploadProgress(Guid userId, string fileId)
        // {
        //     var result = await _fileService.GetFileByIdAsync(userId, fileId);
        //     if (result.IsFailed)
        //     {
        //         Response.StatusCode = 400;
        //         await Response.WriteAsJsonAsync(new ErrorDto("error", "no such file"));
        //         return Result.Fail<int>(result.Errors[0]);
        //     }
        //
        //     if (!_fileService.ProgressTrackingDict.ContainsKey(fileId))
        //     {
        //         Response.StatusCode = 400;
        //         await Response.WriteAsJsonAsync(new ErrorDto("error", "File already transferred"));
        //         return Result.Fail<int>("File already transferred");
        //     }
        //
        //     return Result.Ok(_fileService.ProgressTrackingDict.GetValueOrDefault(fileId));
        // }

        // return Ok(progressSum / taskCount);

        return Ok();
    }

    [HttpDelete("{groupId:guid}/{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFileAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var deleteResult = await _fileService.DeleteFileAsync(user.Id, groupId, fileId);
        return deleteResult.IsSuccess ? NoContent() : NotFound(ErrorModel.FromErrorList(deleteResult.Errors));
    }
}