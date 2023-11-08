using FileHub.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileHub.Infrastructure.Data.Config;

public class FileGroupConfiguration : IEntityTypeConfiguration<FileGroup>
{
    public void Configure(EntityTypeBuilder<FileGroup> builder)
    {
        builder
            .HasKey(fg => fg.Id);

        builder
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(fg => fg.UserId);
    }
}