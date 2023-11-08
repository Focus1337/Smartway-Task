using System.IO.Compression;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FileHub.Core.Errors;
using FileHub.Core.Helpers;
using FileHub.Core.Interfaces;
using FileHub.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Http;

namespace FileHub.Infrastructure.Services;

public class S3Service : IS3Service, IDisposable
{
    private const string CommonBucketName = "common-bucket";
    private readonly AmazonS3Client _s3Client;
    private readonly Dictionary<Guid, int> _fileProgressTrackingDict = new();
    private readonly Dictionary<string, int> _userGroupProgressTrackingDict = new();
    private readonly Dictionary<string, List<Guid>> _userGroupToFilesMap = new();

    public S3Service(AmazonS3Client s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task<Result<GetObjectResponse>> GetFileByIdAsync(Guid ownerId, Guid groupId, Guid fileId) =>
        await GetFileAsync(GetObjectKey(ownerId, groupId, fileId));

    public async Task<Result<List<GetObjectResponse>>> GetGroupByIdAsync(Guid ownerId, Guid groupId) =>
        await GetFilesByPrefix(GetGroupPrefix(ownerId, groupId));

    public Task<Result<int>> GetFileUploadProgress(Guid ownerId, Guid groupId, Guid fileId)
    {
        if (!_userGroupToFilesMap.TryGetValue(GetGroupPrefix(ownerId, groupId), out var filesInProgressList))
            return Task.FromResult(Result.Fail<int>(new GroupAlreadyTransferredError()));

        var searchingFileId = filesInProgressList.FirstOrDefault(id => id == fileId);
        if (!_fileProgressTrackingDict.TryGetValue(searchingFileId, out var progress))
            return Task.FromResult(Result.Fail<int>(new FileAlreadyTransferredError()));

        return Task.FromResult(Result.Ok(progress));
    }

    public Task<Result<int>> GetGroupUploadProgress(Guid ownerId, Guid groupId) =>
        Task.FromResult(
            !_userGroupProgressTrackingDict.TryGetValue(GetGroupPrefix(ownerId, groupId), out var progress)
                ? Result.Fail<int>(new GroupAlreadyTransferredError())
                : Result.Ok(progress));

    public async Task<Result<UploadedFileGroup>> UploadGroupAsync(Guid ownerId, Guid groupId, List<IFormFile> files,
        List<Guid> fileGuids)
    {
        _userGroupToFilesMap.Add(GetGroupPrefix(ownerId, groupId), new List<Guid>(fileGuids));

        var taskList = files.Select((file, index) =>
            MultipartUploadAsync(file.OpenReadStream(), file.ContentType,
                Base64Converter.EncodeToBase64(file.FileName), ownerId,
                groupId, fileGuids[index])).ToList();

        var uploadedFiles = new List<string>();
        while (taskList.Any())
        {
            var finishedTask = await Task.WhenAny(taskList);
            taskList.Remove(finishedTask);

            var task = await finishedTask;
            if (task.IsFailed)
                return Result.Fail<UploadedFileGroup>(task.Errors[0]);

            uploadedFiles.Add(task.Value);
        }

        return Result.Ok(new UploadedFileGroup(groupId.ToString(), uploadedFiles));
    }

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

        var uploadFileResult = await MultipartUploadAsync(zipStream, "application/zip", zipFileName,
            ownerId, gId, fId);

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

    public async Task ZipFiles(Stream stream, List<GetObjectResponse> files, bool leaveOpen = false)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen);
        foreach (var item in files)
        {
            var entry = archive.CreateEntry(
                Base64Converter.DecodeFromBase64(Path.GetFileName(item.Metadata["File-Name"])),
                CompressionLevel.NoCompression);

            await using var entryStream = entry.Open();
            await item.ResponseStream.CopyToAsync(entryStream);
        }
    }

    private async Task<Result<string>> MultipartUploadAsync(Stream stream, string contentType, string fileName,
        Guid ownerId, Guid groupId, Guid fileId)
    {
        await ValidateBucket();

        try
        {
            var userGroup = GetGroupPrefix(ownerId, groupId);
            var fileTransferUtility = new TransferUtility(_s3Client);
            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
            {
                BucketName = CommonBucketName,
                InputStream = stream,
                Key = GetObjectKey(ownerId, groupId, fileId),
                ContentType = contentType
            };
            fileTransferUtilityRequest.Metadata.Add("Owner-Id", ownerId.ToString());
            fileTransferUtilityRequest.Metadata.Add("File-Id", fileId.ToString());
            fileTransferUtilityRequest.Metadata.Add("Group-id", groupId.ToString());
            fileTransferUtilityRequest.Metadata.Add("File-Name", fileName);

            fileTransferUtilityRequest.UploadProgressEvent += (_, args) =>
            {
                _fileProgressTrackingDict[fileId] = args.PercentDone;

                var fileIds = _userGroupToFilesMap[userGroup];
                var sum = fileIds.Sum(fid => _fileProgressTrackingDict[fid]);

                _userGroupProgressTrackingDict[userGroup] = sum / fileIds.Count;

                if (_userGroupProgressTrackingDict[userGroup] == 100)
                {
                    _userGroupToFilesMap.Remove(userGroup);
                    _userGroupProgressTrackingDict.Remove(userGroup);
                    fileIds.ForEach(f => _fileProgressTrackingDict.Remove(f));
                }
            };

            await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
        }
        catch (AmazonS3Exception)
        {
            Result.Fail<string>(new FailedToUploadError(fileName));
        }

        return Result.Ok(fileId.ToString());
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

        var fileList = new List<GetObjectResponse>();
        var taskList = keyList.Select(GetItemAsync).ToList();
        while (taskList.Any())
        {
            var finishedTask = await Task.WhenAny(taskList);
            taskList.Remove(finishedTask);
            var task = await finishedTask;
            if (task.IsFailed)
                return Result.Fail<List<GetObjectResponse>>(task.Errors[0]);
        }

        return fileList.Count == 0
            ? Result.Fail<List<GetObjectResponse>>(new GroupNotFoundError())
            : Result.Ok(fileList);

        async Task<Result> GetItemAsync(string key)
        {
            var res = await GetFileAsync(key);
            if (res.IsFailed)
                return Result.Fail(res.Errors[0]);

            var file = res.Value;
            fileList.Add(file);
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

    private static string GetObjectKey(Guid ownerId, Guid groupId, Guid fileId) =>
        $"{GetGroupPrefix(ownerId, groupId)}/{fileId}";

    private static string GetGroupPrefix(Guid ownerId, Guid groupId) =>
        $"{ownerId}/{groupId}";

    public void Dispose()
    {
        _s3Client.Dispose();
    }
}