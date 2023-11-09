using FileHub.Core.Errors;
using FileHub.Core.Interfaces;
using FileHub.Core.Models;
using FileHub.Core.Services;
using Moq;

namespace FileHubAPI.UnitTests;

public class FileServiceTests
{
    private readonly Mock<IFileGroupRepository> _groupRepositoryMock;
    private readonly Mock<IFileMetaRepository> _metaRepositoryMock;
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        _groupRepositoryMock = new Mock<IFileGroupRepository>();
        _metaRepositoryMock = new Mock<IFileMetaRepository>();
        _fileService = new FileService(_groupRepositoryMock.Object, _metaRepositoryMock.Object);
    }

    [Fact]
    public async Task GetFileMetaAsync_WhenFileMetaExists_ReturnsOkResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var fileMeta = new FileMeta
            { UserId = userId, GroupId = groupId, Id = fileId, FileName = "filename.txt", LastModified = DateTime.Now };
        _metaRepositoryMock.Setup(x => x.GetFileMetaAsync(userId, groupId, fileId))
            .ReturnsAsync(fileMeta);

        // Act
        var result = await _fileService.GetFileMetaAsync(userId, groupId, fileId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(fileMeta, result.Value);
    }

    [Fact]
    public async Task GetFileMetaAsync_WhenFileMetaDoesNotExist_ReturnsFailResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        _metaRepositoryMock.Setup(x => x.GetFileMetaAsync(userId, groupId, fileId))
            .ReturnsAsync((FileMeta)null!);

        // Act
        var result = await _fileService.GetFileMetaAsync(userId, groupId, fileId);
        // Assert
        Assert.True(result.IsFailed);
        Assert.IsType<FileNotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task GetFileGroupAsync_WhenFileGroupExists_ReturnsOkResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var fileMetas = new List<FileMeta>
        {
            new()
            {
                UserId = userId, GroupId = groupId, Id = Guid.NewGuid(), FileName = "filename.txt",
                LastModified = DateTime.Now
            }
        };
        var fileGroup = new FileGroup(groupId, userId) { FileMetas = fileMetas };
        fileGroup.FileMetas = fileMetas;
        _groupRepositoryMock.Setup(x => x.GetFileGroupAsync(userId, groupId))
            .ReturnsAsync(fileGroup);

        // Act
        var result = await _fileService.GetFileGroupAsync(userId, groupId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(fileMetas, result.Value);
    }

    [Fact]
    public async Task GetFileGroupAsync_WhenFileGroupDoesNotExist_ReturnsFailResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        _groupRepositoryMock.Setup(x => x.GetFileGroupAsync(userId, groupId))
            .ReturnsAsync((FileGroup)null!);

        // Act
        var result = await _fileService.GetFileGroupAsync(userId, groupId);

        // Assert
        Assert.True(result.IsFailed);
        Assert.IsType<GroupNotFoundError>(result.Errors[0]);
    }

    [Fact]
    public async Task GetListOfFiles_WhenFilesExist_ReturnsOkResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileMetas = new List<FileMeta>
        {
            new()
            {
                UserId = userId, GroupId = Guid.NewGuid(), Id = Guid.NewGuid(), FileName = "filename.txt",
                LastModified = DateTime.Now
            }
        };
        _metaRepositoryMock.Setup(x => x.GetListOfFilesAsync(userId))
            .ReturnsAsync(fileMetas);

        // Act
        var result = await _fileService.GetListOfFiles(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(fileMetas, result.Value);
    }

    [Fact]
    public async Task GetListOfGroupsAsync_WhenFileGroupsExist_ReturnsOkResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileMetas = new List<FileMeta>
        {
            new()
            {
                UserId = userId, GroupId = Guid.NewGuid(), Id = Guid.NewGuid(), FileName = "filename.txt",
                LastModified = DateTime.Now
            }
        };

        var fileGroups = new List<FileGroup>
        {
            new(Guid.NewGuid(), userId) { FileMetas = fileMetas }
        };

        _groupRepositoryMock.Setup(x => x.GetListOfGroupsAsync(userId)).ReturnsAsync(fileGroups);

        // Act
        var result = await _fileService.GetListOfGroups(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(fileGroups, result.Value);
    }

    [Fact]
    public async Task CreateFileGroupAsync_WhenCalledWithValidFileGroup_CreatesFileGroupSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileMetas = new List<FileMeta>
        {
            new()
            {
                UserId = userId, GroupId = Guid.NewGuid(), Id = Guid.NewGuid(), FileName = "filename.txt",
                LastModified = DateTime.Now
            }
        };
        var fileGroup = new FileGroup(Guid.NewGuid(), userId) { FileMetas = fileMetas };
        _groupRepositoryMock.Setup(x => x.CreateFileGroupAsync(fileGroup)).Returns(Task.CompletedTask);

        // Act
        await _fileService.CreateFileGroupAsync(fileGroup);

        // Assert
        _groupRepositoryMock.Verify(r => r.CreateFileGroupAsync(fileGroup), Times.Once);
    }
}