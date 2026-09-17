using System.Net;
using System.Text.Json;
using FluentAssertions;
using Lutra.API.Middleware;
using Lutra.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lutra.API.IntegrationTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(HttpStatusCode Status, JsonElement Body)> InvokeAsync(Exception exception)
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw exception,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await JsonDocument.ParseAsync(context.Response.Body);

        return ((HttpStatusCode)context.Response.StatusCode, body.RootElement.Clone());
    }

    [Fact]
    public async Task InvokeAsync_ExternalServiceException_ReturnsGeneric502WithoutLeakingDetails()
    {
        var (status, body) = await InvokeAsync(
            new ExternalServiceException("Upstream host 'secret.internal' refused the connection."));

        status.Should().Be(HttpStatusCode.BadGateway);
        body.GetProperty("status").GetInt32().Should().Be(502);
        body.GetProperty("title").GetString().Should().Be("Bad Gateway");
        body.GetProperty("detail").GetString().Should().Be("De externe dienst gaf een foutmelding.");
        body.GetProperty("detail").GetString().Should().NotContain("secret.internal");
    }

    [Fact]
    public async Task InvokeAsync_TooManyRequestsException_Returns429WithMessage()
    {
        var (status, body) = await InvokeAsync(new TooManyRequestsException("Wachtrij vol."));

        status.Should().Be(HttpStatusCode.TooManyRequests);
        body.GetProperty("title").GetString().Should().Be("Too Many Requests");
        body.GetProperty("detail").GetString().Should().Be("Wachtrij vol.");
    }
}