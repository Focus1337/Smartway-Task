using System.IO.Compression;
using Amazon.S3;
using Amazon.S3.Model;
using FileHub.Infrastructure.Repositories;
using FileHub.Presentation.Attributes;
using FileHub.Presentation.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FileHub.Presentation.Controllers;

public record S3ObjectDto(string Name, string PreSignedUrl);

public record GetFileResponse(string FileId, string OwnerId, string FileName, DateTime LastModified,
    long ContentLength);

[Route("api/[controller]")]
[ApiController]
public class FilesController : ControllerBase
{
    private readonly FileService _fileService;
    private readonly ApplicationUserService _userService;
    private readonly FileRepository _fileRepository;

    public FilesController(FileService fileService, ApplicationUserService userService,
        FileRepository fileRepository)
    {
        _fileService = fileService;
        _userService = userService;
        _fileRepository = fileRepository;
    }


    [OpenIddictAuthorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFileAsync(string id)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return BadRequest();

        var result = await _fileService.GetByFileIdAsync(user.Id, id);
        if (result is null)
            return BadRequest();

        return Ok(new GetFileResponse(result.Metadata["File-Id"], result.Metadata["Owner-Id"],
            result.Metadata["File-Name"], result.LastModified, result.ContentLength));
    }

    [OpenIddictAuthorize]
    [HttpGet("multiple")]
    public async Task<IActionResult> GetMultipleFilesAsync([FromQuery] List<string> ids)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return BadRequest();

        var items = new List<GetFileResponse>();
        var taskList = ids.Select(fileId => GetItemAsync(user.Id, fileId)).ToList();

        while (taskList.Any())
        {
            var finishedTask = await Task.WhenAny(taskList);
            taskList.Remove(finishedTask);
            await finishedTask;
        }

        return Ok(items);

