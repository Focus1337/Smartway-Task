using System.IO.Compression;
using System.Net.Mime;
using Amazon.S3.Model;
using FileHub.Presentation.Attributes;
using FileHub.Presentation.Models;
using FileHub.Presentation.Services;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace FileHub.Presentation.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FilesController : ControllerBase
{
    private readonly FileService _fileService;
    private readonly ApplicationUserService _userService;

    public FilesController(FileService fileService, ApplicationUserService userService)
    {
        _fileService = fileService;
        _userService = userService;
    }

    [OpenIddictAuthorize]
    [HttpGet("{groupId:guid}/{fileId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(FileDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return NotFound();

        var result = await _fileService.GetFileByIdAsync(user.Id, groupId, fileId);
        if (result.IsFailed)
            return NotFound();

        var value = result.Value;
        return Ok(FileDetailsDto.ToDto(value));
    }

    [OpenIddictAuthorize]
    [HttpGet("{groupId:guid}")]
    [ProducesResponseType(typeof(List<FileDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupOfFilesAsync([FromRoute] Guid groupId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return NotFound();

        var result = await _fileService.GetGroupOfFilesByIdAsync(user.Id, groupId);
        if (result.IsFailed)
            return NotFound();

        var response = result.Value.Select(FileDetailsDto.ToDto).ToList();
        return Ok(response);
    }

    [OpenIddictAuthorize]
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(List<FileDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllFilesAsync()
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return NotFound();

        var result = await _fileService.GetAllFiles(user.Id);
        if (result.IsFailed)
            return NotFound();

        var response = result.Value.Select(FileDetailsDto.ToDto).ToList();
        return Ok(response);
    }

    [OpenIddictAuthorize]
    [HttpGet("{groupId:guid}/{fileId:guid}/stream")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileStreamAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return NotFound();

        var result = await _fileService.GetFileByIdAsync(user.Id, groupId, fileId);
        if (result.IsFailed)
            return NotFound();

        var value = result.Value;
        return File(value.ResponseStream, value.Headers.ContentType);
    }

    [OpenIddictAuthorize]
    [HttpGet("{groupId:guid}/stream")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task GetGroupOfFilesStreamAsync([FromRoute] Guid groupId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
        {
            Response.StatusCode = 404;
            await Response.WriteAsJsonAsync(new ErrorDto("error", "Not Found"));
            return;
        }

        var result = await _fileService.GetGroupOfFilesByIdAsync(user.Id, groupId);
        if (result.IsFailed)
        {
            Response.StatusCode = 404;
            await Response.WriteAsJsonAsync(new ErrorDto("error", "File Not Found"));
            return;
        }

        Response.ContentType = "application/octet-stream";
        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"files{DateTime.Now:HHmmss-ddMMyy}.zip\"");
        await _fileService.ZipFiles(Response.BodyWriter.AsStream(), result.Value);
    }

    [OpenIddictAuthorize]
    [HttpPost("{groupId:guid}/{fileId:guid}/share")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShareFileAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return NotFound();

        var result = await _fileService.ShareFileAsync(user.Id, groupId, fileId);
        if (result.IsFailed)
            return NotFound();

        return Ok(result.Value);
    }

    // [OpenIddictAuthorize]
    // [HttpPost("{groupId:guid}/share")]
    // [ProducesResponseType(StatusCodes.Status404NotFound)]
    // public async Task<IActionResult> ShareMultipleFilesAsync([FromRoute] Guid groupId)
    // {
    //     if (await _userService.GetCurrentUser() is not { } user)
    //         return NotFound();
    //
    //     var result = await _fileService.GetGroupOfFilesByIdAsync(user.Id, groupId);
    //     if (result.IsFailed)
    //         return NotFound();
    //
    //     var items = result.Value;
    //
    //     var zipFileName = $"files{DateTime.Now:HHmmss-ddMMyy}.zip";
    //     var zipStream = new MemoryStream();
    //
    //     using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
    //     {
    //         foreach (var item in items)
    //         {
    //             var entry = archive.CreateEntry(Path.GetFileName(item.Metadata["File-Name"]),
    //                 CompressionLevel.NoCompression);
    //
    //             await using var entryStream = entry.Open();
    //             await item.ResponseStream.CopyToAsync(entryStream);
    //         }
    //     }
    //
    //     zipStream.Position = 0;
    //
    //     await _fileService.UploadFileAsync(zipStream, "application/zip", zipFileName,
    //         fileId, user.Id.ToString());
    //
    //     var shareResult = await _fileService.ShareFileAsync(user.Id, fileId);
    //     if (shareResult.IsFailed)
    //         return BadRequest();
    //
    //     return Ok(shareResult.Value);
    //
    //     async Task GetItemAsync(Guid userId, string id)
    //     {
    //         var item = await _fileService.GetFileByIdAsync(userId, id);
    //         if (item.IsFailed)
    //             return;
    //
    //         var z = item.Value;
    //
    //         if (z is null)
    //             return;
    //
    //
    //         items.Add(z);
    //     }
    // }

    // [OpenIddictAuthorize]
    // [HttpPost("upload")]
    // [DisableRequestSizeLimit]
    // [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
    // [Consumes("multipart/form-data")]
    // [ProducesResponseType(StatusCodes.Status404NotFound)]
    // public async Task<IActionResult> UploadFilesAsync([FromForm] List<IFormFile> files)
    // {
    //     if (await _userService.GetCurrentUser() is not { } user)
    //         return BadRequest();
    //
    //     var taskList = files.Select(file => _fileService.UploadFileAsync(file.OpenReadStream(), file.ContentType,
    //             file.FileName, fileId: Guid.NewGuid().ToString(), user.Id.ToString()))
    //         .ToList();
    //
    //     var uploadedFiles = new List<string>();
    //
    //     while (taskList.Any())
    //     {
    //         var finishedTask = await Task.WhenAny(taskList);
    //         taskList.Remove(finishedTask);
    //         var task = await finishedTask;
    //         if (task.IsSuccess)
    //             uploadedFiles.Add((await finishedTask).Value);
    //     }
    //
    //     return Ok(uploadedFiles);
    // }

    [OpenIddictAuthorize]
    [HttpGet("{groupId:guid}/{fileId:guid}/progress")]
    public async Task<IActionResult> GetFileProgressAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return NotFound("User not found");

        // if (await _fileService.GetByFileIdAsync(user.Id, id) is null)
        //     return BadRequest("File not found");

        // if (!_fileService.ProgressTrackingDict.ContainsKey(id))
        //     return Ok("Already transferred");
        //
        // var progress = _fileService.ProgressTrackingDict.GetValueOrDefault(id);

        // return Ok(progress);
        return Ok();
    }

    // [OpenIddictAuthorize]
    // [HttpGet("multiple/progress")]
    // [ProducesResponseType(StatusCodes.Status404NotFound)]
    // public async Task<IActionResult> GetMultipleFilesProgressAsync(List<string> ids)
    // {
    //     if (await _userService.GetCurrentUser() is not { } user)
    //         return BadRequest();
    //
    //     var taskList = ids.Select(id => GetUploadProgress(user.Id, id)).ToList();
    //     var taskCount = taskList.Count;
    //     var progressSum = 0;
    //
    //     while (taskList.Any())
    //     {
    //         var finishedTask = await Task.WhenAny(taskList);
    //         taskList.Remove(finishedTask);
    //         var task = await finishedTask;
    //         if (task.IsSuccess)
    //             progressSum += task.Value;
    //     }
    //
    //     async Task<Result<int>> GetUploadProgress(Guid userId, string fileId)
    //     {
    //         var result = await _fileService.GetFileByIdAsync(userId, fileId);
    //         if (result.IsFailed)
    //         {
    //             Response.StatusCode = 400;
    //             await Response.WriteAsJsonAsync(new ErrorDto("error", "no such file"));
    //             return Result.Fail<int>(result.Errors[0]);
    //         }
    //
    //         if (!_fileService.ProgressTrackingDict.ContainsKey(fileId))
    //         {
    //             Response.StatusCode = 400;
    //             await Response.WriteAsJsonAsync(new ErrorDto("error", "File already transferred"));
    //             return Result.Fail<int>("File already transferred");
    //         }
    //
    //         return Result.Ok(_fileService.ProgressTrackingDict.GetValueOrDefault(fileId));
    //     }
    //
    //     return Ok(progressSum / taskCount);
    // }

    [OpenIddictAuthorize]
    [HttpDelete("{groupId:guid}/{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFileAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return NotFound();

        var deleteResult = await _fileService.DeleteFileAsync(user.Id, groupId, fileId);
        return deleteResult.IsSuccess ? NoContent() : NotFound();
    }
}