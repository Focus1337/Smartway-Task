using FileHub.Core.Models;
using FileHub.Infrastructure.Data;
using FileHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FileHubAPI.FileHub.Infrastructure.UnitTests;

public class FileMetaRepositoryTests
{
    private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDatabase")
        .Options;

    [Fact]
    public async Task GetFileMetaAsync_ReturnsFileMeta_WhenFileMetaExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        await using var dbContext = new AppDbContext(_options);
        var repository = new FileMetaRepository(dbContext);

        // Act
        dbContext.FileMetas.Add(new FileMeta
        {
            Id = fileId,
            UserId = userId,
            GroupId = groupId,
            FileName = "filename.txt",
            LastModified = DateTime.Now
        });
        await dbContext.SaveChangesAsync();
        var result = await repository.GetFileMetaAsync(userId, groupId, fileId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fileId, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(groupId, result.GroupId);
    }

    [Fact]
    public async Task GetFileMetaAsync_ReturnsNull_WhenFileMetaDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        await using var dbContext = new AppDbContext(_options);
        var repository = new FileMetaRepository(dbContext);

        // Act
        var result = await repository.GetFileMetaAsync(userId, groupId, fileId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetListOfFilesAsync_ReturnsListOfFiles_WhenFilesExistForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileId1 = Guid.NewGuid();
        var fileId2 = Guid.NewGuid();
        await using var dbContext = new AppDbContext(_options);
        var repository = new FileMetaRepository(dbContext);

        // Act
        dbContext.FileMetas.Add(new FileMeta
        {
            Id = fileId1,
            UserId = userId,
            GroupId = Guid.NewGuid(),
            FileName = "filename1.txt",
            LastModified = DateTime.Now
        });
        dbContext.FileMetas.Add(new FileMeta
        {
            Id = fileId2,
            UserId = userId,
            GroupId = Guid.NewGuid(),
            FileName = "filename2.txt",
            LastModified = DateTime.Now
        });
        await dbContext.SaveChangesAsync();
        var result = await repository.GetListOfFilesAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, fm => fm.Id == fileId1);
        Assert.Contains(result, fm => fm.Id == fileId2);
    }

    [Fact]
    public async Task GetListOfFilesAsync_ReturnsEmptyList_WhenNoFilesExistForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();

        await using var dbContext = new AppDbContext(_options);
        var repository = new FileMetaRepository(dbContext);

        // Act
        var result = await repository.GetListOfFilesAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}