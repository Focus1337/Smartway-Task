using Amazon.S3;
using Amazon.S3.Model;
using FileHub.Core.Errors;
using FileHub.Infrastructure.Services;
using Moq;

namespace FileHubAPI.FileHub.Infrastructure.UnitTests;

public class S3ServiceTests
{
    private readonly Mock<IAmazonS3> _s3ClientMock;
    private readonly S3Service _s3Service;

    public S3ServiceTests()
    {
        _s3ClientMock = new Mock<IAmazonS3>();
        _s3Service = new S3Service(_s3ClientMock.Object);
    }

    [Fact]
    public async Task GetGroupByIdAsync_ReturnsFailedResult_WhenGroupDoesNotExist()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var listObjectsResponse = new ListObjectsV2Response
        {
            S3Objects = new List<S3Object>()
        };

        _s3ClientMock.Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(listObjectsResponse);

        // Act
        var result = await _s3Service.GetGroupByIdAsync(ownerId, groupId);

        // Assert
        Assert.True(result.IsFailed);
        Assert.IsType<GroupNotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task GetFileUploadProgress_ReturnsGroupAlreadyTransferredError_WhenGroupNotInProgress()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        // Act
        var result = await _s3Service.GetFileUploadProgress(ownerId, groupId, fileId);

        // Assert
        Assert.True(result.IsFailed);
        Assert.IsType<GroupAlreadyTransferredError>(result.Errors[0]);
    }

    [Fact]
    public async Task GetGroupUploadProgress_ReturnsGroupAlreadyTransferredError_WhenGroupNotInProgress()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        // Act
        var result = await _s3Service.GetGroupUploadProgress(ownerId, groupId);

        // Assert
        Assert.True(result.IsFailed);
        Assert.IsType<GroupAlreadyTransferredError>(result.Errors[0]);
    }
}