using System.Security.Claims;
using EmmzLive.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EmmzLive.Pages.Auth;

public sealed class VerifyModel(MagicLinkTokenService tokenService) : PageModel
{
    /// <summary>Set when the token is valid but expired — shown in the error view.</summary>
    public bool IsExpired { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return BadRequest();

        var result = tokenService.Validate(token);

        if (!result.IsValid)
        {
            // Tampered / malformed signature → 400.
            if (result.Failure == TokenValidationFailure.BadSignature ||
                result.Failure == TokenValidationFailure.WrongEmail)
                return BadRequest();

            // Valid signature but expired → error page (not 400, not a session).
            IsExpired = true;
            return Page();
        }

        // Valid token — create session cookie.
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, result.OwnerEmail!),
            new Claim(ClaimTypes.Email, result.OwnerEmail!),
        };
        var identity = new ClaimsIdentity(claims, "anon-inbox-session");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            "anon-inbox-session",
            principal,
            new AuthenticationProperties { IsPersistent = false });

        return Redirect("/");
    }
}
