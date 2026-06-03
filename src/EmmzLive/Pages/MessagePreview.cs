namespace EmmzLive.Pages;

/// <summary>
/// Helpers for rendering message preview text in the inbox list.
/// </summary>
internal static class MessagePreview
{
    internal const int PreviewLength = 120;

    /// <summary>
    /// Returns at most <paramref name="maxLength"/> characters of <paramref name="body"/>,
    /// appending an ellipsis when truncated. The cut never lands between a UTF-16 surrogate
    /// pair, so emoji at the boundary are not split.
    /// </summary>
    internal static string Truncate(string body, int maxLength = PreviewLength)
    {
        if (body.Length <= maxLength)
            return body;

        // Walk back from the cut point if we would split a surrogate pair.
        var cut = maxLength;
        if (cut > 0 && char.IsLowSurrogate(body[cut]) && char.IsHighSurrogate(body[cut - 1]))
            cut--;

        return body[..cut] + "…";
    }
}
