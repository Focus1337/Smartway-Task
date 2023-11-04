using FileHub.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FileHub.Infrastructure.Data.Config;

public static class IdentityConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(b => b.ToTable("Users"));
        builder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("UserLogins"));
        builder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("UserTokens"));
        builder.Entity<IdentityRole>(b => b.ToTable("Roles"));
        builder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("RoleClaims"));
        builder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("UserRoles"));
    }
}