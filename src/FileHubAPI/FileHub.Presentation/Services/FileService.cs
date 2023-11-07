using System.IO.Compression;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using FileHub.Presentation.Errors;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace FileHub.Presentation.Services;

public class FileService
{
    private const string CommonBucketName = "common-bucket";
    private const string TempBucketName = "temp-bucket";
    private readonly AmazonS3Client _s3Client;
    public readonly Dictionary<string, int> ProgressTrackingDict = new();

    public FileService(AmazonS3Client s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task<Result<GetObjectResponse>> GetFileByIdAsync(Guid ownerId, Guid groupId, Guid fileId) =>
        await GetFileAsync(ToObjectKey(ownerId, groupId, fileId));

    public async Task<Result<List<GetObjectResponse>>> GetGroupOfFilesByIdAsync(Guid ownerId, Guid groupId) =>
        await GetFilesByPrefix(ToGroupPrefix(ownerId, groupId));

    // public async Task<Result<List<string>>> UploadFilesAsync()
    // {
    // }

    public async Task<Result<string>> UploadFileAsync(Stream stream, string contentType, string fileName,
        Guid fileId, Guid groupId, Guid ownerId)
    {
        await ValidateBucket();
        // var strFileId = fileId.ToString();

        var request = new PutObjectRequest
        {
            Key = ToObjectKey(ownerId, groupId, fileId),
            InputStream = stream,
            AutoCloseStream = true,
            BucketName = CommonBucketName,
            ContentType = contentType,
            StreamTransferProgress = (_, args) =>
            {
                Console.WriteLine($"{groupId}| {fileId} | {args.PercentDone}");
                // ProgressTrackingDict[fileId] = args.PercentDone;
                // Console.WriteLine(fileId + ": " + args.PercentDone);
                // if (args.PercentDone == 100)
                //     ProgressTrackingDict.Remove(fileId);
            }
        };
        request.Metadata.Add("Owner-Id", ownerId.ToString());
        request.Metadata.Add("File-Id", fileId.ToString());
        request.Metadata.Add("Group-id", groupId.ToString());
        request.Metadata.Add("File-Name", fileName);

        var res = await _s3Client.PutObjectAsync(request);
        return res.HttpStatusCode == HttpStatusCode.OK
            ? Result.Ok(fileId.ToString())
            : Result.Fail<string>(
                new Error($"Failed to upload file: {fileName}").WithMetadata("StatusCode", res.HttpStatusCode));
    }

    public async Task<Result<List<GetObjectResponse>>> GetAllFiles(Guid ownerId) =>
        await GetFilesByPrefix(ownerId.ToString());

    public async Task<Result<string>> ShareFileAsync(Guid ownerId, Guid groupId, Guid fileId)
    {
        await ValidateBucket();
        var getResult = await GetFileByIdAsync(ownerId, groupId, fileId);

        if (getResult.IsFailed)
        {
            Console.WriteLine("FAILED");
            return Result.Fail<string>(getResult.Errors[0]);
        }

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
        return res.HttpStatusCode == HttpStatusCode.NoContent ? Result.Ok() : Result.Fail("Error when deleting file");
    }

    public async Task ZipFiles(Stream stream, List<GetObjectResponse> files)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var item in files)
        {
            var entry = archive.CreateEntry(Path.GetFileName(item.Metadata["File-Name"]),
                CompressionLevel.NoCompression);

            await using var entryStream = entry.Open();
            await item.ResponseStream.CopyToAsync(entryStream);
        }
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
        var taskList = keyList.Select(GetItemAsync).ToList();

        while (taskList.Any())
        {
            var finishedTask = await Task.WhenAny(taskList);
            taskList.Remove(finishedTask);
            await finishedTask;
        }

        return Result.Ok(items);

        async Task GetItemAsync(string key)
        {
            var item = await GetFileAsync(key);
            if (!item.IsSuccess)
                return;

            var res = item.Value;

            if (res is null) return;
            items.Add(res);
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
}