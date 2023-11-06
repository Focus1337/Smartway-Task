using System.Collections.Concurrent;
using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace FileHub.Presentation.Services;

public class FileService
{
    private const string CommonBucketName = "common-bucket";
    private const string TempBucketName = "temp-bucket";

    private readonly AmazonS3Client _s3Client;

    // public event EventHandler<StreamTransferProgressArgs>? OnUploadProgress;
    public readonly Dictionary<string, int> ProgressTrackingDict = new();

    public FileService(AmazonS3Client s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task<GetObjectResponse?> GetByFileIdAsync(Guid ownerId, string fileId)
    {
        await ValidateBucket();
        try
        {
            var s3Object = await _s3Client.GetObjectAsync(CommonBucketName, GetObjectKey(ownerId.ToString(), fileId));
            return s3Object;
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return null;

            throw;
        }
    }

    public async Task<GetObjectResponse?> GetByKeyAsync(string key)
    {
        await ValidateBucket();
        try
        {
            var s3Object = await _s3Client.GetObjectAsync(CommonBucketName, key);
            return s3Object;
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return null;

            throw;
        }
    }

    public async Task<string> UploadFileAsync(Stream stream, string contentType, string fileName,
        string fileId, string ownerId)
    {
        await ValidateBucket();

        var request = new PutObjectRequest
        {
            Key = $"{ownerId}/{fileId}",
            InputStream = stream,
            AutoCloseStream = true,
            BucketName = CommonBucketName,
            ContentType = contentType,
            StreamTransferProgress = (_, args) =>
            {
                ProgressTrackingDict[fileId] = args.PercentDone;
                Console.WriteLine(fileId + ": " + args.PercentDone);
                if (args.PercentDone == 100)
                    ProgressTrackingDict.Remove(fileId);
            }
        };
        request.Metadata.Add("Owner-Id", ownerId);
        request.Metadata.Add("File-Id", fileId);
        request.Metadata.Add("File-Name", fileName);

        var putResponse = await _s3Client.PutObjectAsync(request);

        Console.WriteLine(putResponse.HttpStatusCode);

        Console.WriteLine($"File {request.Key} uploaded to S3 successfully!");
        return fileId;
    }

    public async Task UploadFileToTempBucketAsync(string filePath, string contentType, string fileName,
        string fileId, string ownerId)
    {
        await ValidateBucket();

        var request = new PutObjectRequest
        {
            Key = $"{ownerId}/{fileId}",
            AutoCloseStream = true,
            BucketName = CommonBucketName,
            ContentType = contentType,
            FilePath = filePath,
        };
        request.Metadata.Add("Owner-Id", ownerId);
        request.Metadata.Add("File-Id", fileId);
        request.Metadata.Add("File-Name", fileName);

        await _s3Client.PutObjectAsync(request);
        Console.WriteLine($"File {request.Key} uploaded to S3 successfully!");
    }

    public async Task<List<GetObjectResponse>> GetAllFiles(Guid ownerId)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = CommonBucketName,
            Prefix = ownerId.ToString()
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

        return items;

        async Task GetItemAsync(string key)
        {
            var item = await GetByKeyAsync(key);
            if (item is null) return;
            items.Add(item);
        }
    }

    public async Task<string?> ShareFileAsync(Guid ownerId, string fileId)
    {
        await ValidateBucket();
        var file = await GetByFileIdAsync(ownerId, fileId);

        if (file is null)
            return null;

        var urlRequest = new GetPreSignedUrlRequest
        {
            BucketName = CommonBucketName,
            Key = file.Key,
            Expires = DateTime.UtcNow.AddMinutes(1),
            Protocol = Protocol.HTTP,
            Verb = HttpVerb.GET
        };

        return _s3Client.GetPreSignedURL(urlRequest);
    }

    public async Task RemoveFileAsync(string fileId, string ownerId)
    {
        await ValidateBucket();
        var res = await _s3Client.DeleteObjectAsync(CommonBucketName, GetObjectKey(ownerId, fileId));
    }

    private async Task ValidateBucket()
    {
        if (!(await _s3Client.ListBucketsAsync()).Buckets.Exists(bucket => bucket.BucketName == CommonBucketName))
            await _s3Client.PutBucketAsync(CommonBucketName);
    }

    private static string GetObjectKey(string ownerId, string fileName) =>
        $"{ownerId}/{fileName}";
}