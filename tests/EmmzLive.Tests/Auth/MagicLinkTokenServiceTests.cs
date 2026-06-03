using EmmzLive.Auth;

namespace EmmzLive.Tests.Auth;

public sealed class MagicLinkTokenServiceTests
{
    private const string Secret = "test-secret-key-at-least-32-chars-long-ok";
    private const string OwnerEmail = "owner@example.com";

    private static MagicLinkTokenService CreateService(string? secret = null, string? email = null) =>
        new(secret ?? Secret, email ?? OwnerEmail);

    // --- Round-trip ---

    [Fact]
    public void Create_ThenValidate_ReturnsValidWithOwnerEmail()
    {
        var svc = CreateService();
        var token = svc.Create(OwnerEmail);
        var result = svc.Validate(token);

        Assert.True(result.IsValid);
        Assert.Equal(OwnerEmail, result.OwnerEmail);
        Assert.Null(result.Failure);
    }

    // --- Expired token ---

    [Fact]
    public void Validate_ExpiredToken_ReturnsExpired()
    {
        // Build a token whose expiry is 1 second in the past.
        var svc = CreateService();
        var pastExpiry = DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds();
        var payload = $"{OwnerEmail}:{pastExpiry}";
        var payloadEncoded = MagicLinkTokenService.Base64UrlEncode(
            System.Text.Encoding.UTF8.GetBytes(payload));

        // Sign it properly so the signature check passes.
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(Secret);
        using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
        var sig = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payloadEncoded));
        var sigEncoded = MagicLinkTokenService.Base64UrlEncode(sig);

        var token = $"{payloadEncoded}.{sigEncoded}";
        var result = svc.Validate(token);

        Assert.False(result.IsValid);
        Assert.Equal(TokenValidationFailure.Expired, result.Failure);
    }

    // --- Tampered token → BadSignature ---

    [Fact]
    public void Validate_TamperedPayload_ReturnsBadSignature()
    {
        var svc = CreateService();
        var token = svc.Create(OwnerEmail);

        // Flip the first char of the payload section.
        var dot = token.IndexOf('.', StringComparison.Ordinal);
        var chars = token.ToCharArray();
        chars[0] = chars[0] == 'A' ? 'B' : 'A';
        var tampered = new string(chars);

        var result = svc.Validate(tampered);

        Assert.False(result.IsValid);
        Assert.Equal(TokenValidationFailure.BadSignature, result.Failure);
    }

    [Fact]
    public void Validate_TamperedSignature_ReturnsBadSignature()
    {
        var svc = CreateService();
        var token = svc.Create(OwnerEmail);

        var dot = token.IndexOf('.', StringComparison.Ordinal);
        var chars = token.ToCharArray();
        // Flip a char in the signature section.
        chars[dot + 1] = chars[dot + 1] == 'A' ? 'B' : 'A';
        var tampered = new string(chars);

        var result = svc.Validate(tampered);

        Assert.False(result.IsValid);
        Assert.Equal(TokenValidationFailure.BadSignature, result.Failure);
    }

    [Fact]
    public void Validate_MissingDot_ReturnsBadSignature()
    {
        var svc = CreateService();
        var result = svc.Validate("nodotinhere");

        Assert.False(result.IsValid);
        Assert.Equal(TokenValidationFailure.BadSignature, result.Failure);
    }

    // --- Non-owner email → WrongEmail ---

    [Fact]
    public void Validate_TokenForDifferentEmail_ReturnsWrongEmail()
    {
        // Service configured for owner@example.com but token signed for other@example.com.
        var signerSvc = CreateService(email: "other@example.com");
        var token = signerSvc.Create("other@example.com");

        // Validate with a service that expects owner@example.com.
        var validatorSvc = CreateService(email: OwnerEmail);
        var result = validatorSvc.Validate(token);

        Assert.False(result.IsValid);
        Assert.Equal(TokenValidationFailure.WrongEmail, result.Failure);
    }

    // --- Constant-time compare is used (tampered tokens are always rejected) ---

    [Fact]
    public void Validate_AllTamperedTokensRejected_ConstantTimeEnforced()
    {
        var svc = CreateService();
        var token = svc.Create(OwnerEmail);

        // Systematically alter each character of the signature section.
        var dot = token.IndexOf('.', StringComparison.Ordinal);
        for (var i = dot + 1; i < token.Length; i++)
        {
            var chars = token.ToCharArray();
            chars[i] = chars[i] == 'A' ? 'B' : 'A';
            var tampered = new string(chars);

            var result = svc.Validate(tampered);
            Assert.False(result.IsValid,
                $"Expected tampered token at position {i} to be rejected.");
        }
    }

    // --- Constructor guards ---

    [Fact]
    public void Constructor_EmptySecret_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MagicLinkTokenService(string.Empty, OwnerEmail));
    }

    [Fact]
    public void Constructor_EmptyOwnerEmail_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MagicLinkTokenService(Secret, string.Empty));
    }
}
