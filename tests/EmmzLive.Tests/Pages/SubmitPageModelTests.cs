using System.ComponentModel.DataAnnotations;
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

public sealed class SubmitPageModelTests : IDisposable
{
    private readonly AppDbContext _db;

    public SubmitPageModelTests()
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

    private static SubmitModel CreateModel(AppDbContext db, string canonicalSlug = "cph/hitc")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[ValidInboxFilter.InboxSlugKey] = canonicalSlug;
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("emmz.live");
        httpContext.Request.Path = new PathString($"/{canonicalSlug}");

        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var pageContext = new PageContext(actionContext);

        var model = new SubmitModel(db)
        {
            PageContext = pageContext,
            TempData = new NullTempDataDictionary(),
        };

        return model;
    }

    // -------------------------------------------------------------------------
    // OnPostAsync — valid message with name
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnPostAsync_ValidMessageWithName_SavesMessageAndSetsSubmitted()
    {
        var model = CreateModel(_db);
        model.Body = "Great talk!";
        model.SenderName = "Alice";

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.Submitted);

        var msg = await _db.Messages.SingleAsync();
        Assert.Equal("Great talk!", msg.Body);
        Assert.Equal("Alice", msg.SenderName);

        // Confirmation reflects the name it was sent under (captured before fields are cleared).
        Assert.Equal("Alice", model.SubmittedSenderName);
    }

    [Fact]
    public async Task OnPostAsync_ValidMessage_MessageIsAssociatedWithCorrectInbox()
    {
        var model = CreateModel(_db, "ndcoslo/tdd");
        model.Body = "Interesting approach.";

        await model.OnPostAsync();

        var msg = await _db.Messages.Include(m => m.Inbox).SingleAsync();
        Assert.Equal("ndcoslo/tdd", msg.Inbox.Slug);
    }

    // -------------------------------------------------------------------------
    // OnPostAsync — blank name → SenderName stored as null
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OnPostAsync_BlankName_StoresSenderNameAsNull(string? name)
    {
        var model = CreateModel(_db);
        model.Body = "Anonymous message.";
        model.SenderName = name;

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.Submitted);

        var msg = await _db.Messages.SingleAsync();
        Assert.Null(msg.SenderName);

        // Anonymous submission → confirmation shows the anonymous wording.
        Assert.Null(model.SubmittedSenderName);
    }

    // -------------------------------------------------------------------------
    // OnPostAsync — empty/whitespace body → validation error, no save
    // -------------------------------------------------------------------------

    // Verifies that the [Required] annotation on Body actually rejects empty and
    // whitespace input (Required uses IsNullOrWhiteSpace under the hood).
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Body_EmptyOrWhitespace_FailsRequiredAnnotation(string? body)
    {
        var model = CreateModel(_db);
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model) { MemberName = nameof(SubmitModel.Body) };

        var isValid = Validator.TryValidateProperty(body, ctx, results);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void Body_NonEmpty_PassesRequiredAnnotation()
    {
        var model = CreateModel(_db);
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model) { MemberName = nameof(SubmitModel.Body) };

        var isValid = Validator.TryValidateProperty("Hello!", ctx, results);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    // Verifies that when ModelState is invalid (as produced by the framework
    // after annotation validation fails), the handler short-circuits and saves nothing.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OnPostAsync_ModelStateInvalid_NoMessageSaved(string body)
    {
        var model = CreateModel(_db);
        model.Body = body;
        model.ModelState.AddModelError(nameof(model.Body), "A message is required.");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.Submitted);
        Assert.False(model.ModelState.IsValid);
        Assert.Equal(0, await _db.Messages.CountAsync());
    }

    // -------------------------------------------------------------------------
    // Field length limits
    // -------------------------------------------------------------------------

    [Fact]
    public void SenderName_AtLimit_PassesMaxLengthAnnotation()
    {
        var model = CreateModel(_db);
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model) { MemberName = nameof(SubmitModel.SenderName) };
        var name64 = new string('a', 64);

        var isValid = Validator.TryValidateProperty(name64, ctx, results);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void SenderName_OverLimit_FailsMaxLengthAnnotation()
    {
        var model = CreateModel(_db);
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model) { MemberName = nameof(SubmitModel.SenderName) };
        var name65 = new string('a', 65);

        var isValid = Validator.TryValidateProperty(name65, ctx, results);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task OnPostAsync_NameOverLimit_ModelStateInvalid_NoMessageSaved()
    {
        var model = CreateModel(_db);
        model.Body = "Hello!";
        model.SenderName = new string('a', 65);
        model.ModelState.AddModelError(nameof(model.SenderName), "The field SenderName must be a string with a maximum length of 64.");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.Submitted);
        Assert.False(model.ModelState.IsValid);
        Assert.Equal(0, await _db.Messages.CountAsync());
    }

    [Fact]
    public void Body_AtLimit_PassesMaxLengthAnnotation()
    {
        var model = CreateModel(_db);
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model) { MemberName = nameof(SubmitModel.Body) };
        var body512 = new string('x', 512);

        var isValid = Validator.TryValidateProperty(body512, ctx, results);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void Body_OverLimit_FailsMaxLengthAnnotation()
    {
        var model = CreateModel(_db);
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model) { MemberName = nameof(SubmitModel.Body) };
        var body513 = new string('x', 513);

        var isValid = Validator.TryValidateProperty(body513, ctx, results);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task OnPostAsync_BodyOverLimit_ModelStateInvalid_NoMessageSaved()
    {
        var model = CreateModel(_db);
        model.Body = new string('x', 513);
        model.ModelState.AddModelError(nameof(model.Body), "The field Body must be a string with a maximum length of 512.");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.Submitted);
        Assert.False(model.ModelState.IsValid);
        Assert.Equal(0, await _db.Messages.CountAsync());
    }

    // -------------------------------------------------------------------------
    // Get-or-create: first call creates, second call reuses
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrCreateInboxAsync_FirstCall_CreatesInboxRow()
    {
        var model = CreateModel(_db);

        var inbox = await model.GetOrCreateInboxAsync("cph/hitc");

        Assert.Equal("cph/hitc", inbox.Slug);
        Assert.Equal(1, await _db.Inboxes.CountAsync());
    }

    [Fact]
    public async Task GetOrCreateInboxAsync_SecondCall_ReusesExistingRow()
    {
        var model = CreateModel(_db);

        var first = await model.GetOrCreateInboxAsync("cph/hitc");
        var second = await model.GetOrCreateInboxAsync("cph/hitc");

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await _db.Inboxes.CountAsync());
    }

    // -------------------------------------------------------------------------
    // QR code generation
    // -------------------------------------------------------------------------

    [Fact]
    public void GenerateQrCode_ReturnsNonEmptyBase64()
    {
        var base64 = SubmitModel.GenerateQrCode("https://emmz.live/cph/hitc");

        Assert.False(string.IsNullOrWhiteSpace(base64));

        // Verify the decoded bytes start with the PNG magic bytes (\x89PNG).
        var bytes = Convert.FromBase64String(base64);
        Assert.True(bytes.Length > 4);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void BuildPageUrl_ReturnsSchemeHostPath()
    {
        var model = CreateModel(_db, "cph/hitc");

        var url = model.BuildPageUrl();

        Assert.Equal("https://emmz.live/cph/hitc", url);
    }

    // -------------------------------------------------------------------------
    // OnPostAsync — form fields cleared after successful submission
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnPostAsync_Success_ClearsFormFields()
    {
        var model = CreateModel(_db);
        model.Body = "A real question";
        model.SenderName = "Bob";

        await model.OnPostAsync();

        Assert.Null(model.SenderName);
        Assert.Equal(string.Empty, model.Body);
    }

    // -------------------------------------------------------------------------
    // Minimal ITempDataDictionary stub — avoids requiring session infrastructure.
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
