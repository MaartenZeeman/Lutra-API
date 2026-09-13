using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Lutra.Infrastructure.OpenRouter;

/// <summary>
/// Reduces a product page to prompt-sized text while keeping embedded structured data
/// (JSON-LD and __NEXT_DATA__) that often holds the real product details.
/// Also collects candidate product image URLs from the raw markup.
/// </summary>
public static class HtmlContentExtractor
{
    private static readonly Regex ScriptBlockRegex = new(
        "<script\\b[^>]*>(.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex JsonLdRegex = new(
        "<script\\b[^>]*type\\s*=\\s*[\"']application/ld\\+json[\"'][^>]*>(.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex NextDataRegex = new(
        "<script\\b[^>]*id\\s*=\\s*[\"']__NEXT_DATA__[\"'][^>]*>(.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex StyleOrNoiseRegex = new(
        "<style\\b[^>]*>.*?</style>|<noscript\\b[^>]*>.*?</noscript>|<svg\\b[^>]*>.*?</svg>|<!--.*?-->",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);

    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

    private static readonly Regex ImageUrlRegex = new(
        "[\"'](?:src|content|data-src|data-original|url)[\"']\\s*:\\s*[\"']([^\"']+)[\"']|(?:src|content|data-src|data-original)\\s*=\\s*[\"']([^\"']+)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SrcSetRegex = new(
        "srcset\\s*=\\s*[\"']([^\"']+)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Extract(string html, int maxCharacters, out List<string> imageCandidates)
    {
        imageCandidates = CollectImageCandidates(html);

        var structuredBlocks = new StringBuilder();

        foreach (Match match in JsonLdRegex.Matches(html))
        {
            structuredBlocks.AppendLine(match.Groups[1].Value);
        }

        foreach (Match match in NextDataRegex.Matches(html))
        {
            structuredBlocks.AppendLine(match.Groups[1].Value);
        }

        var withoutScripts = ScriptBlockRegex.Replace(html, " ");
        var withoutNoise = StyleOrNoiseRegex.Replace(withoutScripts, " ");
        var withoutTags = TagRegex.Replace(withoutNoise, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        var collapsed = WhitespaceRegex.Replace(decoded, " ");

        var combined = new StringBuilder(structuredBlocks.Length + collapsed.Length + 8);
        combined.AppendLine("STRUCTURED_DATA:");
        combined.AppendLine(structuredBlocks.ToString());
        combined.AppendLine("VISIBLE_TEXT:");
        combined.Append(collapsed);

        var result = combined.ToString();
        return result.Length > maxCharacters ? result[..maxCharacters] : result;
    }

    private static List<string> CollectImageCandidates(string html)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in ImageUrlRegex.Matches(html))
        {
            var value = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            AddCandidate(value, candidates, seen);
        }

        foreach (Match match in SrcSetRegex.Matches(html))
        {
            var first = match.Groups[1].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (first is not null)
            {
                AddCandidate(first.Split(' ')[0], candidates, seen);
            }
        }

        return candidates;
    }

    private static void AddCandidate(string value, List<string> candidates, HashSet<string> seen)
    {
        var candidate = WebUtility.HtmlDecode(value).Trim();

        if (candidate.Length == 0 || candidate.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!candidate.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (seen.Add(candidate))
        {
            candidates.Add(candidate);
        }
    }
}
