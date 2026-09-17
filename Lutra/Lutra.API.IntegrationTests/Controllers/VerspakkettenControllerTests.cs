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
    // 1x1 white PNG as base64
    private const string ValidBase64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI6QAAAABJRU5ErkJggg==";

    // ── GET /api/verspakketten ────────────────────────────────────────────────

    [Fact]
    public async Task Get_ReturnsOk_WithEmptyList_WhenNoDataExists()
    {
        var response = await Client.GetAsync("/api/verspakketten", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakketten.Response>(TestContext.Current.CancellationToken);
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

        var response = await Client.GetAsync("/api/verspakketten", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakketten.Response>(TestContext.Current.CancellationToken);
        body!.Verspakketten.Should().HaveCount(1);
        body.Verspakketten.First().Naam.Should().Be("Lente Pakket");
        body.Verspakketten.First().Foto.Should().NotBeNull();
        body.Verspakketten.First().Foto!.IsMainImage.Should().BeTrue();
    }

    // ── GET /api/verspakketten/{id} ───────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenVerspakketDoesNotExist()
    {
        var response = await Client.GetAsync($"/api/verspakketten/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

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

        var response = await Client.GetAsync($"/api/verspakketten/{verspakket.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakket.Response>(TestContext.Current.CancellationToken);
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
        var response = await Client.PostAsJsonAsync("/api/verspakketten", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateVerspakket.Response>(TestContext.Current.CancellationToken);
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

        var response = await Client.PostAsJsonAsync("/api/verspakketten", command, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateVerspakket.Response>(TestContext.Current.CancellationToken);
        body!.Id.Should().NotBeEmpty();

        var created = await Client.GetFromJsonAsync<GetVerspakket.Response>($"/api/verspakketten/{body.Id}", TestContext.Current.CancellationToken);
        created!.Verspakket.Beoordelingen.Should().ContainSingle();
        created.Verspakket.Beoordelingen!.Single().CijferSmaak.Should().Be(9);
    }

    [Fact]
    public async Task Post_ReturnsNotFound_WhenSupermarktDoesNotExist()
    {
        var request = new CreateVerspakketRequest("Winter Pakket", 999, 2, Guid.NewGuid());
        var response = await Client.PostAsJsonAsync("/api/verspakketten", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenFotoBase64Invalid()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Picnic",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var request = new CreateVerspakketRequest(
            "Pakket",
            999,
            2,
            supermarkt.Id,
            Fotos: [new VerspakketFotoRequest("not-valid-base64!!", true)]);

        var response = await Client.PostAsJsonAsync("/api/verspakketten", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenTooManyFotos()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Picnic",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var fotos = Enumerable.Range(0, 11)
            .Select(_ => new VerspakketFotoRequest(ValidBase64Png, false))
            .ToList();

        var request = new CreateVerspakketRequest("Pakket", 999, 2, supermarkt.Id, Fotos: fotos);

        var response = await Client.PostAsJsonAsync("/api/verspakketten", request, TestContext.Current.CancellationToken);

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
        var response = await Client.PutAsJsonAsync($"/api/verspakketten/{verspakket.Id}", request, TestContext.Current.CancellationToken);

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
        var response = await Client.PutAsJsonAsync($"/api/verspakketten/{Guid.NewGuid()}", request, TestContext.Current.CancellationToken);

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
        var response = await Client.PutAsJsonAsync($"/api/verspakketten/{verspakket.Id}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenFotoBase64Invalid()
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

        var request = new UpdateVerspakketRequest(
            "Pakket",
            999,
            2,
            supermarkt.Id,
            Fotos: [new VerspakketFotoRequest("not-valid-base64!!", true)]);

        var response = await Client.PutAsJsonAsync($"/api/verspakketten/{verspakket.Id}", request, TestContext.Current.CancellationToken);

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

        var response = await Client.GetAsync("/api/verspakketten?skip=1&take=1", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakketten.Response>(TestContext.Current.CancellationToken);
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

        var response = await Client.GetAsync("/api/verspakketten?sortDirection=Descending", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakketten.Response>(TestContext.Current.CancellationToken);
        body!.Verspakketten.First().Naam.Should().Be("Zomerpakket");
        body.Verspakketten.Last().Naam.Should().Be("Aardappel Pakket");
    }

    [Fact]
    public async Task Get_Pagination_ClampsTakeToMaxPageSize()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "AH",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedManyAsync(Enumerable.Range(0, 201).Select(i => new Verspakket
        {
            Id = Guid.NewGuid(),
            Naam = $"Pakket {i:D3}",
            AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        }));

        var response = await Client.GetAsync("/api/verspakketten?take=1000", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakketten.Response>(TestContext.Current.CancellationToken);
        body!.Verspakketten.Should().HaveCount(200);
    }

    [Fact]
    public async Task Get_Pagination_NegativeSkipAndNonPositiveTake_AreClamped()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "AH",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });
        await SeedManyAsync(Enumerable.Range(0, 2).Select(i => new Verspakket
        {
            Id = Guid.NewGuid(),
            Naam = $"Pakket {i}",
            AantalPersonen = 2,
            SupermarktId = supermarkt.Id,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        }));

        var response = await Client.GetAsync("/api/verspakketten?skip=-5&take=0", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetVerspakketten.Response>(TestContext.Current.CancellationToken);
        body!.Verspakketten.Should().ContainSingle();
    }

    // ── POST /api/verspakketten/{id}/beoordelingen ────────────────────────────

    [Fact]
    public async Task AddBeoordeling_ReturnsNotFound_WhenVerspakketDoesNotExist()
    {
        var command = new AddBeoordeling.Command(Guid.NewGuid(), 8, 7, true, "Heerlijk!");
        var response = await Client.PostAsJsonAsync($"/api/verspakketten/{command.VerspakketId}/beoordelingen", command, TestContext.Current.CancellationToken);

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
        var response = await Client.PostAsJsonAsync($"/api/verspakketten/{verspakket.Id}/beoordelingen", command, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AddBeoordeling.Response>(TestContext.Current.CancellationToken);
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

        var response = await Client.PostAsJsonAsync("/api/verspakketten", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateVerspakket.Response>(TestContext.Current.CancellationToken);

        var created = await Client.GetFromJsonAsync<GetVerspakket.Response>($"/api/verspakketten/{body!.Id}", TestContext.Current.CancellationToken);
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

        var response = await Client.PutAsJsonAsync($"/api/verspakketten/{verspakket.Id}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var created = await Client.GetFromJsonAsync<GetVerspakket.Response>($"/api/verspakketten/{verspakket.Id}", TestContext.Current.CancellationToken);
        created!.Verspakket.Ingredienten.Should().ContainSingle();
        created.Verspakket.Ingredienten!.Single().Naam.Should().Be("Olijfolie");
    }

    // ── Voedingswaarde & allergenen ───────────────────────────────────────────

    [Fact]
    public async Task Post_CreatesVerspakket_WithVoedingswaardeEnAllergenen()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Picnic",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var request = new CreateVerspakketRequest(
            "Pasta Pesto",
            899,
            2,
            supermarkt.Id,
            Voedingswaarden:
            [
                new VoedingswaardeRequest(VoedingswaardeBasis.Per100Gram, 520, 124, 4.5m, 1.2m, 14, 2.1m, 2.4m, 5.8m, 0.35m),
                new VoedingswaardeRequest(VoedingswaardeBasis.PerPortie, 2723, 648, 21.9m, 3.8m, 76.0m, 15.4m, 7.5m, 33.1m, 1.94m)
            ],
            Allergenen: [Allergeen.Gluten, Allergeen.Melk, Allergeen.Pinda]);

        var response = await Client.PostAsJsonAsync("/api/verspakketten", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateVerspakket.Response>(TestContext.Current.CancellationToken);

        var created = await Client.GetFromJsonAsync<GetVerspakket.Response>($"/api/verspakketten/{body!.Id}", TestContext.Current.CancellationToken);
        created!.Verspakket.Voedingswaarden.Should().HaveCount(2);
        var per100Gram = created.Verspakket.Voedingswaarden!.Single(w => w.Basis == VoedingswaardeBasis.Per100Gram);
        per100Gram.EnergieKj.Should().Be(520);
        per100Gram.EnergieKcal.Should().Be(124);
        per100Gram.Vetten.Should().Be(4.5m);
        per100Gram.WaarvanVerzadigd.Should().Be(1.2m);
        per100Gram.Koolhydraten.Should().Be(14);
        per100Gram.WaarvanSuikers.Should().Be(2.1m);
        per100Gram.Vezels.Should().Be(2.4m);
        per100Gram.Eiwitten.Should().Be(5.8m);
        per100Gram.Zout.Should().Be(0.35m);
        var perPortie = created.Verspakket.Voedingswaarden.Single(w => w.Basis == VoedingswaardeBasis.PerPortie);
        perPortie.EnergieKj.Should().Be(2723);
        perPortie.EnergieKcal.Should().Be(648);
        perPortie.Vetten.Should().Be(21.9m);
        perPortie.WaarvanVerzadigd.Should().Be(3.8m);
        perPortie.Koolhydraten.Should().Be(76.0m);
        perPortie.WaarvanSuikers.Should().Be(15.4m);
        perPortie.Vezels.Should().Be(7.5m);
        perPortie.Eiwitten.Should().Be(33.1m);
        perPortie.Zout.Should().Be(1.94m);
        created.Verspakket.Allergenen.Should().BeEquivalentTo(new[]
        {
            Allergeen.Gluten,
            Allergeen.Melk,
            Allergeen.Pinda
        });
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenWaarvanVerzadigdGreaterThanVetten()
    {
        var supermarkt = await SeedAsync(new Supermarkt
        {
            Id = Guid.NewGuid(), Naam = "Picnic",
            CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        });

        var request = new CreateVerspakketRequest(
            "Pasta Pesto",
            899,
            2,
            supermarkt.Id,
            Voedingswaarden:
            [
                new VoedingswaardeRequest(VoedingswaardeBasis.Per100Gram, null, null, 2, 3, null, null, null, null, null)
            ]);

        var response = await Client.PostAsJsonAsync("/api/verspakketten", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ReplacesAllergenen_EnVoedingswaarden()
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
        verspakket.AddVoedingswaarde(new Voedingswaarde
        {
            Id = Guid.NewGuid(),
            Basis = VoedingswaardeBasis.Per100Gram,
            EnergieKj = 100,
            Vetten = 1,
            VerspakketId = verspakket.Id,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        });
        verspakket.AddAllergeen(new VerspakketAllergeen
        {
            Id = Guid.NewGuid(),
            Allergeen = Allergeen.Gluten,
            VerspakketId = verspakket.Id,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        });
        await SeedAsync(verspakket);

        var request = new UpdateVerspakketRequest(
            "Pakket",
            999,
            2,
            supermarkt.Id,
            Voedingswaarden:
            [
                new VoedingswaardeRequest(VoedingswaardeBasis.PerPortie, 2723, 648, null, null, null, null, null, null, null)
            ],
            Allergenen: [Allergeen.Melk]);

        var response = await Client.PutAsJsonAsync($"/api/verspakketten/{verspakket.Id}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var created = await Client.GetFromJsonAsync<GetVerspakket.Response>($"/api/verspakketten/{verspakket.Id}", TestContext.Current.CancellationToken);
        created!.Verspakket.Voedingswaarden.Should().ContainSingle();
        created.Verspakket.Voedingswaarden!.Single().Basis.Should().Be(VoedingswaardeBasis.PerPortie);
        created.Verspakket.Voedingswaarden.Single().EnergieKj.Should().Be(2723);
        created.Verspakket.Allergenen.Should().BeEquivalentTo(new[] { Allergeen.Melk });
    }
}
