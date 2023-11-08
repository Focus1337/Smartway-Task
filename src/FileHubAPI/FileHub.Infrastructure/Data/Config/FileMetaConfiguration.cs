using FileHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileHub.Infrastructure.Data.Config;

public class FileMetaConfiguration : IEntityTypeConfiguration<FileMeta>
{
    public void Configure(EntityTypeBuilder<FileMeta> builder)
    {
        builder
            .HasKey(e => e.Id);

        builder
            .HasKey(fm => fm.Id);

        builder
            .HasOne<FileGroup>()
            .WithMany()
            .HasForeignKey(fm => fm.GroupId);

        builder
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(fm => fm.UserId);
    }
}