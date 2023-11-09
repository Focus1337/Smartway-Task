using FileHub.Core.Models;
using FileHub.Infrastructure.Data;
using FileHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FileHubAPI.FileHub.Infrastructure.UnitTests;

public class FileGroupRepositoryTests
{
    private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDatabase")
        .Options;

    [Fact]
    public async Task GetListOfGroupsAsync_ReturnsListOfGroups()
    {
        // Arrange
        await using var dbContext = new AppDbContext(_options);
        var repository = new FileGroupRepository(dbContext);
        var userId = Guid.NewGuid();
        var fileGroup1 = new FileGroup(Guid.NewGuid(), userId) { FileMetas = new List<FileMeta>() };
        var fileGroup2 = new FileGroup(Guid.NewGuid(), userId) { FileMetas = new List<FileMeta>() };

        // Act
        dbContext.FileGroups.AddRange(fileGroup1, fileGroup2);
        await dbContext.SaveChangesAsync();
        var result = await repository.GetListOfGroupsAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(fileGroup1, result);
        Assert.Contains(fileGroup2, result);
    }

    [Fact]
    public async Task GetFileGroupAsync_ReturnsFileGroup()
    {
        // Arrange
        await using var dbContext = new AppDbContext(_options);
        var repository = new FileGroupRepository(dbContext);
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var fileGroup = new FileGroup(groupId, userId) { FileMetas = new List<FileMeta>() };

        // Act
        dbContext.FileGroups.Add(fileGroup);
        await dbContext.SaveChangesAsync();
        var result = await repository.GetFileGroupAsync(userId, groupId);

        // Assert
        Assert.Equal(fileGroup, result);
    }

    [Fact]
    public async Task CreateFileGroupAsync_AddsFileGroupToDatabase()
    {
        // Arrange
        await using var dbContext = new AppDbContext(_options);
        var repository = new FileGroupRepository(dbContext);
        var fileGroup = new FileGroup(Guid.NewGuid(), Guid.NewGuid()) { FileMetas = new List<FileMeta>() };

        // Act
        await repository.CreateFileGroupAsync(fileGroup);
        await dbContext.SaveChangesAsync();

        // Assert
        var result = await dbContext.FileGroups.FindAsync(fileGroup.Id);
        Assert.Equal(fileGroup, result);
    }
}