using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lutra.API.IntegrationTests.Infrastructure;
using Lutra.Application.BackgroundCommands;
using Lutra.Application.Verspakketten;
using Lutra.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Lutra.API.IntegrationTests.Controllers;

public class ImportVerspakketControllerTests(LutraApiFactory factory) : IntegrationTestBase(factory)
{
    private async Task SeedAlbertHeijnAsync()
    {
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(),
            Naam = "Albert Heijn",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        });
    }

    private async Task<bool> ProcessOneJobAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<BackgroundCommandProcessor>();
        return await processor.ProcessNextAsync(CancellationToken.None);
    }

    private async Task<EnqueueImportVerspakket.Response> QueueAsync(string url)
    {
        var response = await Client.PostAsJsonAsync("/api/verspakketten/import", new { url });
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<EnqueueImportVerspakket.Response>();
        body.Should().NotBeNull();
        return body!;
    }

    private async Task<GetBackgroundCommand.Response> GetStatusAsync(Guid id)
    {
        var response = await Client.GetAsync($"/api/background-commands/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetBackgroundCommand.Response>();
        body.Should().NotBeNull();
        return body!;
    }

    [Fact]
    public async Task Post_QueuesJob_ThenProcessingSucceeds()
    {
        await SeedAlbertHeijnAsync();

        var job = await QueueAsync("https://www.ah.nl/product/123");
        job.Status.Should().Be(BackgroundCommandStatus.Queued);
        job.AttemptCount.Should().Be(0);

        (await ProcessOneJobAsync()).Should().BeTrue();

        var status = await GetStatusAsync(job.Id);
        status.Status.Should().Be(BackgroundCommandStatus.Succeeded);
        status.AttemptCount.Should().Be(1);
        status.ResultVerspakketId.Should().NotBeNull();
        status.LastError.Should().BeNull();

        var verspakketResponse = await Client.GetAsync($"/api/verspakketten/{status.ResultVerspakketId}");
        verspakketResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_SameUrlWhileActive_ReturnsSameJob()
    {
        await SeedAlbertHeijnAsync();

        var first = await QueueAsync("https://www.ah.nl/product/456");
        var second = await QueueAsync("https://www.ah.nl/product/456?utm_source=test");

        second.Id.Should().Be(first.Id);
        second.Existing.Should().BeTrue();
    }

    [Fact]
    public async Task Post_TransientFailures_RetryThenFailAfterMaxAttempts()
    {
        var job = await QueueAsync("https://www.ah.nl/transient-fail");

        (await ProcessOneJobAsync()).Should().BeTrue();
        (await GetStatusAsync(job.Id)).Status.Should().Be(BackgroundCommandStatus.RetryScheduled);
        (await GetStatusAsync(job.Id)).AttemptCount.Should().Be(1);

        (await ProcessOneJobAsync()).Should().BeTrue();
        (await GetStatusAsync(job.Id)).Status.Should().Be(BackgroundCommandStatus.RetryScheduled);
        (await GetStatusAsync(job.Id)).AttemptCount.Should().Be(2);

        (await ProcessOneJobAsync()).Should().BeTrue();
        var terminal = await GetStatusAsync(job.Id);
        terminal.Status.Should().Be(BackgroundCommandStatus.Failed);
        terminal.AttemptCount.Should().Be(3);
        terminal.CompletedAt.Should().NotBeNull();
        terminal.LastError.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_PermanentFailure_FailsImmediatelyWithoutRetry()
    {
        var job = await QueueAsync("https://www.ah.nl/permanent-fail");

        (await ProcessOneJobAsync()).Should().BeTrue();

        var status = await GetStatusAsync(job.Id);
        status.Status.Should().Be(BackgroundCommandStatus.Failed);
        status.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task Post_InvalidUrl_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/verspakketten/import",
            new { url = "not-a-valid-url" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetStatus_UnknownId_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/background-commands/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}