using System.Text;

namespace Lutra.Application.Verspakketten;

/// <summary>
/// Produces a canonical form of a product URL so that the same product is not imported twice.
/// </summary>
public static class VerspakketUrlNormalizer
{
    private static readonly HashSet<string> TrackingParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "gclid", "fbclid", "mc_cid", "mc_eid", "_gl", "_ga", "msclkid",
        "igshid", "yclid", "dclid", "ref", "referrer"
    };

    private static readonly HashSet<string> TrackingPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "utm_"
    };

    public static bool TryNormalize(string? url, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var builder = new UriBuilder(uri)
        {
            Scheme = "https",
            Host = uri.Host.ToLowerInvariant(),
            Fragment = string.Empty
        };

        if (builder.Port == 443)
        {
            builder.Port = -1;
        }

        var path = builder.Path;
        if (path.Length > 1 && path.EndsWith('/'))
        {
            path = path.TrimEnd('/');
        }

        builder.Path = path;
        builder.Query = BuildQuery(uri.Query);

        normalized = builder.Uri.AbsoluteUri;
        return true;
    }

    private static string BuildQuery(string query)
    {
        if (string.IsNullOrEmpty(query) || query == "?")
        {
            return string.Empty;
        }

        var kept = new List<string>();
        var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var separatorIndex = pair.IndexOf('=');
            var name = separatorIndex < 0 ? pair : pair[..separatorIndex];

            if (TrackingParameters.Contains(name) || TrackingPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            kept.Add(pair);
        }

        if (kept.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder("?");
        builder.Append(string.Join('&', kept));
        return builder.ToString();
    }
}
