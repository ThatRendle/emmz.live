using EmmzLive.Data;
using EmmzLive.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace EmmzLive.Pages;

[Authorize(AuthenticationSchemes = "anon-inbox-session")]
[ServiceFilter(typeof(ValidInboxFilter))]
public sealed class InboxModel(AppDbContext db) : PageModel
{
    public IReadOnlyList<Message> Messages { get; private set; } = [];
    public int TotalCount => Messages.Count;

    public async Task<IActionResult> OnGetAsync()
    {
        Messages = await LoadMessagesAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetListAsync()
    {
        Messages = await LoadMessagesAsync();
        // Construct directly so the handler is testable without a full MVC ViewContext.
        // IncludeOobCount=true tells the partial to emit the OOB count span; this must
        // only happen in poll responses, never in the initial inline <partial> render.
        var viewData = new ViewDataDictionary<InboxModel>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = this,
            ["IncludeOobCount"] = true,
        };
        return new PartialViewResult
        {
            ViewName = "_MessageList",
            ViewData = viewData,
        };
    }

    // Internal so tests can call it directly without routing infrastructure.
    internal async Task<IReadOnlyList<Message>> LoadMessagesAsync()
    {
        var slug = HttpContext.Items[ValidInboxFilter.InboxSlugKey] as string
            ?? throw new InvalidOperationException(
                "Canonical slug missing from HttpContext.Items — ValidInboxFilter must have run.");

        // If no inbox row exists yet (submit page not yet visited), return empty — don't 500.
        var inbox = await db.Inboxes
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Slug == slug);

        if (inbox is null)
            return [];

        return await db.Messages
            .AsNoTracking()
            .Where(m => m.InboxId == inbox.Id)
            .OrderBy(m => m.ReceivedAt)
            .ToListAsync();
    }
}
