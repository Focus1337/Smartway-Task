using System.Security.Claims;
using FileHub.Core.Models;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;

namespace FileHub.Presentation.Services;

/// <summary>
/// Сервис для работы с текущим пользователем. Получает данные из Http контекста.
/// </summary>
public class ApplicationUserService : IApplicationUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationUserService(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ApplicationUser?> GetCurrentUser()
    {
        var email = _httpContextAccessor.HttpContext?.User.FindFirstValue(OpenIddictConstants.Claims.Email);
        return email is not null ? await _userManager.FindByEmailAsync(email) : null;
    }
}