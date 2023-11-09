using System.Net.Mime;
using FileHub.Core.Interfaces;
using FileHub.Core.Models;
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
    private readonly IS3Service _s3Service;
    private readonly IApplicationUserService _userService;
    private readonly IFileService _fileService;

    public FilesController(IApplicationUserService userService, IS3Service s3Service, IFileService fileService)
    {
        _userService = userService;
        _s3Service = s3Service;
        _fileService = fileService;
    }

    [HttpGet("{groupId:guid}/{fileId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(FileMetaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.GetFileMetaAsync(user.Id, groupId, fileId);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        var value = result.Value;
        return Ok(FileMetaDto.EntityToDto(value));
    }

    [HttpGet("{groupId:guid}")]
    [ProducesResponseType(typeof(List<FileMetaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupOfFilesAsync([FromRoute] Guid groupId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.GetFileGroupAsync(user.Id, groupId);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        var response = result.Value.Select(FileMetaDto.EntityToDto).ToList();
        return Ok(response);
    }

    [HttpGet("all-files")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(List<FileMetaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllFilesAsync()
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.GetListOfFiles(user.Id);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        var response = result.Value.Select(FileMetaDto.EntityToDto).ToList();
        return Ok(response);
    }

    [HttpGet("all-groups")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(List<FileGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllGroupsAsync()
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.GetListOfGroups(user.Id);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        var response = result.Value.Select(FileGroupDto.EntityToDto).ToList();
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

        var result = await _s3Service.GetFileByIdAsync(user.Id, groupId, fileId);
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
            await Response.WriteAsJsonAsync(new List<ErrorModel> { new("UserNotFound", "User not found") });
            return;
        }

        var result = await _s3Service.GetGroupByIdAsync(user.Id, groupId);
        if (result.IsFailed)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(ErrorModel.FromErrorList(result.Errors));
            return;
        }

        Response.ContentType = "application/octet-stream";
        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"files{DateTime.Now:HHmmss-ddMMyy}.zip\"");
        await _s3Service.ZipFiles(Response.BodyWriter.AsStream(), result.Value);
    }

    [HttpPost("{groupId:guid}/{fileId:guid}/share")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShareFileAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _s3Service.ShareFileAsync(user.Id, groupId, fileId);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        return Ok(result.Value);
    }

    [HttpPost("{groupId:guid}/share")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShareGroupOfFilesAsync([FromRoute] Guid groupId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _s3Service.ShareGroupAsync(user.Id, groupId);
        if (result.IsFailed)
            return BadRequest(result.Errors[0].Message);

        return Ok(result.Value);
    }

    [HttpPut("{groupId:guid}")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadedFileGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadFilesAsync([FromRoute] Guid groupId, [FromForm] List<IFormFile> files)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var fileGuids = new List<Guid>();
        for (var i = 0; i < files.Count; i++)
            fileGuids.Add(Guid.NewGuid());

        await _fileService.CreateFileGroupAsync(new FileGroup(groupId, user.Id)
        {
            FileMetas = files.Select((formFile, index) => new FileMeta
            {
                Id = fileGuids[index], UserId = user.Id, GroupId = groupId, FileName = formFile.FileName,
                LastModified = DateTime.Now
            }).ToList()
        });

        var result = await _s3Service.UploadGroupAsync(user.Id, groupId, files, fileGuids);
        if (result.IsFailed)
            return BadRequest(ErrorModel.FromErrorList(result.Errors));

        return Ok(UploadedFileGroupDto.FromEntity(result.Value));
    }

    [HttpGet("{groupId:guid}/{fileId:guid}/progress")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileProgressAsync([FromRoute] Guid groupId, [FromRoute] Guid fileId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _s3Service.GetFileUploadProgress(user.Id, groupId, fileId);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        return Ok(result.Value);
    }

    [HttpGet("{groupId:guid}/progress")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(List<ErrorModel>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupOfFilesProgressAsync([FromRoute] Guid groupId)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return UserNotFoundBadRequest();

        var result = await _fileService.GetFileGroupAsync(user.Id, groupId);
        if (result.IsFailed)
            return NotFound(ErrorModel.FromErrorList(result.Errors));

        var progressResult = await _s3Service.GetGroupUploadProgress(user.Id, groupId);
        if (progressResult.IsFailed)
            return NotFound(ErrorModel.FromErrorList(progressResult.Errors));

        return Ok(progressResult.Value);
    }
}