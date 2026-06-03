using System.Security.Cryptography;
using System.Text;

namespace EmmzLive.Auth;

/// <summary>
/// Distinguishes why a token failed validation.
/// </summary>
public enum TokenValidationFailure
{
    /// <summary>Token is structurally invalid or its HMAC does not match.</summary>
    BadSignature,
    /// <summary>Token was validly signed but is past its 15-minute window.</summary>
    Expired,
    /// <summary>Token was validly signed and unexpired but the embedded email
    /// does not match the configured owner email.</summary>
    WrongEmail,
}

/// <summary>
/// Represents the result of a token validation attempt.
/// </summary>
public sealed class TokenValidationResult
{
    private TokenValidationResult() { }

    public bool IsValid => OwnerEmail is not null;
    public string? OwnerEmail { get; private init; }
    public TokenValidationFailure? Failure { get; private init; }

    internal static TokenValidationResult Valid(string email) =>
        new() { OwnerEmail = email };

    internal static TokenValidationResult Failed(TokenValidationFailure reason) =>
        new() { Failure = reason };
}

/// <summary>
/// Creates and validates HMAC-SHA256 magic-link tokens.
/// Payload format: "{email}:{unixExpirySeconds}" base64url-encoded,
/// appended with "." and the base64url-encoded HMAC-SHA256 signature over the payload bytes.
/// Signature verification uses <see cref="CryptographicOperations.FixedTimeEquals"/>
/// to prevent timing attacks.
/// </summary>
public sealed class MagicLinkTokenService
{
    private const int ExpiryMinutes = 15;
    private readonly byte[] _keyBytes;
    private readonly string _ownerEmail;

    public MagicLinkTokenService(string sessionSecret, string ownerEmail)
    {
        if (string.IsNullOrWhiteSpace(sessionSecret))
            throw new ArgumentException("SESSION_SECRET must not be empty.", nameof(sessionSecret));
        if (string.IsNullOrWhiteSpace(ownerEmail))
            throw new ArgumentException("OWNER_EMAIL must not be empty.", nameof(ownerEmail));

        _keyBytes = Encoding.UTF8.GetBytes(sessionSecret);
        _ownerEmail = ownerEmail;
    }

    /// <summary>
    /// Creates a URL-safe token for <paramref name="email"/> that is valid for 15 minutes.
    /// </summary>
    public string Create(string email)
    {
        var expiry = DateTimeOffset.UtcNow.AddMinutes(ExpiryMinutes).ToUnixTimeSeconds();
        var payload = $"{email}:{expiry}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var payloadEncoded = Base64UrlEncode(payloadBytes);

        var sig = Sign(Encoding.UTF8.GetBytes(payloadEncoded));
        return $"{payloadEncoded}.{Base64UrlEncode(sig)}";
    }

    /// <summary>
    /// Validates a token. Returns a <see cref="TokenValidationResult"/> indicating success or
    /// the specific failure reason.
    /// </summary>
    public TokenValidationResult Validate(string token)
    {
        var dot = token.IndexOf('.', StringComparison.Ordinal);
        if (dot < 0)
            return TokenValidationResult.Failed(TokenValidationFailure.BadSignature);

        var payloadEncoded = token[..dot];
        var sigEncoded = token[(dot + 1)..];

        // Verify signature — constant-time compare.
        byte[] providedSig;
        try
        {
            providedSig = Base64UrlDecode(sigEncoded);
        }
        catch (FormatException)
        {
            return TokenValidationResult.Failed(TokenValidationFailure.BadSignature);
        }

        var expectedSig = Sign(Encoding.UTF8.GetBytes(payloadEncoded));
        if (!CryptographicOperations.FixedTimeEquals(expectedSig, providedSig))
            return TokenValidationResult.Failed(TokenValidationFailure.BadSignature);

        // Decode payload.
        string payload;
        try
        {
            payload = Encoding.UTF8.GetString(Base64UrlDecode(payloadEncoded));
        }
        catch (FormatException)
        {
            return TokenValidationResult.Failed(TokenValidationFailure.BadSignature);
        }

        var sep = payload.LastIndexOf(':');
        if (sep < 0 || !long.TryParse(payload[(sep + 1)..], out var expiry))
            return TokenValidationResult.Failed(TokenValidationFailure.BadSignature);

        var email = payload[..sep];

        // Check expiry before email — expiry is an objective fact; email mismatch is policy.
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
            return TokenValidationResult.Failed(TokenValidationFailure.Expired);

        if (!string.Equals(email, _ownerEmail, StringComparison.OrdinalIgnoreCase))
            return TokenValidationResult.Failed(TokenValidationFailure.WrongEmail);

        return TokenValidationResult.Valid(email);
    }

    private byte[] Sign(byte[] data)
    {
        using var hmac = new HMACSHA256(_keyBytes);
        return hmac.ComputeHash(data);
    }

    internal static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    internal static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded,
        };
        return Convert.FromBase64String(padded);
    }
}
