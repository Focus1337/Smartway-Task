using FileHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileHub.Infrastructure.Data.Config;

public class FileMetaConfiguration : IEntityTypeConfiguration<FileMeta>
{
    public void Configure(EntityTypeBuilder<FileMeta> builder)
    {
        builder
            .HasKey(fm => fm.Id);

        builder
            .HasOne<FileGroup>()
            .WithMany(fm => fm.FileMetas)
            .HasForeignKey(fm => fm.GroupId);

        builder
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(fm => fm.UserId);
    }
}