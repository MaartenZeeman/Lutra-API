namespace Lutra.Infrastructure.OpenRouter;

/// <summary>
/// Configuration for the OpenRouter-backed verspakket extractor.
/// </summary>
public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "nvidia/nemotron-3-super-120b-a12b:free";

    public string? HttpReferer { get; set; }

    public string AppTitle { get; set; } = "Lutra API";

    public int TimeoutSeconds { get; set; } = 120;

    public int MaxHtmlCharacters { get; set; } = 150_000;

    public long MaxHtmlBytes { get; set; } = 5_000_000;

    public int MaxImages { get; set; } = 10;

    public long MaxImageBytes { get; set; } = 5_242_880;

    public long MaxTotalImageBytes { get; set; } = 20_971_520;

    public int MaxRedirects { get; set; } = 5;

    public bool RequireParameters { get; set; } = true;

    /// <summary>Hosts whose product pages may be imported.</summary>
    public List<string> AllowedHosts { get; set; } =
    [
        "ah.nl",
        "allerhande.nl",
        "jumbo.com",
        "poiesz.nl",
        "lidl.nl"
    ];

    /// <summary>Additional hosts allowed to serve product images (CDNs).</summary>
    public List<string> AllowedImageHosts { get; set; } =
    [
        "ah.nl",
        "ahstatic.com",
        "jumbo.com",
        "jumbolocalcdn.nl",
        "lidl.nl",
        "lidl.com",
        "lidl-services.com",
        "poiesz.nl"
    ];
}
