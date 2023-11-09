namespace FileHub.Infrastructure.Options;

public class S3Options
{
    public const string S3Configuration = "MinioConfiguration";
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public required string ServiceUrl { get; set; }
}