using EmmzLive.Auth;

namespace EmmzLive.Tests.Auth;

/// <summary>
/// Tests for the HTML body builder in ResendMagicLinkEmailSender.
/// The body-building logic is extracted to an internal static method so tests do not
/// need to implement the large IResend interface.
/// </summary>
public sealed class ResendMagicLinkEmailSenderTests
{
    private const string VerifyUrl = "https://emmz.live/auth/verify?token=abc123";

    [Fact]
    public void BuildHtmlBody_WithOwnerName_IncludesGreeting()
    {
        var body = ResendMagicLinkEmailSender.BuildHtmlBody("Alice", VerifyUrl);

        Assert.Contains("Hi Alice,", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildHtmlBody_WithoutOwnerName_OmitsGreeting()
    {
        var body = ResendMagicLinkEmailSender.BuildHtmlBody(ownerName: null, VerifyUrl);

        Assert.DoesNotContain("Hi ", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildHtmlBody_WithBlankOwnerName_OmitsGreeting()
    {
        var body = ResendMagicLinkEmailSender.BuildHtmlBody("  ", VerifyUrl);

        Assert.DoesNotContain("Hi ", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildHtmlBody_OwnerNameIsHtmlEncoded()
    {
        var body = ResendMagicLinkEmailSender.BuildHtmlBody("<script>alert(1)</script>", VerifyUrl);

        Assert.DoesNotContain("<script>", body, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildHtmlBody_ContainsVerifyUrl()
    {
        var body = ResendMagicLinkEmailSender.BuildHtmlBody(ownerName: null, VerifyUrl);

        Assert.Contains(VerifyUrl, body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildHtmlBody_VerifyUrlIsHtmlEncoded()
    {
        // A URL containing an ampersand must be HTML-encoded in the href attribute.
        const string urlWithAmpersand = "https://emmz.live/auth/verify?token=abc&foo=bar";
        var body = ResendMagicLinkEmailSender.BuildHtmlBody(ownerName: null, urlWithAmpersand);

        Assert.DoesNotContain("token=abc&foo=bar", body, StringComparison.Ordinal);
        Assert.Contains("token=abc&amp;foo=bar", body, StringComparison.Ordinal);
    }
}
