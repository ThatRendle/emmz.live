namespace EmmzLive.Infrastructure;

public static class ConnectionStringHelper
{
    // Maps URL sslmode values to Npgsql "SSL Mode" keyword values.
    // Npgsql accepts the same names as the libpq sslmode values (case-insensitive on read,
    // but we normalise to the conventional capitalised form for readability in logs/traces).
    private static readonly Dictionary<string, string> SslModeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["disable"] = "Disable",
        ["require"] = "Require",
        ["prefer"] = "Prefer",
        ["verify-ca"] = "VerifyCA",
        ["verify-full"] = "VerifyFull",
    };

    /// <summary>
    /// Accepts a postgres:// or postgresql:// URL and converts it to Npgsql keyword=value form,
    /// translating recognised query parameters (at minimum: sslmode).
    /// Input already in keyword=value form (i.e. not starting with postgres[ql]://) is returned unchanged.
    /// </summary>
    public static string ToNpgsqlConnectionString(string databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
            throw new ArgumentException("Database URL must not be empty.", nameof(databaseUrl));

        // Detect URL form by scheme, not by character heuristics.
        if (!databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return databaseUrl;
        }

        var uri = new Uri(databaseUrl);

        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        var parts = new List<string>
        {
            $"Host={host}",
            $"Port={port}",
            $"Database={database}",
            $"Username={username}",
            $"Password={password}",
        };

        // Parse query string and translate recognised parameters.
        if (!string.IsNullOrEmpty(uri.Query))
        {
            // Strip leading '?' and split into key=value pairs.
            var queryPairs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in queryPairs)
            {
                var eq = pair.IndexOf('=');
                if (eq < 0) continue;
                var key = Uri.UnescapeDataString(pair[..eq]);
                var value = Uri.UnescapeDataString(pair[(eq + 1)..]);

                if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                {
                    if (SslModeMap.TryGetValue(value, out var npgsqlSslMode))
                        parts.Add($"SSL Mode={npgsqlSslMode}");
                    // Unrecognised sslmode values are silently dropped rather than forwarding
                    // a potentially malformed value to Npgsql.
                }
            }
        }

        return string.Join(";", parts);
    }
}
