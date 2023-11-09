using FileHub.Core.Models;

namespace FileHub.Presentation.Services;

public interface IApplicationUserService
{
    Task<ApplicationUser?> GetCurrentUser();
}