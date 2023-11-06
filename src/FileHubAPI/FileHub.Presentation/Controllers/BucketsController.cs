using Amazon.S3;
using Microsoft.AspNetCore.Mvc;

namespace FileHub.Presentation.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BucketsController : ControllerBase
{
    private readonly AmazonS3Client _s3Client;

    public BucketsController(AmazonS3Client s3Client)
    {
        _s3Client = s3Client;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBucketAsync(string bucketName)
    {
        if ((await _s3Client.ListBucketsAsync()).Buckets.Exists(bucket => bucket.BucketName == bucketName))
            return BadRequest($"Bucket {bucketName} already exists.");

        await _s3Client.PutBucketAsync(bucketName);
        return Ok($"Bucket {bucketName} created.");
    }

    [HttpGet]
    public async Task<IActionResult> GetAllBucketAsync()
    {
        var data = await _s3Client.ListBucketsAsync();
        var buckets = data.Buckets.Select(b => b.BucketName);
        return Ok(buckets);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteBucketAsync(string bucketName)
    {
        await _s3Client.DeleteBucketAsync(bucketName);
        return NoContent();
    }
}