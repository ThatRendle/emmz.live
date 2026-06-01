using Resend;

namespace EmmzLive.Auth;

/// <summary>
/// Sends the magic-link email via the Resend API.
/// OWNER_EMAIL and MAIL_FROM are read from configuration at construction time.
/// Neither the token nor secrets are ever logged.
/// </summary>
public sealed class ResendMagicLinkEmailSender(
    IResend resend,
    IConfiguration configuration) : IMagicLinkEmailSender
{
    private readonly string _ownerEmail = configuration["OWNER_EMAIL"]
        ?? throw new InvalidOperationException("OWNER_EMAIL is not configured.");

    private readonly string _mailFrom = configuration["MAIL_FROM"]
        ?? throw new InvalidOperationException("MAIL_FROM is not configured.");

    public async Task SendAsync(string verifyUrl)
    {
        var message = new EmailMessage
        {
            From = _mailFrom,
            Subject = "Your emmz.live magic link",
            HtmlBody = $"""
                <p>Click the link below to sign in to your emmz.live inbox. The link expires in 15 minutes.</p>
                <p><a href="{System.Net.WebUtility.HtmlEncode(verifyUrl)}">Sign in to emmz.live</a></p>
                <p>If you did not request this link, you can ignore this email.</p>
                """,
        };
        message.To.Add(_ownerEmail);

        await resend.EmailSendAsync(message);
    }
}
