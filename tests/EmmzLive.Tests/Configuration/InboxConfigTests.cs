using EmmzLive.Configuration;
using EmmzLive.Filters;

namespace EmmzLive.Tests.Configuration;

public sealed class InboxConfigTests
{
    // --- InboxConfig.Parse ---

    [Fact]
    public void Parse_ValidMultiSlugString_ReturnsAllSlugs()
    {
        var result = InboxConfig.Parse("cph/hitc,cph/rtbc");
        Assert.Equal(2, result.Count);
        Assert.Contains("cph/hitc", result);
        Assert.Contains("cph/rtbc", result);
    }

    [Fact]
    public void Parse_SingleSlug_ReturnsSingleEntry()
    {
        var result = InboxConfig.Parse("ndcoslo/tdd");
        Assert.Single(result);
        Assert.Contains("ndcoslo/tdd", result);
    }

    [Fact]
    public void Parse_TrailingComma_IgnoresEmptyEntry()
    {
        var result = InboxConfig.Parse("cph/hitc,");
        Assert.Single(result);
        Assert.Contains("cph/hitc", result);
    }

    [Fact]
    public void Parse_WhitespaceAroundEntries_TrimsCorrectly()
    {
        var result = InboxConfig.Parse("  cph/hitc , cph/rtbc  ");
        Assert.Equal(2, result.Count);
        Assert.Contains("cph/hitc", result);
        Assert.Contains("cph/rtbc", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhitespace_ReturnsEmptySet(string? raw)
    {
        var result = InboxConfig.Parse(raw);
        Assert.Empty(result);
    }

    // --- InboxConfig constructor ---

    [Fact]
    public void Constructor_ValidString_PopulatesSlugs()
    {
        var cfg = new InboxConfig("cph/hitc,cph/rtbc");
        Assert.Equal(2, cfg.Slugs.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",,,")]
    public void Constructor_MissingOrEmpty_ThrowsDescriptiveError(string? raw)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new InboxConfig(raw));
        Assert.Contains("INBOXES", ex.Message);
    }

    // --- Case-insensitive matching ---

    [Fact]
    public void IsValid_SlugMatchesCaseInsensitively()
    {
        var cfg = new InboxConfig("CPH/HITC");
        Assert.True(cfg.IsValid("cph/hitc"));
        Assert.True(cfg.IsValid("CPH/HITC"));
        Assert.True(cfg.IsValid("Cph/Hitc"));
    }

    [Fact]
    public void IsValid_UnknownSlug_ReturnsFalse()
    {
        var cfg = new InboxConfig("cph/hitc");
        Assert.False(cfg.IsValid("ndcoslo/tdd"));
    }

    // --- ValidInboxFilter.IsValidSlug ---

    [Fact]
    public void ValidInboxFilter_KnownSlug_ReturnsTrue()
    {
        var cfg = new InboxConfig("cph/hitc");
        var filter = new ValidInboxFilter(cfg);

        var routeValues = new Dictionary<string, object?> { ["conf"] = "cph", ["talk"] = "hitc" };
        Assert.True(filter.IsValidSlug(routeValues));
    }

    [Fact]
    public void ValidInboxFilter_UnknownSlug_ReturnsFalse()
    {
        var cfg = new InboxConfig("cph/hitc");
        var filter = new ValidInboxFilter(cfg);

        var routeValues = new Dictionary<string, object?> { ["conf"] = "ndcoslo", ["talk"] = "tdd" };
        Assert.False(filter.IsValidSlug(routeValues));
    }

    [Theory]
    [InlineData(null, "hitc")]
    [InlineData("cph", null)]
    [InlineData("", "hitc")]
    [InlineData("cph", "")]
    public void ValidInboxFilter_MissingRouteValues_ReturnsFalse(string? conf, string? talk)
    {
        var cfg = new InboxConfig("cph/hitc");
        var filter = new ValidInboxFilter(cfg);

        var routeValues = new Dictionary<string, object?> { ["conf"] = conf, ["talk"] = talk };
        Assert.False(filter.IsValidSlug(routeValues));
    }

