using EmmzLive.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EmmzLive.Pages.Auth;

public sealed class RequestModel(
    MagicLinkTokenService tokenService,
    IConfiguration configuration,
    ILogger<RequestModel> logger) : PageModel
{
    private readonly string _ownerEmail = configuration["OWNER_EMAIL"]
        ?? throw new InvalidOperationException("OWNER_EMAIL is not configured.");

    public bool EmailSent { get; private set; }
    public bool SendFailed { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(
        [FromServices] IMagicLinkEmailSender emailSender)
    {
        var verifyUrl = BuildVerifyUrl();
        try
        {
            await emailSender.SendAsync(verifyUrl);
            EmailSent = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send magic-link email.");
            SendFailed = true;
        }
        return Page();
    }

    /// <summary>
    /// Builds the absolute verify URL using the forwarded-header-corrected scheme and host.
    /// </summary>
    internal string BuildVerifyUrl()
    {
        var token = tokenService.Create(_ownerEmail);
        return $"{Request.Scheme}://{Request.Host}/auth/verify?token={Uri.EscapeDataString(token)}";
    }
}
