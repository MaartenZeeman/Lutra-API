using System.Net;
using FluentAssertions;
using Lutra.Application.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lutra.Infrastructure.OpenRouter.UnitTests;

public class OpenRouterVerspakketExtractorTests
{
    private static OpenRouterVerspakketExtractor CreateExtractor(
        HttpResponseMessage openRouterResponse,
        string retailHtml = "<html><body>Product</body></html>")
    {
        var factory = new StubHttpClientFactory(name => name == "OpenRouter"
            ? new HttpClient(new StubHttpMessageHandler(_ => openRouterResponse))
            : new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(retailHtml)
            })));

        var options = new OpenRouterOptions
        {
            ApiKey = "test-key",
            AllowedHosts = ["ah.nl"]
        };

        return new OpenRouterVerspakketExtractor(
            factory,
            Options.Create(options),
            NullLogger<OpenRouterVerspakketExtractor>.Instance);
    }

    [Fact]
    public async Task ExtractAsync_OpenRouterReturnsMalformedJson_ThrowsExternalServiceException()
    {
        var extractor = CreateExtractor(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ this is not json")
        });

        var act = () => extractor.ExtractAsync("https://www.ah.nl/product/1", CancellationToken.None);

        await act.Should().ThrowAsync<ExternalServiceException>();
    }

    [Fact]
    public async Task ExtractAsync_HostNotAllowed_ThrowsValidationException()
    {
        var extractor = CreateExtractor(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });

        var act = () => extractor.ExtractAsync("https://www.example.com/product/1", CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private sealed class StubHttpClientFactory(Func<string, HttpClient> factory) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => factory(name);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}