        async Task GetItemAsync(Guid userId, string fileId)
        {
            var item = await _fileService.GetByFileIdAsync(userId, fileId);
            if (item is null) return;
            items.Add(new GetFileResponse(item.Metadata["File-Id"], item.Metadata["Owner-Id"],
                item.Metadata["File-Name"], item.LastModified, item.ContentLength));
        }
    }

    [OpenIddictAuthorize]
    [HttpGet]
    public async Task<IActionResult> GetAllFilesAsync()
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return BadRequest();

        var result = await _fileService.GetAllFiles(user.Id);
        var response = result.Select(item => new GetFileResponse(item.Metadata["File-Id"], item.Metadata["Owner-Id"],
            item.Metadata["File-Name"], item.LastModified, item.ContentLength)).ToList();

        return Ok(response);
    }

    [OpenIddictAuthorize]
    [HttpGet("{id}/stream")]
    public async Task<IActionResult> GetFileStreamAsync(string id)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return BadRequest();

        var result = await _fileService.GetByFileIdAsync(user.Id, id);
        if (result is null)
            return BadRequest();

        return File(result.ResponseStream, result.Headers.ContentType);
    }

    [OpenIddictAuthorize]
    [HttpGet("multiple/stream")]
    public async Task GetMultipleFilesStreamAsync([FromQuery] List<string> ids)
    {
        if (await _userService.GetCurrentUser() is not { } user)
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new S3ObjectDto("error", "smth"));
            return;
        }

        var items = new List<GetObjectResponse>();
        var taskList = ids.Select(fileId => GetItemAsync(user.Id, fileId)).ToList();

        while (taskList.Any())
        {
            var finishedTask = await Task.WhenAny(taskList);
            taskList.Remove(finishedTask);
            await finishedTask;
            if (Response.StatusCode == 404)
                return;
        }

        async Task GetItemAsync(Guid userId, string fileId)
        {
            var item = await _fileService.GetByFileIdAsync(userId, fileId);
            if (item is null)
            {
                Response.StatusCode = 404;
                await Response.WriteAsJsonAsync(new S3ObjectDto("doesn't exist", fileId));
                Console.WriteLine("STOPPED STOPPED STOPPED STOPPED");
                return;
            }

            items.Add(item);
        }

        Console.WriteLine("Starting to!!!!");

        Response.ContentType = "application/octet-stream";
        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"files{DateTime.Now:HHmmss-ddMMyy}.zip\"");

        using var archive = new ZipArchive(Response.BodyWriter.AsStream(), ZipArchiveMode.Create);
        foreach (var item in items)
        {
            var entry = archive.CreateEntry(Path.GetFileName(item.Metadata["File-Name"]),
                CompressionLevel.NoCompression);

            await using var entryStream = entry.Open();
            await item.ResponseStream.CopyToAsync(entryStream);
        }
    }

    [OpenIddictAuthorize]
    [HttpPost("{id}/share")]
    public async Task<IActionResult> ShareFileAsync(string id)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return BadRequest();

        var result = await _fileService.ShareFileAsync(user.Id, id);

        if (result is null)
            return BadRequest();

        return Ok(result);
    }

    [OpenIddictAuthorize]
    [HttpPost("multiple/share")]
    public async Task<IActionResult> ShareMultipleFilesAsync([FromQuery] List<string> ids)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return BadRequest();

        var items = new List<GetObjectResponse>();
        var taskList = ids.Select(fileId => GetItemAsync(user.Id, fileId)).ToList();
        var fileId = Guid.NewGuid().ToString();

        while (taskList.Any())
        {
            var finishedTask = await Task.WhenAny(taskList);
            taskList.Remove(finishedTask);
            await finishedTask;
        }

        var zipFileName = $"files{DateTime.Now:HHmmss-ddMMyy}.zip";
        var zipStream = new MemoryStream();

        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            foreach (var item in items)
            {
                var entry = archive.CreateEntry(Path.GetFileName(item.Metadata["File-Name"]),
                    CompressionLevel.NoCompression);

                await using var entryStream = entry.Open();
                await item.ResponseStream.CopyToAsync(entryStream);
            }
        }

        zipStream.Position = 0;

        await _fileService.UploadFileAsync(zipStream, "application/zip", zipFileName,
            fileId, user.Id.ToString());

        var result = await _fileService.ShareFileAsync(user.Id, fileId);

        if (result is null)
            return BadRequest();

        return Ok(result);

        async Task GetItemAsync(Guid userId, string id)
        {
            var item = await _fileService.GetByFileIdAsync(userId, id);
            if (item is null)
            {
                return;
            }

            items.Add(item);
        }
    }

    [OpenIddictAuthorize]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = Int32.MaxValue, ValueLengthLimit = Int32.MaxValue)]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFilesAsync([FromForm] List<IFormFile> files)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return BadRequest();

        var taskList = files.Select(file => _fileService.UploadFileAsync(file.OpenReadStream(), file.ContentType,
                file.FileName, fileId: Guid.NewGuid().ToString(), user.Id.ToString()))
            .ToList();

        var uploadedFiles = new List<string>();

        while (taskList.Any())
        {
            var finishedTask = await Task.WhenAny(taskList);
            taskList.Remove(finishedTask);
            uploadedFiles.Add(await finishedTask);
        }

        return Ok(uploadedFiles);
    }

    [OpenIddictAuthorize]
    [HttpGet("{id}/progress")]
    public async Task<IActionResult> GetFileProgressAsync(string id)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return BadRequest("User not found");

        // if (await _fileService.GetByFileIdAsync(user.Id, id) is null)
        //     return BadRequest("File not found");

        if (!_fileService.ProgressTrackingDict.ContainsKey(id))
            return BadRequest("Already transferred");
        var progress = _fileService.ProgressTrackingDict.GetValueOrDefault(id);

        return Ok(progress);
    }

    [OpenIddictAuthorize]
    [HttpGet("multiple/progress")]
    public async Task<IActionResult> GetMultipleFilesProgressAsync(List<string> ids)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return BadRequest();

        var taskList = ids.Select(id => GetUploadProgress(user.Id, id)).ToList();
        var taskCount = taskList.Count;
        var progressSum = 0;

        while (taskList.Any())
        {
            var finishedTask = await Task.WhenAny(taskList);
            taskList.Remove(finishedTask);
            progressSum += await finishedTask;
        }

        async Task<int> GetUploadProgress(Guid userId, string fileId)
        {
            if (await _fileService.GetByFileIdAsync(userId, fileId) is null)
            {
                Response.StatusCode = 400;
                await Response.WriteAsJsonAsync(new S3ObjectDto("error", "no such file"));
                return 0;
            }

            if (!_fileService.ProgressTrackingDict.ContainsKey(fileId))
            {
                Response.StatusCode = 400;
                await Response.WriteAsJsonAsync(new S3ObjectDto("error", "File already transferred"));
                return 0;
            }
            // return BadRequest("Already transferred");

            return _fileService.ProgressTrackingDict.GetValueOrDefault(fileId);
        }

        return Ok(progressSum / taskCount);
    }

    [OpenIddictAuthorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFileAsync(string id)
    {
        if (await _userService.GetCurrentUser() is not { } user)
            return BadRequest();

        await _fileService.RemoveFileAsync(id, user.Id.ToString());
        // var file = (await _fileRepository.GetAllAsync()).FirstOrDefault(f => f.StorageKey == Guid.Parse(id));
        // if (file is null)
        //     return BadRequest();

        // await _fileRepository.DeleteAsync(file);
        // await _fileRepository.
        return NoContent();
    }
}