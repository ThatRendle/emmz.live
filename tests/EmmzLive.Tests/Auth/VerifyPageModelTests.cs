using System.Security.Claims;
using EmmzLive.Auth;
using EmmzLive.Pages.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace EmmzLive.Tests.Auth;

public sealed class VerifyPageModelTests
{
    private const string Secret = "test-secret-key-at-least-32-chars-long-ok";
    private const string OwnerEmail = "owner@example.com";

    private static (VerifyModel model, SpyAuthenticationService authSpy) CreateModel()
    {
        var tokenService = new MagicLinkTokenService(Secret, OwnerEmail);

        var authSpy = new SpyAuthenticationService();
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(authSpy);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
        };

        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new PageActionDescriptor(),
            modelState);
        var pageContext = new PageContext(actionContext);

        var model = new VerifyModel(tokenService)
        {
            PageContext = pageContext,
        };

        return (model, authSpy);
    }

    private static string CreateValidToken()
    {
        var svc = new MagicLinkTokenService(Secret, OwnerEmail);
        return svc.Create(OwnerEmail);
    }

    // --- Valid token → redirect to / and sign-in invoked ---

    [Fact]
    public async Task OnGetAsync_ValidToken_RedirectsToRoot()
    {
        var (model, _) = CreateModel();
        var token = CreateValidToken();

        var result = await model.OnGetAsync(token);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public async Task OnGetAsync_ValidToken_SignInInvoked()
    {
        var (model, authSpy) = CreateModel();
        var token = CreateValidToken();

        await model.OnGetAsync(token);

        Assert.True(authSpy.SignInCalled);
    }

    [Fact]
    public async Task OnGetAsync_ValidToken_SignInWithCorrectScheme()
    {
        var (model, authSpy) = CreateModel();
        var token = CreateValidToken();

        await model.OnGetAsync(token);

        Assert.Equal("anon-inbox-session", authSpy.LastScheme);
    }

    [Fact]
    public async Task OnGetAsync_ValidToken_SessionCookieNotPersistent()
    {
        var (model, authSpy) = CreateModel();
        var token = CreateValidToken();

        await model.OnGetAsync(token);

        Assert.NotNull(authSpy.LastProperties);
        Assert.False(authSpy.LastProperties!.IsPersistent);
    }

    // --- BadSignature → 400 ---

    [Fact]
    public async Task OnGetAsync_TamperedToken_ReturnsBadRequest()
    {
        var (model, authSpy) = CreateModel();
        var token = CreateValidToken();
        var tampered = token[..^2] + "ZZ";

        var result = await model.OnGetAsync(tampered);

        Assert.IsType<BadRequestResult>(result);
        Assert.False(authSpy.SignInCalled);
    }

    [Fact]
    public async Task OnGetAsync_NullToken_ReturnsBadRequest()
    {
        var (model, authSpy) = CreateModel();

        var result = await model.OnGetAsync(null);

        Assert.IsType<BadRequestResult>(result);
        Assert.False(authSpy.SignInCalled);
    }

    // --- Expired token → error page (not 400, no sign-in) ---

    [Fact]
    public async Task OnGetAsync_ExpiredToken_ReturnsPageResult()
    {
        var (model, authSpy) = CreateModel();
        var expiredToken = BuildExpiredToken();

        var result = await model.OnGetAsync(expiredToken);

        // Not a BadRequestResult — it is the error page path.
        Assert.IsNotType<BadRequestResult>(result);
        Assert.IsType<PageResult>(result);
        Assert.False(authSpy.SignInCalled);
    }

    [Fact]
    public async Task OnGetAsync_ExpiredToken_SetsIsExpiredTrue()
    {
        var (model, _) = CreateModel();
        var expiredToken = BuildExpiredToken();

        await model.OnGetAsync(expiredToken);

        Assert.True(model.IsExpired);
    }

    private static string BuildExpiredToken()
    {
        var pastExpiry = DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds();
        var payload = $"{OwnerEmail}:{pastExpiry}";
        var payloadEncoded = MagicLinkTokenService.Base64UrlEncode(
            System.Text.Encoding.UTF8.GetBytes(payload));
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(Secret);
        using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
        var sig = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payloadEncoded));
        return $"{payloadEncoded}.{MagicLinkTokenService.Base64UrlEncode(sig)}";
    }

    // -------------------------------------------------------------------------
    // Spy authentication service
    // -------------------------------------------------------------------------

    private sealed class SpyAuthenticationService : IAuthenticationService
    {
        public bool SignInCalled { get; private set; }
        public string? LastScheme { get; private set; }
        public AuthenticationProperties? LastProperties { get; private set; }

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties)
        {
            SignInCalled = true;
            LastScheme = scheme;
            LastProperties = properties;
            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }
}
