using EmmzLive.Auth;
using EmmzLive.Pages.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmmzLive.Tests.Auth;

public sealed class RequestPageModelTests
{
    private const string Secret = "test-secret-key-at-least-32-chars-long-ok";
    private const string OwnerEmail = "owner@example.com";

    private static RequestModel CreateModel(out SpyEmailSender spy)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OWNER_EMAIL"] = OwnerEmail,
            })
            .Build();

        var tokenService = new MagicLinkTokenService(Secret, OwnerEmail);
        var model = new RequestModel(tokenService, config, NullLogger<RequestModel>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("emmz.live");

        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new PageActionDescriptor(),
            modelState);
        var pageContext = new PageContext(actionContext);
        model.PageContext = pageContext;

        spy = new SpyEmailSender();
        return model;
    }

    [Fact]
    public async Task OnPostAsync_CallsEmailSenderExactlyOnce()
    {
        var model = CreateModel(out var spy);

        await model.OnPostAsync(spy);

        Assert.Equal(1, spy.CallCount);
    }

    [Fact]
    public async Task OnPostAsync_SetsEmailSentTrue()
    {
        var model = CreateModel(out var spy);

        await model.OnPostAsync(spy);

        Assert.True(model.EmailSent);
    }

    [Fact]
    public async Task OnPostAsync_SendsAbsoluteVerifyUrl()
    {
        var model = CreateModel(out var spy);

        await model.OnPostAsync(spy);

        Assert.NotNull(spy.LastVerifyUrl);
        Assert.StartsWith("https://emmz.live/auth/verify?token=", spy.LastVerifyUrl,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OnPostAsync_VerifyUrlContainsToken()
    {
        var model = CreateModel(out var spy);

        await model.OnPostAsync(spy);

        // Token must be present and parseable (i.e., URL contains a non-empty token param).
        Assert.NotNull(spy.LastVerifyUrl);
        var uri = new Uri(spy.LastVerifyUrl!);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        Assert.False(string.IsNullOrEmpty(query["token"]));
    }

    [Fact]
    public async Task OnPostAsync_OwnerEmailNotExposedInPageModel()
    {
        var model = CreateModel(out var spy);

        await model.OnPostAsync(spy);

        // The page model must not carry the owner email as a public property
        // (spec: the page SHALL NOT display the owner email address).
        var props = typeof(RequestModel).GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var prop in props)
        {
            var value = prop.GetValue(model)?.ToString();
            Assert.False(
                value == OwnerEmail,
                $"Property '{prop.Name}' exposes the owner email.");
        }
    }

    [Fact]
    public void OnGet_DoesNotThrow()
    {
        var model = CreateModel(out _);
        var ex = Record.Exception(() => model.OnGet());
        Assert.Null(ex);
    }

    [Fact]
    public async Task OnPostAsync_WhenSenderThrows_ReturnPageResult()
    {
        var model = CreateModel(out _);

        var result = await model.OnPostAsync(new FailingEmailSender());

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_WhenSenderThrows_SetsSendFailedTrue()
    {
        var model = CreateModel(out _);

        await model.OnPostAsync(new FailingEmailSender());

        Assert.True(model.SendFailed);
    }

    [Fact]
    public async Task OnPostAsync_WhenSenderThrows_DoesNotSetEmailSent()
    {
        var model = CreateModel(out _);

        await model.OnPostAsync(new FailingEmailSender());

        Assert.False(model.EmailSent);
    }

    [Fact]
    public async Task OnPostAsync_WhenSenderThrows_OwnerEmailNotExposedInPageModel()
    {
        var model = CreateModel(out _);

        await model.OnPostAsync(new FailingEmailSender());

        var props = typeof(RequestModel).GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var prop in props)
        {
            var value = prop.GetValue(model)?.ToString();
            Assert.False(
                value == OwnerEmail,
                $"Property '{prop.Name}' exposes the owner email.");
        }
    }

    // -------------------------------------------------------------------------
    // Spy email sender
    // -------------------------------------------------------------------------

    internal sealed class SpyEmailSender : IMagicLinkEmailSender
    {
        public int CallCount { get; private set; }
        public string? LastVerifyUrl { get; private set; }

        public Task SendAsync(string verifyUrl)
        {
            CallCount++;
            LastVerifyUrl = verifyUrl;
            return Task.CompletedTask;
        }
    }

    internal sealed class FailingEmailSender : IMagicLinkEmailSender
    {
        public Task SendAsync(string verifyUrl) =>
            Task.FromException(new HttpRequestException("Resend unavailable"));
    }
}
