using FileHub.Infrastructure.Data;

namespace FileHub.Infrastructure.Repositories;

public class FileRepository : EfRepository<FileHub.Core.Models.File, AppDbContext>
{
    public FileRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}