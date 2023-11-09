using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;

namespace FileHub.Presentation.Attributes;

public class OpenIddictAuthorizeAttribute : AuthorizeAttribute
{
    public OpenIddictAuthorizeAttribute() =>
        AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
}