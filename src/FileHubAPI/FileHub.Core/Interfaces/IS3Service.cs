using Amazon.S3.Model;
using FileHub.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Http;

namespace FileHub.Core.Interfaces;

public interface IS3Service
{
    Task<Result<GetObjectResponse>> GetFileByIdAsync(Guid ownerId, Guid groupId, Guid fileId);
    Task<Result<List<GetObjectResponse>>> GetGroupByIdAsync(Guid ownerId, Guid groupId);
    Task<Result<int>> GetFileUploadProgress(Guid ownerId, Guid groupId, Guid fileId);
    Task<Result<int>> GetGroupUploadProgress(Guid ownerId, Guid groupId);
    Task<Result<UploadedFileGroup>> UploadGroupAsync(Guid ownerId, Guid groupId, List<IFormFile> files, List<Guid> fileGuids);
    Task<Result<string>> ShareGroupAsync(Guid ownerId, Guid groupId);
    Task<Result<string>> ShareFileAsync(Guid ownerId, Guid groupId, Guid fileId);
    Task ZipFiles(Stream stream, List<GetObjectResponse> files, bool leaveOpen = false);
}