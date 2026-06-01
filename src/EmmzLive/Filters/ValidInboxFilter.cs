using EmmzLive.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EmmzLive.Filters;

/// <summary>
/// Page filter that returns 404 for any {conf}/{talk} route combination not present
/// in <see cref="InboxConfig"/>. Apply with [ServiceFilter(typeof(ValidInboxFilter))]
/// or via convention on pages that have both {conf} and {talk} route values.
///
/// When the slug is valid the filter writes the canonical (configured-form) slug to
/// <see cref="HttpContext.Items"/> under the key <see cref="InboxSlugKey"/>. Downstream
/// page handlers and section 4/6 DB lookups must read from that key so that URL casing
/// never produces distinct database rows for the same logical inbox.
/// </summary>
public sealed class ValidInboxFilter(InboxConfig inboxConfig) : IAsyncPageFilter
{
    /// <summary>
    /// Key used to store the canonical inbox slug in <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/>.
    /// Page handlers that need the slug must read it from this key rather than from raw route values.
    /// </summary>
    public const string InboxSlugKey = "InboxSlug";

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        if (!TryGetCanonicalSlug(context.RouteData.Values, out var canonical))
        {
            context.Result = new NotFoundResult();
            return;
        }

        context.HttpContext.Items[InboxSlugKey] = canonical;
        await next();
    }

    /// <summary>
    /// Reconstructs "conf/talk" from route values, looks up the canonical form, and returns it.
    /// Extracted for unit-testability without a full Razor Pages pipeline.
    /// </summary>
    public bool TryGetCanonicalSlug(IReadOnlyDictionary<string, object?> routeValues, out string canonical)
    {
        canonical = string.Empty;
        var conf = routeValues.TryGetValue("conf", out var c) ? c?.ToString() : null;
        var talk = routeValues.TryGetValue("talk", out var t) ? t?.ToString() : null;

        if (string.IsNullOrEmpty(conf) || string.IsNullOrEmpty(talk))
            return false;

        return inboxConfig.TryGetCanonical($"{conf}/{talk}", out canonical);
    }

    /// <summary>
    /// Convenience wrapper retained for tests that only need a boolean validity check.
    /// </summary>
    public bool IsValidSlug(IReadOnlyDictionary<string, object?> routeValues)
        => TryGetCanonicalSlug(routeValues, out _);
}
