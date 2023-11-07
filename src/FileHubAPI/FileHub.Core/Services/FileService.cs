using System.IO.Compression;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using FileHub.Core.Errors;
using FileHub.Core.Interfaces;
using FileHub.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Http;

namespace FileHub.Core.Services;

public class FileService : IFileService, IDisposable
{
    private const string CommonBucketName = "common-bucket";
    private readonly AmazonS3Client _s3Client;
    public readonly Dictionary<Guid, int> ProgressTrackingDict = new();

    public FileService(AmazonS3Client s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task<Result<GetObjectResponse>> GetFileByIdAsync(Guid ownerId, Guid groupId, Guid fileId) =>
        await GetFileAsync(ToObjectKey(ownerId, groupId, fileId));

    public async Task<Result<List<GetObjectResponse>>> GetGroupByIdAsync(Guid ownerId, Guid groupId) =>
        await GetFilesByPrefix(ToGroupPrefix(ownerId, groupId));

    public async Task<Result<FileGroup>> UploadGroupAsync(Guid ownerId, Guid groupId, List<IFormFile> files)
    {
        var taskList = files.Select(file => UploadFileAsync(file.OpenReadStream(), file.ContentType,
                file.FileName, fileId: Guid.NewGuid(), groupId, ownerId))
            .ToList();

        var uploadedFiles = new List<string>();
        while (taskList.Any())
        {
            var finishedTask = await Task.WhenAny(taskList);
            Console.WriteLine("TASK FINISHED");
            taskList.Remove(finishedTask);
            var task = await finishedTask;

            if (task.IsFailed)
                return Result.Fail<FileGroup>(task.Errors[0]);

            uploadedFiles.Add((await finishedTask).Value);
        }

        return Result.Ok(new FileGroup(groupId.ToString(), new List<string>()));
        // return Result.Ok(new FileGroup(groupId.ToString(), uploadedFiles));
    }

    // public async Task<int> GetFileUploadProgress(Guid ownerId, Guid groupId, Guid fileId)
    // {
    //     
    // }
    //
    // public async Task<int> GetGroupUploadProgress(Guid ownerId, Guid groupId)
    // {
    // }

    public async Task<Result<List<GetObjectResponse>>> GetAllFiles(Guid ownerId) =>
        await GetFilesByPrefix(ownerId.ToString());

    public async Task<Result<string>> ShareGroupAsync(Guid ownerId, Guid groupId)
    {
        var result = await GetGroupByIdAsync(ownerId, groupId);
        if (result.IsFailed)
            return Result.Fail<string>(result.Errors[0]);

        var items = result.Value;

        var zipFileName = $"files{DateTime.Now:HHmmss-ddMMyy}.zip";
        var zipStream = new MemoryStream();
        await ZipFiles(zipStream, items, true);

        zipStream.Position = 0;

        var (gId, fId) = (Guid.NewGuid(), Guid.NewGuid());

        var uploadFileResult = await UploadFileAsync(zipStream, "application/zip", zipFileName,
            fId, gId, ownerId);

        await zipStream.DisposeAsync();

        if (uploadFileResult.IsFailed)
            return Result.Fail<string>(uploadFileResult.Errors[0]);

        var shareResult = await ShareFileAsync(ownerId, gId, fId);
        if (shareResult.IsFailed)
            return Result.Fail<string>(shareResult.Errors[0]);

        return Result.Ok(shareResult.Value);
    }

    public async Task<Result<string>> ShareFileAsync(Guid ownerId, Guid groupId, Guid fileId)
    {
        var getResult = await GetFileByIdAsync(ownerId, groupId, fileId);

        if (getResult.IsFailed)
            return Result.Fail<string>(getResult.Errors[0]);

        var file = getResult.Value;

        var urlRequest = new GetPreSignedUrlRequest
        {
            BucketName = CommonBucketName,
            Key = file.Key,
            Expires = DateTime.UtcNow.AddMinutes(1),
            Protocol = Protocol.HTTP,
            Verb = HttpVerb.GET
        };

        return Result.Ok(_s3Client.GetPreSignedURL(urlRequest));
    }

    public async Task<Result> DeleteFileAsync(Guid ownerId, Guid groupId, Guid fileId)
    {
        await ValidateBucket();

        var res = await _s3Client.DeleteObjectAsync(CommonBucketName, ToObjectKey(ownerId, groupId, fileId));
        return res.HttpStatusCode == HttpStatusCode.NoContent
            ? Result.Ok()
            : Result.Fail(new FailedToDeleteError(fileId.ToString()));
    }

    public async Task ZipFiles(Stream stream, List<GetObjectResponse> files, bool leaveOpen = false)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen);
        foreach (var item in files)
        {
            var entry = archive.CreateEntry(Path.GetFileName(item.Metadata["File-Name"]),
                CompressionLevel.NoCompression);

            await using var entryStream = entry.Open();
            await item.ResponseStream.CopyToAsync(entryStream);
        }
    }

    private async Task<Result<string>> UploadFileAsync(Stream stream, string contentType, string fileName,
        Guid fileId, Guid groupId, Guid ownerId)
    {
        await ValidateBucket();

        var request = new PutObjectRequest
        {
            Key = ToObjectKey(ownerId, groupId, fileId),
            InputStream = stream,
            AutoCloseStream = true,
            BucketName = CommonBucketName,
            ContentType = contentType,
            StreamTransferProgress = (_, args) =>
            {
                // Console.WriteLine($"{groupId}| {fileId} | {args.PercentDone}");
                ProgressTrackingDict[fileId] = args.PercentDone;
                // Console.WriteLine(fileId + ": " + args.PercentDone);
                if (args.PercentDone == 100)
                    ProgressTrackingDict.Remove(fileId);
            }
        };
        request.Metadata.Add("Owner-Id", ownerId.ToString());
        request.Metadata.Add("File-Id", fileId.ToString());
        request.Metadata.Add("Group-id", groupId.ToString());
        request.Metadata.Add("File-Name", fileName);
        Console.WriteLine($"{fileName} REQUEST PREPARED");

        var res = await _s3Client.PutObjectAsync(request);
        return res.HttpStatusCode == HttpStatusCode.OK
            ? Result.Ok(fileId.ToString())
            : Result.Fail<string>(new FailedToUploadError(fileName));
    }

    private async Task<Result<List<GetObjectResponse>>> GetFilesByPrefix(string prefix)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = CommonBucketName,
            Prefix = prefix
        };
        var result = await _s3Client.ListObjectsV2Async(request);
        var keyList = result.S3Objects.Select(o => o.Key).ToList();

        var items = new List<GetObjectResponse>();
        if (items.Count == 0)
            return Result.Fail<List<GetObjectResponse>>(new GroupNotFoundError());

        var taskList = keyList.Select(GetItemAsync).ToList();

        while (taskList.Any())
        {
            var finishedTask = await Task.WhenAny(taskList);
            taskList.Remove(finishedTask);
            var task = await finishedTask;
            if (task.IsFailed)
                return Result.Fail<List<GetObjectResponse>>(task.Errors[0]);
        }

        return Result.Ok(items);

        async Task<Result> GetItemAsync(string key)
        {
            var res = await GetFileAsync(key);
            if (res.IsFailed)
                return Result.Fail(res.Errors[0]);

            var file = res.Value;
            items.Add(file);
            return Result.Ok();
        }
    }

    private async Task<Result<GetObjectResponse>> GetFileAsync(string key)
    {
        await ValidateBucket();
        try
        {
            var file = await _s3Client.GetObjectAsync(CommonBucketName, key);
            return Result.Ok(file);
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return Result.Fail<GetObjectResponse>(new FileNotFoundError()
                    .WithMetadata("StatusCode", ex.StatusCode)
                    .WithMetadata("ErrorCode", ex.ErrorCode));

            throw;
        }
    }

    private async Task ValidateBucket()
    {
        if (!(await _s3Client.ListBucketsAsync()).Buckets.Exists(bucket => bucket.BucketName == CommonBucketName))
            await _s3Client.PutBucketAsync(CommonBucketName);
    }

    private static string ToObjectKey(Guid ownerId, Guid groupId, Guid fileId) =>
        $"{ToGroupPrefix(ownerId, groupId)}/{fileId}";

    private static string ToGroupPrefix(Guid ownerId, Guid groupId) =>
        $"{ownerId}/{groupId}";

    public void Dispose()
    {
        _s3Client.Dispose();
    }
}