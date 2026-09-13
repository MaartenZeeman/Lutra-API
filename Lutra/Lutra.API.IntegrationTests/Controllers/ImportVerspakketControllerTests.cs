using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lutra.API.IntegrationTests.Infrastructure;
using Lutra.Application.Verspakketten;
using Lutra.Domain.Entities;

namespace Lutra.API.IntegrationTests.Controllers;

public class ImportVerspakketControllerTests(LutraApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Post_ImportsNewVerspakket_AndSecondImportReturnsSameId()
    {
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(),
            Naam = "Albert Heijn",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        });

        var request = new { url = "https://www.ah.nl/product/123" };

        var firstResponse = await Client.PostAsJsonAsync("/api/verspakketten/import", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await firstResponse.Content.ReadFromJsonAsync<ImportVerspakket.Response>();
        created.Should().NotBeNull();
        created!.Created.Should().BeTrue();
        created.Id.Should().NotBeEmpty();

        var secondResponse = await Client.PostAsJsonAsync("/api/verspakketten/import", request);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var existing = await secondResponse.Content.ReadFromJsonAsync<ImportVerspakket.Response>();
        existing.Should().NotBeNull();
        existing!.Id.Should().Be(created.Id);
        existing.Created.Should().BeFalse();
    }

    [Fact]
    public async Task Post_InvalidUrl_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/verspakketten/import",
            new { url = "not-a-valid-url" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
