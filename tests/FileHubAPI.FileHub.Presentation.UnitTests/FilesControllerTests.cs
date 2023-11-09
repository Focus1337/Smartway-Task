using Amazon.S3.Model;
using FileHub.Core.Errors;
using FileHub.Core.Interfaces;
using FileHub.Core.Models;
using FileHub.Presentation.Controllers;
using FileHub.Presentation.Models;
using FileHub.Presentation.Services;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FileHubAPI.FileHub.Presentation.UnitTests;

public class FilesControllerTests
{
    private readonly Mock<IS3Service> _s3ServiceMock;
    private readonly Mock<IApplicationUserService> _userServiceMock;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly FilesController _controller;

    public FilesControllerTests()
    {
        _s3ServiceMock = new Mock<IS3Service>();

        _userServiceMock = new Mock<IApplicationUserService>();
        _fileServiceMock = new Mock<IFileService>();
        _controller = new FilesController(_userServiceMock.Object, _s3ServiceMock.Object, _fileServiceMock.Object);
    }

    [Fact]
    public async Task GetFileAsync_WithValidData_ReturnsFileMetaDto()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var user = new ApplicationUser { Id = Guid.NewGuid() };
        var fileMeta = new FileMeta
        {
            Id = Guid.NewGuid(), UserId = user.Id, GroupId = Guid.NewGuid(), FileName = "filename.txt",
            LastModified = DateTime.Now
        };
        var expectedResult = FileMetaDto.EntityToDto(fileMeta);

        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user);
        _fileServiceMock.Setup(x => x.GetFileMetaAsync(user.Id, groupId, fileId))
            .ReturnsAsync(Result.Ok(fileMeta));

        // Act
        var result = await _controller.GetFileAsync(groupId, fileId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResult = Assert.IsType<FileMetaDto>(okResult.Value);
        Assert.Equivalent(expectedResult, actualResult);
    }

    [Fact]
    public async Task GetFileAsync_Should_Return_BadRequest_When_User_Is_Not_Found()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _controller.GetFileAsync(groupId, fileId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorModels = Assert.IsType<List<ErrorModel>>(badRequestResult.Value);
        Assert.Single(errorModels);
        Assert.Equal("User not found", errorModels[0].Description);

        _userServiceMock.Verify(x => x.GetCurrentUser(), Times.Once);
        _fileServiceMock.Verify(x => x.GetFileMetaAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task GetFileAsync_Should_Return_NotFound_When_FileMeta_Is_Not_Found()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var user = new ApplicationUser { Id = Guid.NewGuid() };

        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user);

        _fileServiceMock.Setup(x => x.GetFileMetaAsync(user.Id, groupId, fileId))
            .ReturnsAsync(Result.Fail<FileMeta>(new FileNotFoundError()));

        // Act
        var result = await _controller.GetFileAsync(groupId, fileId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var errorModels = Assert.IsType<List<ErrorModel>>(notFoundResult.Value);
        Assert.Single(errorModels);
        Assert.Equal(nameof(FileNotFoundError), errorModels[0].Code);
    }

    [Fact]
    public async Task GetGroupOfFilesAsync_WithValidData_ReturnsListOfFileMetaDto()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var user = new ApplicationUser { Id = Guid.NewGuid() };
        var fileMetaList = new List<FileMeta>
        {
            new()
            {
                Id = Guid.NewGuid(), UserId = user.Id, GroupId = Guid.NewGuid(), FileName = "filename.txt",
                LastModified = DateTime.Now
            }
        };
        var expectedResult = fileMetaList.Select(FileMetaDto.EntityToDto).ToList();

        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user);
        _fileServiceMock.Setup(x => x.GetFileGroupAsync(user.Id, groupId))
            .ReturnsAsync(Result.Ok(fileMetaList));

        // Act
        var result = await _controller.GetGroupOfFilesAsync(groupId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResult = Assert.IsType<List<FileMetaDto>>(okResult.Value);
        Assert.Equivalent(expectedResult, actualResult);
    }

    [Fact]
    public async Task GetAllFilesAsync_WithValidData_ReturnsListOfFileMetaDto()
    {
        // Arrange
        var user = new ApplicationUser { Id = Guid.NewGuid() };
        var fileMetaList = new List<FileMeta>
        {
            new()
            {
                Id = Guid.NewGuid(), UserId = user.Id, GroupId = Guid.NewGuid(), FileName = "filename.txt",
                LastModified = DateTime.Now
            }
        };
        var expectedResult = fileMetaList.Select(FileMetaDto.EntityToDto).ToList();

        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user);
        _fileServiceMock.Setup(x => x.GetListOfFiles(user.Id))
            .ReturnsAsync(Result.Ok(fileMetaList));

        // Act
        var result = await _controller.GetAllFilesAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResult = Assert.IsType<List<FileMetaDto>>(okResult.Value);
        Assert.Equivalent(expectedResult, actualResult);
    }

    [Fact]
    public async Task GetAllGroupsAsync_ReturnsListOfFileGroups()
    {
        // Arrange
        var user = new ApplicationUser { Id = Guid.NewGuid() };
        var fileMetaList = new List<FileMeta>
        {
            new()
            {
                Id = Guid.NewGuid(), UserId = user.Id, GroupId = Guid.NewGuid(), FileName = "filename.txt",
                LastModified = DateTime.Now
            }
        };
        var groups = new List<FileGroup> { new(Guid.NewGuid(), user.Id) { FileMetas = fileMetaList } };
        _userServiceMock.Setup(mock => mock.GetCurrentUser()).ReturnsAsync(user);
        _fileServiceMock.Setup(mock => mock.GetListOfGroups(user.Id))
            .ReturnsAsync(Result.Ok(groups));

        // Act
        var result = await _controller.GetAllGroupsAsync();

        // Assert
        var okResult = result as OkObjectResult;
        var response = okResult?.Value as List<FileGroupDto>;
        Assert.NotNull(okResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.NotNull(response);
        Assert.Equal(groups.Count, response.Count);
    }

    [Fact]
    public async Task DownloadFileAsync_Should_Return_FileStreamResult_When_User_Is_Found_And_File_Is_Available()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var user = new ApplicationUser { Id = Guid.NewGuid() };

        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user);

        var fileStream = new MemoryStream();
        var contentType = "application/pdf";
        var objResponse = new GetObjectResponse { ResponseStream = fileStream, ContentLength = 10000 };
        objResponse.Headers.ContentType = contentType;
        objResponse.Headers.ContentLength = 10000;
        _s3ServiceMock.Setup(x => x.GetFileByIdAsync(user.Id, groupId, fileId))
            .ReturnsAsync(Result.Ok(objResponse));

        // Act
        var result = await _controller.DownloadFileAsync(groupId, fileId);

        // Assert
        var fileStreamResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(fileStream, fileStreamResult.FileStream);
        Assert.Equal(contentType, fileStreamResult.ContentType);

        _userServiceMock.Verify(x => x.GetCurrentUser(), Times.Once);
        _s3ServiceMock.Verify(x => x.GetFileByIdAsync(user.Id, groupId, fileId), Times.Once);
    }

    [Fact]
    public async Task DownloadFileAsync_Should_Return_BadRequest_When_User_Is_Not_Found()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _controller.DownloadFileAsync(groupId, fileId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorModels = Assert.IsType<List<ErrorModel>>(badRequestResult.Value);
        Assert.Single(errorModels);
        Assert.Equal("User not found", errorModels[0].Description);

        _userServiceMock.Verify(x => x.GetCurrentUser(), Times.Once);
        _s3ServiceMock.Verify(x => x.GetFileByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task DownloadFileAsync_Should_Return_NotFound_When_File_Is_Not_Available()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var user = new ApplicationUser { Id = Guid.NewGuid() };

        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user);

        _s3ServiceMock.Setup(x => x.GetFileByIdAsync(user.Id, groupId, fileId))
            .ReturnsAsync(Result.Fail<GetObjectResponse>(new FileNotFoundError()));

        // Act
        var result = await _controller.DownloadFileAsync(groupId, fileId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var errorModels = Assert.IsType<List<ErrorModel>>(notFoundResult.Value);
        Assert.Single(errorModels);
        Assert.Equal(nameof(FileNotFoundError), errorModels[0].Code);
    }

    [Fact]
    public async Task ShareFileAsync_ReturnsOkResult_WhenCurrentUserExists()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var user = new ApplicationUser { Id = Guid.NewGuid() };
        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user);
        _s3ServiceMock.Setup(x => x.ShareFileAsync(user.Id, groupId, fileId))
            .ReturnsAsync(Result.Ok(
                "http://localhost:9000/common-bucket/userid/groupid/fileid?AWSAccessKeyId=key&Expires=expiretime&Signature=signature"));

        // Act
        var result = await _controller.ShareFileAsync(groupId, fileId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        Assert.Equal(
            "http://localhost:9000/common-bucket/userid/groupid/fileid?AWSAccessKeyId=key&Expires=expiretime&Signature=signature",
            okResult.Value);
    }

    [Fact]
    public async Task ShareGroupOfFilesAsync_ReturnsOkResult_WhenCurrentUserExists()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var user = new ApplicationUser { Id = Guid.NewGuid() };
        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(user);
        _s3ServiceMock.Setup(x => x.ShareGroupAsync(user.Id, groupId))
            .ReturnsAsync(Result.Ok(
                "http://localhost:9000/common-bucket/userid/groupid/fileid?AWSAccessKeyId=key&Expires=expiretime&Signature=signature"));

        // Act
        var result = await _controller.ShareGroupOfFilesAsync(groupId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        Assert.Equal(
            "http://localhost:9000/common-bucket/userid/groupid/fileid?AWSAccessKeyId=key&Expires=expiretime&Signature=signature",
            okResult.Value);
    }

    [Fact]
    public async Task UploadFilesAsync_ValidData_ReturnsOkResult()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var files = new List<IFormFile>
        {
            // Add test files here
        };

        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(new ApplicationUser());
        _fileServiceMock.Setup(x => x.CreateFileGroupAsync(It.IsAny<FileGroup>())).Returns(Task.CompletedTask);
        _s3ServiceMock.Setup(x => x.UploadGroupAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<List<IFormFile>>(), It.IsAny<List<Guid>>()))
            .ReturnsAsync(Result.Ok(new UploadedFileGroup(groupId.ToString(), new List<string>())));

        // Act
        var result = await _controller.UploadFilesAsync(groupId, files);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.IsType<UploadedFileGroupDto>(okResult?.Value);
    }

    [Fact]
    public async Task GetFileProgressAsync_ValidData_ReturnsOkResult()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(new ApplicationUser());
        _s3ServiceMock.Setup(x => x.GetFileUploadProgress(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok(0));

        // Act
        var result = await _controller.GetFileProgressAsync(groupId, fileId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.IsType<int>(okResult?.Value);
        Assert.Equal(0, okResult.Value);
    }

    [Fact]
    public async Task GetGroupOfFilesProgressAsync_ValidData_ReturnsOkResult()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        _userServiceMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(new ApplicationUser());
        _fileServiceMock.Setup(x => x.GetFileGroupAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok(new List<FileMeta>()));
        _s3ServiceMock.Setup(x => x.GetGroupUploadProgress(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(Result.Ok(0));

        // Act
        var result = await _controller.GetGroupOfFilesProgressAsync(groupId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.IsType<int>(okResult?.Value);
        Assert.Equal(0, okResult.Value);
    }
}