using System.Security.Claims;
using FileHub.Core.Models;
using FileHub.Presentation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using OpenIddict.Abstractions;

namespace FileHubAPI.FileHub.Presentation.UnitTests;

public class ApplicationUserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly ApplicationUserService _userService;

    public ApplicationUserServiceTests()
    {
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null!, null!,
            null!, null!, null!, null!, null!, null!);
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _userService = new ApplicationUserService(_mockUserManager.Object, _mockHttpContextAccessor.Object);
    }

    [Fact]
    public async Task GetCurrentUser_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext)null!);

        // Act
        var result = await _userService.GetCurrentUser();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentUser_WhenEmailIsNull_ReturnsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        // Act
        var result = await _userService.GetCurrentUser();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentUser_WhenEmailIsNotNull_ReturnsUser()
    {
        // Arrange
        var email = "test@example.com";
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(OpenIddictConstants.Claims.Email, email)
        }));
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        _mockUserManager.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(new ApplicationUser());

        // Act
        var result = await _userService.GetCurrentUser();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ApplicationUser>(result);
    }
}