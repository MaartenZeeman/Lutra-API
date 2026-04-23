using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lutra.API.IntegrationTests.Infrastructure;
using Lutra.API.Requests;
using Lutra.Application.Verspakketten;
using Lutra.Domain.Entities;

namespace Lutra.API.IntegrationTests.Controllers;

public class VerspakkettenControllerTests(LutraApiFactory factory)
    : IntegrationTestBase(factory)
{
    // ── GET /api/verspakketten ────────────────────────────────────────────────

    [Fact]
    public async Task Get_ReturnsOk_WithEmptyList_WhenNoDataExists()
    {
        var response = await Client.GetAsync("/api/verspakketten");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakketten.Response>();
        body.Should().NotBeNull();
        body!.Verspakketten.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ReturnsSeededVerspakket()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "AH",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Lente Pakket", AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var response = await Client.GetAsync("/api/verspakketten");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakketten.Response>();
        body!.Verspakketten.Should().HaveCount(1);
        body.Verspakketten.First().Naam.Should().Be("Lente Pakket");
    }

    // ── GET /api/verspakketten/{id} ───────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenVerspakketDoesNotExist()
    {
        var response = await Client.GetAsync($"/api/verspakketten/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ReturnsVerspakket_WhenItExists()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Jumbo",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        var verspakket = await SeedAsync(new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Zomer Pakket", AantalPersonen = 4,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var response = await Client.GetAsync($"/api/verspakketten/{verspakket.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakket.Response>();
        body!.Verspakket.Should().NotBeNull();
        body.Verspakket!.Naam.Should().Be("Zomer Pakket");
    }

    // ── POST /api/verspakketten ───────────────────────────────────────────────

    [Fact]
    public async Task Post_CreatesVerspakket_AndReturns201()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Picnic",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var command = new CreateVerspakket.Command("Herfst Pakket", 1499, 3, supermarkt.Id);
        var response = await Client.PostAsJsonAsync("/api/verspakketten", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateVerspakket.Response>();
        body!.Id.Should().NotBeEmpty();
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenSupermarktDoesNotExist()
    {
        var command = new CreateVerspakket.Command("Winter Pakket", 999, 2, Guid.NewGuid());
        var response = await Client.PostAsJsonAsync("/api/verspakketten", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/verspakketten/{id} ───────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsNoContent_WhenVerspakketExists()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "AH",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        var verspakket = await SeedAsync(new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Oud Pakket", AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var request = new UpdateVerspakketRequest("Nieuw Pakket", 1999, 3, supermarkt.Id);
        var response = await Client.PutAsJsonAsync($"/api/verspakketten/{verspakket.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenVerspakketDoesNotExist()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "AH",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var request = new UpdateVerspakketRequest("Pakket", 999, 2, supermarkt.Id);
        var response = await Client.PutAsJsonAsync($"/api/verspakketten/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenSupermarktDoesNotExist()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "AH",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        var verspakket = await SeedAsync(new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Pakket", AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var request = new UpdateVerspakketRequest("Pakket", 999, 2, Guid.NewGuid());
        var response = await Client.PutAsJsonAsync($"/api/verspakketten/{verspakket.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/verspakketten — pagination & sorting ─────────────────────────

    [Fact]
    public async Task Get_Pagination_ReturnsCorrectPage()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "AH",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Aardappel Pakket", AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Broccoli Pakket", AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Courgette Pakket", AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var response = await Client.GetAsync("/api/verspakketten?skip=1&take=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakketten.Response>();
        body!.Verspakketten.Should().HaveCount(1);
        body.Verspakketten.First().Naam.Should().Be("Broccoli Pakket");
    }

    [Fact]
    public async Task Get_SortDescending_ReturnsItemsInReverseOrder()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "AH",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Aardappel Pakket", AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Zomerpakket", AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var response = await Client.GetAsync("/api/verspakketten?sortDirection=Descending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakketten.Response>();
        body!.Verspakketten.First().Naam.Should().Be("Zomerpakket");
        body.Verspakketten.Last().Naam.Should().Be("Aardappel Pakket");
    }

    // ── POST /api/verspakketten/{id}/beoordelingen ────────────────────────────

    [Fact]
    public async Task AddBeoordeling_ReturnsBadRequest_WhenVerspakketDoesNotExist()
    {
        var command = new AddBeoordeling.Command(Guid.NewGuid(), 8, 7, true, "Heerlijk!");
        var response = await Client.PostAsJsonAsync($"/api/verspakketten/{command.VerspakketId}/beoordelingen", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddBeoordeling_Returns201_WhenVerspakketExists()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Lidl",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        var verspakket = await SeedAsync(new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Basis Pakket", AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var command = new AddBeoordeling.Command(verspakket.Id, 8, 7, true, "Heerlijk!");
        var response = await Client.PostAsJsonAsync($"/api/verspakketten/{verspakket.Id}/beoordelingen", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AddBeoordeling.Response>();
        body!.Id.Should().NotBeEmpty();
    }
}
