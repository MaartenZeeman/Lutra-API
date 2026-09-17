using FluentAssertions;
using Lutra.Infrastructure.OpenRouter;

namespace Lutra.Infrastructure.OpenRouter.UnitTests;

public class HtmlContentExtractorTests
{
    [Fact]
    public void Extract_KeepsStructuredDataAndVisibleText()
    {
        const string html = """
            <html><head>
              <script type="application/ld+json">{"name":"Pakket"}</script>
              <style>.a { color: red; }</style>
            </head><body>
              <!-- comment -->
              <p>Hallo   wereld</p>
            </body></html>
            """;

        var text = HtmlContentExtractor.Extract(html, 10_000, out _);

        text.Should().Contain("STRUCTURED_DATA:");
        text.Should().Contain("{\"name\":\"Pakket\"}");
        text.Should().Contain("VISIBLE_TEXT:");
        text.Should().Contain("Hallo wereld");
        text.Should().NotContain("color: red");
    }

    [Fact]
    public void Extract_TruncatesToMaxCharacters()
    {
        var html = "<p>" + new string('a', 500) + "</p>";

        var text = HtmlContentExtractor.Extract(html, 100, out _);

        text.Should().HaveLength(100);
    }

    [Fact]
    public void Extract_CollectsHttpImageCandidates_AndSkipsDataAndRelativeUris()
    {
        const string html = """
            <img src="https://cdn.ah.nl/a.png" />
            <img data-src="https://cdn.ah.nl/b.png" />
            <img src="data:image/png;base64,AAAA" />
            <img src="/relative/c.png" />
            """;

        HtmlContentExtractor.Extract(html, 10_000, out var candidates);

        candidates.Should().Contain("https://cdn.ah.nl/a.png");
        candidates.Should().Contain("https://cdn.ah.nl/b.png");
        candidates.Should().NotContain(c => c.StartsWith("data:", StringComparison.OrdinalIgnoreCase));
        candidates.Should().NotContain("/relative/c.png");
    }

    [Fact]
    public void Extract_CollectsFirstSrcSetCandidate()
    {
        const string html = """<img srcset="https://cdn.ah.nl/small.png 320w, https://cdn.ah.nl/large.png 1024w" />""";

        HtmlContentExtractor.Extract(html, 10_000, out var candidates);

        candidates.Should().Contain("https://cdn.ah.nl/small.png");
    }
}