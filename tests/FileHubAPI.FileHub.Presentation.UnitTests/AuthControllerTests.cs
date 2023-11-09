using System.Security.Claims;
using FileHub.Core.Models;
using FileHub.Presentation.Controllers;
using FileHub.Presentation.Models;
using FileHubAPI.FileHub.Presentation.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FileHubAPI.FileHub.Presentation.UnitTests;

public class AuthControllerTests
{
    private readonly Mock<FakeUserManager> _mockUserManager;
    private readonly Mock<FakeSignInManager> _mockSignInManager;
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        _mockUserManager = new Mock<FakeUserManager>();
        _mockSignInManager = new Mock<FakeSignInManager>();
        _authController = new AuthController(_mockUserManager.Object, _mockSignInManager.Object);
    }

    [Theory]
    [InlineData("test@bk.ru", "dsfhsfjsf")]
    [InlineData("zxfadxiog345@yahoo.com", "FSDj~29304+_=-0")]
    public async Task Register_ValidModel_ReturnsCreated(string email, string password)
    {
        // Arrange
        var registerUserDto = new RegisterUserDto(email, password);

        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerUserDto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authController.Register(registerUserDto);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
    }

    [Theory]
    [InlineData("test@bk.ru", "dsfhsfjsf")]
    [InlineData("zxfadxiog345@yahoo.com", "FSDj~29304+_=-0")]
    public async Task Register_InvalidModel_ReturnsBadRequest(string email, string password)
    {
        // Arrange
        var registerUserDto = new RegisterUserDto(email, password);
        _authController.ModelState.AddModelError("Email", "The Email field is required.");

        // Act
        var result = await _authController.Register(registerUserDto);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Theory]
    [InlineData("test@bk.ru", "dsfhsfjsf")]
    [InlineData("zxfadxiog345@yahoo.com", "FSDj~29304+_=-0")]
    public async Task Register_CreateUserFailed_ReturnsBadRequest(string email, string password)
    {
        // Arrange
        var registerUserDto = new RegisterUserDto(email, password);

        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerUserDto.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Description = "Failed to create user."
            }));

        // Act
        var result = await _authController.Register(registerUserDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("test@bk.ru", "dsfhsfjsf")]
    [InlineData("zxfadxiog345@yahoo.com", "FSDj~29304+_=-0")]
    public async Task Register_AddClaimsFailed_ReturnsBadRequest(string email, string password)
    {
        // Arrange
        var registerUserDto = new RegisterUserDto(email, password);

        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerUserDto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Description = "Failed to add claims."
            }));

        // Act
        var result = await _authController.Register(registerUserDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Logout_ReturnsOkResult()
    {
        // Act
        var result = await _authController.Logout();

        // Assert
        Assert.IsType<OkResult>(result);
    }
}