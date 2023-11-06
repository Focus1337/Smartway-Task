using System.Collections.Immutable;
using System.Net.Mime;
using System.Security.Claims;
using FileHub.Core.Models;
using FileHub.Presentation.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Swashbuckle.AspNetCore.Annotations;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace FileHub.Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(List<IdentityError>))]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserDto registerUserDto)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var user = new ApplicationUser { Email = registerUserDto.Email, UserName = registerUserDto.Email };

        string NormalizedErrorCodes(IdentityResult result) =>
            string.Join("; ", result.Errors.Select(e => e.Code));

        string NormalizedErrorDescriptions(IdentityResult result) =>
            string.Join("; ", result.Errors.Select(e => e.Description));

        var createResult = await _userManager.CreateAsync(user, registerUserDto.Password);
        if (!createResult.Succeeded)
            return BadRequest();
        // return new RegisterResponse
        // {
        //     ErrorResponse = new ErrorResponse
        //     {
        //         Error = NormalizedErrorCodes(result),
        //         ErrorDescription = NormalizedErrorDescriptions(result)
        //     }
        // };

        var claimsResult = await _userManager.AddClaimsAsync(user,
            new[]
            {
                new Claim(ClaimTypes.Email, registerUserDto.Email),
                new Claim(ClaimTypes.Name, registerUserDto.Email)
            });

        if (!claimsResult.Succeeded)
            return BadRequest();
        // return new RegisterResponse
        // {
        //     ErrorResponse = new ErrorResponse
        //     {
        //         Error = NormalizedErrorCodes(result),
        //         ErrorDescription = NormalizedErrorDescriptions(result)
        //     }
        // };

        return StatusCode(StatusCodes.Status201Created, user.Id);
    }

    [Consumes("application/x-www-form-urlencoded")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok();
    }

    [Consumes("application/x-www-form-urlencoded")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(OpenIddictResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(OpenIddictResponse))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OpenIddictResponse))]
    [SwaggerOperation(
        Summary = "Access Token generation operation",
        Description = "Content-Type: application/x-www-form-urlencoded | Body: username, password, grant_type",
        OperationId = "Exchange"
    )]
    [HttpPost("~/connect/token"), IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
        ApplicationUser? user;

        if (request.IsPasswordGrantType())
        {
            if (request.Username is null || request.Password is null)
                return BadRequest(new OpenIddictResponse
                {
                    Error = Errors.InvalidRequest,
                    ErrorDescription = "Username/password is null."
                });

            user = await _userManager.FindByNameAsync(request.Username);
            if (user is null)
                return BadRequest(new OpenIddictResponse
                {
                    Error = Errors.InvalidGrant,
                    ErrorDescription = "The username/password couple is invalid."
                });

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
            if (!result.Succeeded)
                return BadRequest(new OpenIddictResponse
                {
                    Error = Errors.InvalidGrant,
                    ErrorDescription = "The username/password couple is invalid."
                });

            // Create the claims-based identity that will be used by OpenIddict to generate tokens.
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            // Add the claims that will be persisted in the tokens.
            await SetClaimsIdentity(identity, user);

            // Note: in this sample, the granted scopes match the requested scope
            // but you may want to allow the user to uncheck specific scopes.
            // For that, simply restrict the list of scopes before calling SetScopes.
            // identity.SetScopes(request.GetScopes());
            identity.SetScopes(new[]
            {
                Scopes.OpenId,
                Scopes.Email,
                Scopes.Profile,
                Scopes.Roles,
                Scopes.OfflineAccess
            }.Intersect(request.GetScopes()));

            identity.SetDestinations(GetDestinations);

            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            var result =
                await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme) ??
                throw new InvalidOperationException("AuthenticateResult is null");

            user = await _userManager.FindByIdAsync(result.Principal?.GetClaim(Claims.Subject) ??
                                                    throw new InvalidOperationException("Invalid claims"));

            if (user is null)
                return Forbid(new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The refresh token is no longer valid."
                }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (!await _signInManager.CanSignInAsync(user))
                return Forbid(new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The user is no longer allowed to sign in."
                }!), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var identity = new ClaimsIdentity(result.Principal.Claims,
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            // Override the user claims present in the principal in case they changed since the refresh token was issued.
            await SetClaimsIdentity(identity, user);

            identity.SetDestinations(GetDestinations);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }

    private async Task SetClaimsIdentity(ClaimsIdentity identity, ApplicationUser user) =>
        identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
            .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
            .SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user))
            .SetClaims(Claims.Role, (await _userManager.GetRolesAsync(user)).ToImmutableArray());

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        if (claim.Subject is null)
            throw new InvalidOperationException($"Subject of the claim {claim} is null");

        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;

                if (claim.Subject.HasScope(Permissions.Scopes.Profile))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;

                if (claim.Subject.HasScope(Permissions.Scopes.Email))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;

                if (claim.Subject.HasScope(Permissions.Scopes.Roles))
                    yield return Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}