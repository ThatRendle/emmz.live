namespace EmmzLive.Auth;

/// <summary>
/// Sends a magic-link verification email to the inbox owner.
/// The implementation is abstracted so tests can substitute a mock without network calls.
/// </summary>
public interface IMagicLinkEmailSender
{
    /// <summary>
    /// Sends the magic-link email containing <paramref name="verifyUrl"/> to the owner.
    /// </summary>
    Task SendAsync(string verifyUrl);
}
