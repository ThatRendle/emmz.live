using System.ComponentModel.DataAnnotations;
using EmmzLive.Data;
using EmmzLive.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace EmmzLive.Pages;

[ServiceFilter(typeof(ValidInboxFilter))]
public sealed class SubmitModel(AppDbContext db) : PageModel
{
    [BindProperty]
    [MaxLength(64)]
    public string? SenderName { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "A message is required.")]
    [MaxLength(512)]
    public string Body { get; set; } = string.Empty;

    public string QrCodeBase64 { get; private set; } = string.Empty;

    public bool Submitted { get; private set; }

    /// <summary>
    /// The name the just-submitted message was sent under, or <c>null</c> if it was sent
    /// anonymously. Captured before the form fields are cleared so the confirmation can
    /// reflect whether a name was given.
    /// </summary>
    public string? SubmittedSenderName { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var slug = GetCanonicalSlug();
        await GetOrCreateInboxAsync(slug);
        QrCodeBase64 = GenerateQrCode(BuildPageUrl());
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Regenerate QR code for the re-rendered page regardless of outcome.
        QrCodeBase64 = GenerateQrCode(BuildPageUrl());

        if (!ModelState.IsValid)
            return Page();

        var slug = GetCanonicalSlug();
        var inbox = await GetOrCreateInboxAsync(slug);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            InboxId = inbox.Id,
            SenderName = string.IsNullOrWhiteSpace(SenderName) ? null : SenderName.Trim(),
            Body = Body.Trim(),
            ReceivedAt = DateTimeOffset.UtcNow,
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        Submitted = true;
        SubmittedSenderName = message.SenderName;

        // Clear form fields after successful submission.
        SenderName = null;
        Body = string.Empty;
        ModelState.Clear();

        return Page();
    }

    /// <summary>
    /// Returns the canonical slug written by <see cref="ValidInboxFilter"/>.
    /// The filter guarantees this is present before any handler runs.
    /// </summary>
    private string GetCanonicalSlug() =>
        HttpContext.Items[ValidInboxFilter.InboxSlugKey] as string
        ?? throw new InvalidOperationException("Canonical slug missing from HttpContext.Items — ValidInboxFilter must have run.");

    /// <summary>
    /// Gets or creates the <see cref="Inbox"/> row for the given canonical slug.
    /// A try-then-requery pattern handles concurrent first-load races without over-engineering.
    /// </summary>
    internal async Task<Inbox> GetOrCreateInboxAsync(string canonicalSlug)
    {
        var existing = await db.Inboxes.FirstOrDefaultAsync(i => i.Slug == canonicalSlug);
        if (existing is not null)
            return existing;

        var inbox = new Inbox
        {
            Id = Guid.NewGuid(),
            Slug = canonicalSlug,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        try
        {
            db.Inboxes.Add(inbox);
            await db.SaveChangesAsync();
            return inbox;
        }
        catch (DbUpdateException)
        {
            // Concurrent insert of the same slug — re-query to get the winner's row.
            db.ChangeTracker.Clear();
            return await db.Inboxes.FirstAsync(i => i.Slug == canonicalSlug);
        }
    }

    /// <summary>
    /// Builds the full URL of this page using forwarded-header-corrected scheme and host.
    /// </summary>
    internal string BuildPageUrl() =>
        $"{Request.Scheme}://{Request.Host}{Request.Path}";

    /// <summary>
    /// Generates a base64-encoded PNG QR code for the given URL using <see cref="PngByteQRCode"/>
    /// so there is no dependency on System.Drawing / libgdiplus in the Linux container.
    /// </summary>
    internal static string GenerateQrCode(string url)
    {
        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(10);
        return Convert.ToBase64String(bytes);
    }
}
