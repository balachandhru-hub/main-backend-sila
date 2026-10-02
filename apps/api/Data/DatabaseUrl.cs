using Npgsql;

namespace SilaMe.Api.Data;

public static class DatabaseUrl
{
    public static string Resolve(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Trim();
            }
        }

        throw new InvalidOperationException("DATABASE_URL or ConnectionStrings:DefaultConnection must be configured.");
    }

    public static string Normalize(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = ResolveSslMode(uri),
        };

        return builder.ConnectionString;
    }

    internal static SslMode ResolveSslMode(Uri uri)
    {
        var sslMode = ReadQueryValue(uri.Query, "sslmode");
        if (!string.IsNullOrWhiteSpace(sslMode))
        {
            return sslMode.Trim().ToLowerInvariant() switch
            {
                "disable" => SslMode.Disable,
                "allow" => SslMode.Prefer,
                "prefer" => SslMode.Prefer,
                "require" => SslMode.Require,
                "verify-ca" => SslMode.VerifyCA,
                "verify-full" => SslMode.VerifyFull,
                _ => SslMode.Require,
            };
        }

        var isLoopback = uri.Host is "localhost" or "127.0.0.1" or "::1" or "[::1]";
        return isLoopback ? SslMode.Disable : SslMode.Require;
    }

    private static string? ReadQueryValue(string query, string key)
    {
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 0)
            {
                continue;
            }

            if (string.Equals(Uri.UnescapeDataString(pair[0]), key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Length > 1 ? Uri.UnescapeDataString(pair[1].Replace('+', ' ')) : string.Empty;
            }
        }

        return null;
    }
}
