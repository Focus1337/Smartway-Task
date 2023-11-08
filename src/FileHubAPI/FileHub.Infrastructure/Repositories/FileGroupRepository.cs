using FileHub.Core.Interfaces;
using FileHub.Core.Models;
using FileHub.Infrastructure.Data;

namespace FileHub.Infrastructure.Repositories;

public class FileGroupRepository : EfRepository<FileGroup, AppDbContext>, IFileGroupRepository
{
    public FileGroupRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}