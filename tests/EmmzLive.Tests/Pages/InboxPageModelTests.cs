using EmmzLive.Data;
using EmmzLive.Filters;
using EmmzLive.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EmmzLive.Tests.Pages;

/// <summary>
/// Unit tests for <see cref="InboxModel"/>.
///
/// Note: the [Authorize] redirect behaviour (unauthenticated request → /auth/request) cannot be
/// exercised here because it requires the full Razor Pages + authentication middleware pipeline.
/// That path is validated by the reviewer and confirmed via manual HITL.
///
/// Similarly, the JavaScript keyboard navigation, selection persistence across HTMX swaps,
/// and projector readability require a real browser / manual verification.
/// </summary>
public sealed class InboxPageModelTests : IDisposable
{
    private readonly AppDbContext _db;

    public InboxPageModelTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static InboxModel CreateModel(AppDbContext db, string canonicalSlug = "cph/hitc")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[ValidInboxFilter.InboxSlugKey] = canonicalSlug;

        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new PageActionDescriptor(),
            modelState);
        var pageContext = new PageContext(actionContext);

        return new InboxModel(db)
        {
            PageContext = pageContext,
            TempData = new NullTempDataDictionary(),
        };
    }

    private async Task<Inbox> SeedInboxAsync(string slug)
    {
        var inbox = new Inbox { Id = Guid.NewGuid(), Slug = slug, CreatedAt = DateTimeOffset.UtcNow };
        _db.Inboxes.Add(inbox);
        await _db.SaveChangesAsync();
        return inbox;
    }

    // -------------------------------------------------------------------------
    // OnGetAsync — returns Page() and populates Messages
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnGetAsync_WithMessages_ReturnsPageResult()
    {
        var inbox = await SeedInboxAsync("cph/hitc");
        _db.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            InboxId = inbox.Id,
            Body = "Hello",
            ReceivedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync();

        var model = CreateModel(_db, "cph/hitc");
        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Messages);
    }

    // -------------------------------------------------------------------------
    // LoadMessagesAsync — correct inbox, ascending order
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadMessagesAsync_ReturnsMessagesForCorrectInbox_OrderedAscending()
    {
        var inbox = await SeedInboxAsync("cph/hitc");
        var now = DateTimeOffset.UtcNow;

        _db.Messages.AddRange(
            new Message { Id = Guid.NewGuid(), InboxId = inbox.Id, Body = "Second", ReceivedAt = now.AddMinutes(1) },
            new Message { Id = Guid.NewGuid(), InboxId = inbox.Id, Body = "First", ReceivedAt = now });
        await _db.SaveChangesAsync();

        var model = CreateModel(_db, "cph/hitc");
        var messages = await model.LoadMessagesAsync();

        Assert.Equal(2, messages.Count);
        Assert.Equal("First", messages[0].Body);
        Assert.Equal("Second", messages[1].Body);
    }

    [Fact]
    public async Task LoadMessagesAsync_ExcludesMessagesFromOtherInboxes()
    {
        var target = await SeedInboxAsync("cph/hitc");
        var other = await SeedInboxAsync("ndcoslo/tdd");

        _db.Messages.AddRange(
            new Message { Id = Guid.NewGuid(), InboxId = target.Id, Body = "Mine", ReceivedAt = DateTimeOffset.UtcNow },
            new Message { Id = Guid.NewGuid(), InboxId = other.Id, Body = "Theirs", ReceivedAt = DateTimeOffset.UtcNow });
        await _db.SaveChangesAsync();

        var model = CreateModel(_db, "cph/hitc");
        var messages = await model.LoadMessagesAsync();

        Assert.Single(messages);
        Assert.Equal("Mine", messages[0].Body);
    }

    // -------------------------------------------------------------------------
    // Empty inbox (no row)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadMessagesAsync_MissingInboxRow_ReturnsEmptyList()
    {
        // No inbox row seeded — should not throw.
        var model = CreateModel(_db, "cph/hitc");
        var messages = await model.LoadMessagesAsync();

        Assert.Empty(messages);
    }

    [Fact]
    public async Task LoadMessagesAsync_ExistingInboxWithNoMessages_ReturnsEmptyList()
    {
        await SeedInboxAsync("cph/hitc");

        var model = CreateModel(_db, "cph/hitc");
        var messages = await model.LoadMessagesAsync();

        Assert.Empty(messages);
    }

    // -------------------------------------------------------------------------
    // TotalCount reflects Messages.Count
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TotalCount_EqualsMessagesCount()
    {
        var inbox = await SeedInboxAsync("cph/hitc");
        var now = DateTimeOffset.UtcNow;

        _db.Messages.AddRange(
            new Message { Id = Guid.NewGuid(), InboxId = inbox.Id, Body = "A", ReceivedAt = now },
            new Message { Id = Guid.NewGuid(), InboxId = inbox.Id, Body = "B", ReceivedAt = now.AddSeconds(1) },
            new Message { Id = Guid.NewGuid(), InboxId = inbox.Id, Body = "C", ReceivedAt = now.AddSeconds(2) });
        await _db.SaveChangesAsync();

        var model = CreateModel(_db, "cph/hitc");
        await model.OnGetAsync();

        Assert.Equal(3, model.TotalCount);
        Assert.Equal(model.Messages.Count, model.TotalCount);
    }

    // -------------------------------------------------------------------------
    // OnGetListAsync — returns PartialViewResult for _MessageList
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnGetListAsync_ReturnsPartialViewResult_Named_MessageList()
    {
        var inbox = await SeedInboxAsync("cph/hitc");
        _db.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            InboxId = inbox.Id,
            Body = "From the list handler",
            ReceivedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync();

        var model = CreateModel(_db, "cph/hitc");
        var result = await model.OnGetListAsync();

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_MessageList", partial.ViewName);
    }

    [Fact]
    public async Task OnGetListAsync_PartialModel_ContainsMessagesOrderedAscending()
    {
        var inbox = await SeedInboxAsync("cph/hitc");
        var now = DateTimeOffset.UtcNow;

        _db.Messages.AddRange(
            new Message { Id = Guid.NewGuid(), InboxId = inbox.Id, Body = "Later", ReceivedAt = now.AddMinutes(2) },
            new Message { Id = Guid.NewGuid(), InboxId = inbox.Id, Body = "Earlier", ReceivedAt = now });
        await _db.SaveChangesAsync();

        var model = CreateModel(_db, "cph/hitc");
        var result = await model.OnGetListAsync();

        var partial = Assert.IsType<PartialViewResult>(result);
        var returnedModel = Assert.IsType<InboxModel>(partial.Model);
        Assert.Equal(2, returnedModel.Messages.Count);
        Assert.Equal("Earlier", returnedModel.Messages[0].Body);
        Assert.Equal("Later", returnedModel.Messages[1].Body);
    }

    // -------------------------------------------------------------------------
    // SenderName null vs present carried through to model
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadMessagesAsync_SenderNameNull_IsNullOnModel()
    {
        var inbox = await SeedInboxAsync("cph/hitc");
        _db.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            InboxId = inbox.Id,
            Body = "Anonymous",
            SenderName = null,
            ReceivedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync();

        var model = CreateModel(_db, "cph/hitc");
        var messages = await model.LoadMessagesAsync();

        Assert.Null(messages[0].SenderName);
    }

    [Fact]
    public async Task LoadMessagesAsync_SenderNamePresent_IsPreservedOnModel()
    {
        var inbox = await SeedInboxAsync("cph/hitc");
        _db.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            InboxId = inbox.Id,
            Body = "Named",
            SenderName = "Alice",
            ReceivedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync();

        var model = CreateModel(_db, "cph/hitc");
        var messages = await model.LoadMessagesAsync();

        Assert.Equal("Alice", messages[0].SenderName);
    }

    // -------------------------------------------------------------------------
    // OnGetListAsync — TotalCount in partial model matches loaded messages
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnGetListAsync_PartialModel_TotalCount_MatchesMessages()
    {
        var inbox = await SeedInboxAsync("cph/hitc");
        var now = DateTimeOffset.UtcNow;

        _db.Messages.AddRange(
            new Message { Id = Guid.NewGuid(), InboxId = inbox.Id, Body = "X", ReceivedAt = now },
            new Message { Id = Guid.NewGuid(), InboxId = inbox.Id, Body = "Y", ReceivedAt = now.AddSeconds(1) });
        await _db.SaveChangesAsync();

        var model = CreateModel(_db, "cph/hitc");
        var result = await model.OnGetListAsync();

        var partial = Assert.IsType<PartialViewResult>(result);
        var returnedModel = Assert.IsType<InboxModel>(partial.Model);
        Assert.Equal(returnedModel.Messages.Count, returnedModel.TotalCount);
        Assert.Equal(2, returnedModel.TotalCount);
    }

    [Fact]
    public async Task OnGetListAsync_ViewData_IncludeOobCount_IsTrue()
    {
        // The poll partial must carry IncludeOobCount=true so _MessageList.cshtml
        // emits the OOB count span.  The initial inline <partial> render never sets
        // this flag, preventing a duplicate id="msg-count" on first paint.
        var inbox = await SeedInboxAsync("cph/hitc");
        _db.Messages.Add(new Message { Id = Guid.NewGuid(), InboxId = inbox.Id, Body = "Hi", ReceivedAt = DateTimeOffset.UtcNow });
        await _db.SaveChangesAsync();

        var model = CreateModel(_db, "cph/hitc");
        var result = await model.OnGetListAsync();

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.True(partial.ViewData["IncludeOobCount"] is true);
    }

    // -------------------------------------------------------------------------
    // MessagePreview.Truncate — surrogate-safe truncation
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Hello", 10, "Hello")]
    [InlineData("Hello world!", 5, "Hello…")]
    public void Truncate_ShortOrExactBody_NoEllipsis_OrTruncatesAt(string body, int max, string expected)
    {
        Assert.Equal(expected, MessagePreview.Truncate(body, max));
    }

    [Fact]
    public void Truncate_BodyEndingWithEmojiAtBoundary_DoesNotSplitSurrogatePair()
    {
        // "A" * 119 + emoji (2 UTF-16 code units) => body.Length == 121.
        // A naive body[..120] would place the cut between the high and low surrogate.
        var prefix = new string('A', 119);
        const string emoji = "\U0001F600"; // 😀 — high surrogate at [119], low surrogate at [120]
        var body = prefix + emoji; // length 121

        var result = MessagePreview.Truncate(body, 120);

        // The result must end with the ellipsis character and the char before it must be 'A',
        // confirming the whole surrogate pair was dropped rather than leaving a lone surrogate.
        Assert.EndsWith("…", result);
        Assert.Equal('A', result[^2]); // '…' is a single BMP char, so [^1] is '…', [^2] is last kept char
        Assert.False(result.Any(char.IsLowSurrogate), "Result must not contain a lone low surrogate.");
    }

    [Fact]
    public void Truncate_BodyWithEmojiNotAtBoundary_IncludesEmoji()
    {
        // Emoji well within the limit — should pass through unchanged.
        const string emoji = "\U0001F600";
        var body = "Hello " + emoji + " world";

        var result = MessagePreview.Truncate(body, 120);

        Assert.Equal(body, result);
    }

    // -------------------------------------------------------------------------
    // Minimal ITempDataDictionary stub — copied from SubmitPageModelTests.
    // -------------------------------------------------------------------------

    private sealed class NullTempDataDictionary : ITempDataDictionary
    {
        private readonly Dictionary<string, object?> _data = [];

        public object? this[string key]
        {
            get => _data.TryGetValue(key, out var v) ? v : null;
            set => _data[key] = value;
        }

        public ICollection<string> Keys => _data.Keys;
        public ICollection<object?> Values => _data.Values!;
        public int Count => _data.Count;
        public bool IsReadOnly => false;

        public void Load() { }
        public void Save() { }
        public void Keep() { }
        public void Keep(string key) { }
        public object? Peek(string key) => _data.TryGetValue(key, out var v) ? v : null;
        public void Add(string key, object? value) => _data.Add(key, value);
        public void Clear() => _data.Clear();
        public bool Contains(KeyValuePair<string, object?> item) => ((ICollection<KeyValuePair<string, object?>>)_data).Contains(item);
        public bool ContainsKey(string key) => _data.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, object?>>)_data).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _data.GetEnumerator();
        public bool Remove(string key) => _data.Remove(key);
        public bool Remove(KeyValuePair<string, object?> item) => ((ICollection<KeyValuePair<string, object?>>)_data).Remove(item);
        public bool TryGetValue(string key, out object? value) => _data.TryGetValue(key, out value);
        public void Add(KeyValuePair<string, object?> item) => ((ICollection<KeyValuePair<string, object?>>)_data).Add(item);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _data.GetEnumerator();
    }
}
