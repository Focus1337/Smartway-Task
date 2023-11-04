using System.Security.Claims;
using FileHub.Core.Models;
using FileHub.Presentation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace FileHub.Presentation.Services;

public class ApplicationUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationUserService(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<ApplicationUser>> GetUsers() =>
        await _userManager.Users.ToListAsync();

    public async Task<ApplicationUser?> GetCurrentUser()
    {
        var email = _httpContextAccessor.HttpContext?.User.FindFirstValue(OpenIddictConstants.Claims.Email);
        return email is not null ? await _userManager.FindByEmailAsync(email) : null;
    }
}