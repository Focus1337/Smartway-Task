﻿using Amazon.S3;
using FileHub.Core.Interfaces;
using FileHub.Core.Models;
using FileHub.Core.Services;
using FileHub.Infrastructure.Data;
using FileHub.Infrastructure.Options;
using FileHub.Infrastructure.Repositories;
using FileHub.Infrastructure.Services;
using FileHub.Presentation.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Polly;

namespace FileHub.Presentation;

public static class ProgramExtensions
{
    private const string AppName = "FileHub";

    public static void AddCustomControllers(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });
        builder.Services.AddControllers()
            .AddNewtonsoftJson();
    }

    public static void AddCustomApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IApplicationUserService, ApplicationUserService>();
        builder.Services.AddSingleton<IS3Service, S3Service>();
        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<IFileGroupRepository, FileGroupRepository>();
        builder.Services.AddScoped<IFileMetaRepository, FileMetaRepository>();
    }

    public static void AddCustomSwaggerGen(this WebApplicationBuilder builder) =>
        builder.Services.AddSwaggerGen(option =>
        {
            option.EnableAnnotations();
            option.SwaggerDoc("v1", new OpenApiInfo { Title = $"{AppName} API", Version = "v1" });
            option.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    Password = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri("/connect/token", UriKind.Relative)
                    }
                }
            });
            option.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

    public static void AddCustomDb(this WebApplicationBuilder builder)
    {
        builder.Services
            .Configure<DbOptions>(
                builder.Configuration.GetSection(DbOptions.DbConfiguration));

        var dbOptions = builder.Configuration.GetSection(DbOptions.DbConfiguration).Get<DbOptions>();
        if (dbOptions is null)
            throw new ArgumentException("Cannot register DbContext: DbOptions is null. Check appsettings.");

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(dbOptions.ConnectionString,
                action => action.MigrationsAssembly(typeof(AppDbContext).Assembly
                    .FullName));
            options.UseOpenIddict();
            options.EnableSensitiveDataLogging();
        });
    }

    public static void AddS3(this WebApplicationBuilder builder)
    {
        builder.Services
            .Configure<S3Options>(
                builder.Configuration.GetSection(S3Options.S3Configuration));

        var s3Options = builder.Configuration.GetSection(S3Options.S3Configuration).Get<S3Options>();
        if (s3Options is null)
            throw new ArgumentException("Cannot register Minio: MinioOptions is null. Check appsettings.");

        builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey,
            new AmazonS3Config { ServiceURL = s3Options.ServiceUrl, ForcePathStyle = true }));
    }

    public static void AddCustomAuthentication(this WebApplicationBuilder builder) =>
        builder.Services.AddAuthentication(options =>
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

    public static void AddCustomOpenIddict(this WebApplicationBuilder builder) =>
        builder.Services.AddOpenIddict()
            .AddCore(options =>
                options.UseEntityFrameworkCore()
                    .UseDbContext<AppDbContext>())
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/connect/token")
                    .SetAccessTokenLifetime(TimeSpan.FromHours(1))
                    .SetRefreshTokenLifetime(TimeSpan.FromDays(30));

                options.AllowPasswordFlow()
                    .AllowRefreshTokenFlow();

                options.AcceptAnonymousClients();

                options
                    .AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

    public static void AddCustomIdentity(this WebApplicationBuilder builder) =>
        builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                // password
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 1;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredUniqueChars = 0;
                options.Password.RequireNonAlphanumeric = false;
                // user
                options.User.RequireUniqueEmail = true;
                // claims identity
                options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
                options.ClaimsIdentity.EmailClaimType = OpenIddictConstants.Claims.Email;
                options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Username;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

    public static async Task MigrateDbContext(this WebApplication app) =>
        await Policy.Handle<Exception>()
            .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(10),
                onRetry: (exception, retryTime) =>
                    Console.WriteLine($"Error on migration apply: {exception.Message} | Retry in {retryTime}"))
            .ExecuteAsync(async () =>
            {
                await using var scope = app.Services.CreateAsyncScope();
                var context = scope.ServiceProvider.GetService<AppDbContext>();
                await context!.Database.MigrateAsync();
            });
}