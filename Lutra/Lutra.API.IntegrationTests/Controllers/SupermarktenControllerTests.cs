using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lutra.API.IntegrationTests.Infrastructure;
using Lutra.API.Requests;
using Lutra.Application.Supermarkten;
using Lutra.Domain.Entities;

namespace Lutra.API.IntegrationTests.Controllers;

public class SupermarktenControllerTests(LutraApiFactory factory)
    : IntegrationTestBase(factory)
{
    // ── GET /api/supermarkten ─────────────────────────────────────────────────

    [Fact]
    public async Task Get_ReturnsOk_WithEmptyList_WhenNoDataExists()
    {
        var response = await Client.GetAsync("/api/supermarkten");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSupermarkten.Response>();
        body.Should().NotBeNull();
        body!.Supermarkten.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ReturnsSeededSupermarkt()
    {
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Albert Heijn",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var response = await Client.GetAsync("/api/supermarkten");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSupermarkten.Response>();
        body!.Supermarkten.Should().HaveCount(1);
        body.Supermarkten.First().Naam.Should().Be("Albert Heijn");
    }

    [Fact]
    public async Task Get_ReturnsAllSeededSupermarkten()
    {
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Albert Heijn",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Jumbo",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Picnic",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var response = await Client.GetAsync("/api/supermarkten");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSupermarkten.Response>();
        body!.Supermarkten.Should().HaveCount(3);
    }

    // ── GET /api/supermarkten — pagination ────────────────────────────────────

    [Fact]
    public async Task Get_Pagination_ReturnsCorrectPage()
    {
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Albert Heijn",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Jumbo",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Picnic",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var response = await Client.GetAsync("/api/supermarkten?skip=1&take=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSupermarkten.Response>();
        body!.Supermarkten.Should().HaveCount(1);
        // Handler orders by Naam ascending, so skip=1 skips "Albert Heijn" and returns "Jumbo".
        body.Supermarkten.First().Naam.Should().Be("Jumbo");
    }

    [Fact]
    public async Task Get_Pagination_ReturnsEmptyList_WhenSkipExceedsTotalCount()
    {
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Albert Heijn",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var response = await Client.GetAsync("/api/supermarkten?skip=10&take=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSupermarkten.Response>();
        body!.Supermarkten.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_Pagination_ClampsTakeToMaxPageSize()
    {
        await SeedManyAsync(Enumerable.Range(0, 201).Select(i => new Supermarkt
        {
            Id = Guid.NewGuid(),
            Naam = $"Supermarkt {i:D3}",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        }));

        var response = await Client.GetAsync("/api/supermarkten?take=1000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSupermarkten.Response>();
        body!.Supermarkten.Should().HaveCount(200);
    }

    [Fact]
    public async Task Get_Pagination_NegativeSkipAndNonPositiveTake_AreClamped()
    {
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Albert Heijn",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Jumbo",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var response = await Client.GetAsync("/api/supermarkten?skip=-5&take=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSupermarkten.Response>();
        body!.Supermarkten.Should().ContainSingle().Which.Naam.Should().Be("Albert Heijn");
    }

    // ── GET /api/supermarkten — sorting ───────────────────────────────────────

    [Fact]
    public async Task Get_ReturnsSupermarkten_OrderedByNaamAscending()
    {
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Picnic",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Albert Heijn",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var response = await Client.GetAsync("/api/supermarkten");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSupermarkten.Response>();
        var namen = body!.Supermarkten.Select(s => s.Naam).ToList();
        namen.Should().BeInAscendingOrder();
    }

    // ── Soft-delete behaviour ─────────────────────────────────────────────────

    [Fact]
    public async Task Get_DoesNotReturn_SoftDeletedSupermarkt()
    {
        await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Verwijderd",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow,
            DeletedAt = DateTime.UtcNow
        });

        var response = await Client.GetAsync("/api/supermarkten");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetSupermarkten.Response>();
        body!.Supermarkten.Should().BeEmpty();
    }

    // ── POST /api/supermarkten ────────────────────────────────────────────────

    [Fact]
    public async Task Post_CreatesSupermarkt_AndReturns201()
    {
        var request = new SupermarktRequest("Jumbo");
        var response = await Client.PostAsJsonAsync("/api/supermarkten", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateSupermarkt.Response>();
        body!.Id.Should().NotBeEmpty();
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenNaamEmpty()
    {
        var request = new SupermarktRequest("");
        var response = await Client.PostAsJsonAsync("/api/supermarkten", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/supermarkten/{id} ────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsNoContent_WhenSupermarktExists()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Oud",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var request = new SupermarktRequest("Nieuw");
        var response = await Client.PutAsJsonAsync($"/api/supermarkten/{supermarkt.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenSupermarktDoesNotExist()
    {
        var request = new SupermarktRequest("Nieuw");
        var response = await Client.PutAsJsonAsync($"/api/supermarkten/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
