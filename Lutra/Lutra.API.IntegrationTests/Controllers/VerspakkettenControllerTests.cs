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
        var verspakket = new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Lente Pakket", AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        };
        verspakket.AddFoto(new VerspakketFoto
        {
            Id = Guid.NewGuid(),
            Data = [1, 2, 3],
            IsMainImage = true,
            VerspakketId = verspakket.Id,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        });
        verspakket.AddFoto(new VerspakketFoto
        {
            Id = Guid.NewGuid(),
            Data = [4, 5, 6],
            IsMainImage = false,
            VerspakketId = verspakket.Id,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(verspakket);

        var response = await Client.GetAsync("/api/verspakketten");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakketten.Response>();
        body!.Verspakketten.Should().HaveCount(1);
        body.Verspakketten.First().Naam.Should().Be("Lente Pakket");
        body.Verspakketten.First().Foto.Should().NotBeNull();
        body.Verspakketten.First().Foto!.IsMainImage.Should().BeTrue();
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

        var request = new CreateVerspakketRequest("Herfst Pakket", 1499, 3, supermarkt.Id);
        var response = await Client.PostAsJsonAsync("/api/verspakketten", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateVerspakket.Response>();
        body!.Id.Should().NotBeEmpty();
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_CreatesVerspakket_WithBeoordeling_AndReturns201()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Picnic",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var command = new CreateVerspakket.Command(
            "Herfst Pakket",
            1499,
            3,
            supermarkt.Id,
            new Lutra.Application.Models.Verspakketten.Beoordeling
            {
                CijferSmaak = 9,
                CijferBereiden = 8,
                Aanbevolen = true,
                Tekst = "Heel goed"
            });

        var response = await Client.PostAsJsonAsync("/api/verspakketten", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateVerspakket.Response>();
        body!.Id.Should().NotBeEmpty();

        var created = await Client.GetFromJsonAsync<GetVerspakket.Response>($"/api/verspakketten/{body.Id}");
        created!.Verspakket.Beoordelingen.Should().ContainSingle();
        created.Verspakket.Beoordelingen!.Single().CijferSmaak.Should().Be(9);
    }

    [Fact]
    public async Task Post_ReturnsNotFound_WhenSupermarktDoesNotExist()
    {
        var request = new CreateVerspakketRequest("Winter Pakket", 999, 2, Guid.NewGuid());
        var response = await Client.PostAsJsonAsync("/api/verspakketten", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
    public async Task Update_ReturnsNotFound_WhenSupermarktDoesNotExist()
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

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
    public async Task AddBeoordeling_ReturnsNotFound_WhenVerspakketDoesNotExist()
    {
        var command = new AddBeoordeling.Command(Guid.NewGuid(), 8, 7, true, "Heerlijk!");
        var response = await Client.PostAsJsonAsync($"/api/verspakketten/{command.VerspakketId}/beoordelingen", command);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    // ── Ingredienten ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_CreatesVerspakket_WithIngredienten()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Picnic",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var request = new CreateVerspakketRequest(
            "Pasta Bolognese",
            899,
            2,
            supermarkt.Id,
            Ingredienten:
            [
                new IngredientRequest("Tomaten", 400, Lutra.Domain.Entities.Eenheid.Gram, true),
                new IngredientRequest("Gehakt", 300, Lutra.Domain.Entities.Eenheid.Gram, false)
            ]);

        var response = await Client.PostAsJsonAsync("/api/verspakketten", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateVerspakket.Response>();

        var created = await Client.GetFromJsonAsync<GetVerspakket.Response>($"/api/verspakketten/{body!.Id}");
        created!.Verspakket.Ingredienten.Should().HaveCount(2);
        var tomaten = created.Verspakket.Ingredienten!.Single(i => i.Naam == "Tomaten");
        tomaten.Hoeveelheid.Should().Be(400);
        tomaten.Eenheid.Should().Be(Lutra.Domain.Entities.Eenheid.Gram);
        tomaten.Inbegrepen.Should().BeTrue();
        created.Verspakket.Ingredienten.Single(i => i.Naam == "Gehakt").Inbegrepen.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ReplacesIngredienten()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "AH",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        var verspakket = new Verspakket
        {
            Id = Guid.NewGuid(), Naam = "Pakket", AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        };
        verspakket.AddIngredient(new Lutra.Domain.Entities.Ingredient
        {
            Id = Guid.NewGuid(), Naam = "Oud", Hoeveelheid = 100,
            Eenheid = Lutra.Domain.Entities.Eenheid.Gram, Inbegrepen = true,
            VerspakketId = verspakket.Id,
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(verspakket);

        var request = new UpdateVerspakketRequest(
            "Pakket",
            999,
            2,
            supermarkt.Id,
            Ingredienten:
            [
                new IngredientRequest("Olijfolie", 1, Lutra.Domain.Entities.Eenheid.Eetlepel, false)
            ]);

        var response = await Client.PutAsJsonAsync($"/api/verspakketten/{verspakket.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var created = await Client.GetFromJsonAsync<GetVerspakket.Response>($"/api/verspakketten/{verspakket.Id}");
        created!.Verspakket.Ingredienten.Should().ContainSingle();
        created.Verspakket.Ingredienten!.Single().Naam.Should().Be("Olijfolie");
    }
}