    [Fact]
    public void ValidInboxFilter_SlugMatchesCaseInsensitively()
    {
        var cfg = new InboxConfig("CPH/HITC");
        var filter = new ValidInboxFilter(cfg);

        var routeValues = new Dictionary<string, object?> { ["conf"] = "cph", ["talk"] = "hitc" };
        Assert.True(filter.IsValidSlug(routeValues));
    }

    // --- InboxConfig.TryGetCanonical ---

    [Fact]
    public void TryGetCanonical_ExactMatch_ReturnsConfiguredForm()
    {
        var cfg = new InboxConfig("cph/hitc");
        var found = cfg.TryGetCanonical("cph/hitc", out var canonical);
        Assert.True(found);
        Assert.Equal("cph/hitc", canonical);
    }

    [Fact]
    public void TryGetCanonical_DifferentCaseInput_ReturnsConfiguredForm()
    {
        var cfg = new InboxConfig("cph/hitc");
        var found = cfg.TryGetCanonical("CPH/HITC", out var canonical);
        Assert.True(found);
        Assert.Equal("cph/hitc", canonical);
    }

    [Fact]
    public void TryGetCanonical_MixedCaseInput_ReturnsConfiguredForm()
    {
        var cfg = new InboxConfig("cph/hitc");
        var found = cfg.TryGetCanonical("Cph/Hitc", out var canonical);
        Assert.True(found);
        Assert.Equal("cph/hitc", canonical);
    }

    [Fact]
    public void TryGetCanonical_ConfiguredUpperCase_DifferentCaseInput_ReturnsConfiguredForm()
    {
        var cfg = new InboxConfig("CPH/HITC");
        var found = cfg.TryGetCanonical("cph/hitc", out var canonical);
        Assert.True(found);
        Assert.Equal("CPH/HITC", canonical);
    }

    [Fact]
    public void TryGetCanonical_UnknownSlug_ReturnsFalse()
    {
        var cfg = new InboxConfig("cph/hitc");
        var found = cfg.TryGetCanonical("ndcoslo/tdd", out var canonical);
        Assert.False(found);
        Assert.Equal(string.Empty, canonical);
    }

    // --- ValidInboxFilter.TryGetCanonicalSlug ---

    [Fact]
    public void ValidInboxFilter_TryGetCanonicalSlug_KnownSlug_ReturnsCanonicalForm()
    {
        var cfg = new InboxConfig("cph/hitc");
        var filter = new ValidInboxFilter(cfg);

        var routeValues = new Dictionary<string, object?> { ["conf"] = "CPH", ["talk"] = "HITC" };
        var found = filter.TryGetCanonicalSlug(routeValues, out var canonical);

        Assert.True(found);
        Assert.Equal("cph/hitc", canonical);
    }

    [Fact]
    public void ValidInboxFilter_TryGetCanonicalSlug_UnknownSlug_ReturnsFalse()
    {
        var cfg = new InboxConfig("cph/hitc");
        var filter = new ValidInboxFilter(cfg);

        var routeValues = new Dictionary<string, object?> { ["conf"] = "ndcoslo", ["talk"] = "tdd" };
        var found = filter.TryGetCanonicalSlug(routeValues, out var canonical);

        Assert.False(found);
        Assert.Equal(string.Empty, canonical);
    }

    [Fact]
    public void ValidInboxFilter_TryGetCanonicalSlug_DifferentCaseValid_IsAccepted()
    {
        var cfg = new InboxConfig("cph/hitc");
        var filter = new ValidInboxFilter(cfg);

        // Simulates /CPH/HITC arriving in route values — must not 404.
        var routeValues = new Dictionary<string, object?> { ["conf"] = "CPH", ["talk"] = "HITC" };
        Assert.True(filter.TryGetCanonicalSlug(routeValues, out _));
    }

    [Fact]
    public void ValidInboxFilter_InboxSlugKey_IsDefinedConst()
    {
        // Ensures downstream sections can reference the key without string literals.
        Assert.Equal("InboxSlug", ValidInboxFilter.InboxSlugKey);
    }
}
