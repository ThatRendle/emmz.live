namespace EmmzLive.Configuration;

/// <summary>
/// Parses and holds the set of valid inbox slugs from the INBOXES environment variable.
/// Slugs are matched case-insensitively (OrdinalIgnoreCase) because they originate from
/// URL paths and Railway/Docker env vars may be set in any case.
/// </summary>
public sealed class InboxConfig
{
    // Maps normalised-lower slug → configured slug (preserves the original casing from INBOXES).
    private readonly Dictionary<string, string> _canonical;

    public IReadOnlySet<string> Slugs { get; }

    /// <param name="rawInboxes">Raw value of the INBOXES env var, e.g. "cph/hitc,cph/rtbc".</param>
    public InboxConfig(string? rawInboxes)
    {
        var parsed = Parse(rawInboxes);
        if (parsed.Count == 0)
            throw new InvalidOperationException(
                "INBOXES is not configured or is empty. " +
                "Set it to a comma-separated list of conf/talk slugs, e.g. INBOXES=cph/hitc,cph/rtbc");

        Slugs = parsed;
        _canonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var slug in parsed)
            _canonical[slug] = slug;
    }

    /// <summary>Returns true when the given "conf/talk" slug is in the configured set.</summary>
    public bool IsValid(string slug) => TryGetCanonical(slug, out _);

    /// <summary>
    /// Returns true and sets <paramref name="canonical"/> to the slug exactly as it appears in the
    /// INBOXES configuration when the input matches case-insensitively. Returns false for unknown slugs.
    /// Downstream code (page handlers, DB lookups) must use the canonical form so that URL casing
    /// never produces distinct database rows for the same logical inbox.
    /// </summary>
    public bool TryGetCanonical(string slug, out string canonical)
    {
        if (_canonical.TryGetValue(slug, out var found))
        {
            canonical = found;
            return true;
        }
        canonical = string.Empty;
        return false;
    }

    /// <summary>
    /// Parses a raw INBOXES string into a set of normalised slugs.
    /// Public so unit tests can exercise parsing without depending on real env vars.
    /// </summary>
    public static IReadOnlySet<string> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in raw.Split(','))
        {
            var slug = entry.Trim();
            if (slug.Length > 0)
                set.Add(slug);
        }
        return set;
    }
}
