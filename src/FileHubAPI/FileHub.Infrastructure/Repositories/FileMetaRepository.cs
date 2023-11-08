using FileHub.Core.Interfaces;
using FileHub.Infrastructure.Data;

namespace FileHub.Infrastructure.Repositories;

public class FileMetaRepository : EfRepository<FileHub.Core.Models.FileMeta, AppDbContext>, IFileMetaRepository
{
    public FileMetaRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}