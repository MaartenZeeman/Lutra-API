using FluentAssertions;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Verspakketten;
using Lutra.Application.Verspakketten;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Verspakketten;

public class CreateVerspakketHandlerTests
{
    private readonly Mock<ILutraDbContext> _contextMock;
    private readonly CreateVerspakket.Handler _handler;

    public CreateVerspakketHandlerTests()
    {
        _contextMock = new Mock<ILutraDbContext>();
        _handler = new CreateVerspakket.Handler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_SupermarktExists_CreatesVerspakket()
    {
        var supermarktId = Guid.NewGuid();
        var supermarkten = new List<Domain.Entities.Supermarkt>
        {
            new() { Id = supermarktId, Naam = "AH", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        };

        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(supermarkten);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateVerspakket.Command("Lente Pakket", 1299, 2, supermarktId, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SupermarktNotFound_ThrowsNotFoundException()
    {
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(new List<Domain.Entities.Supermarkt>());

        var command = new CreateVerspakket.Command("Lente Pakket", 1299, 2, Guid.NewGuid(), null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*was not found*");
    }

    [Fact]
    public async Task Handle_WithIngredienten_CreatesVerspakketWithIngredienten()
    {
        var supermarktId = Guid.NewGuid();
        var supermarkten = new List<Domain.Entities.Supermarkt>
        {
            new() { Id = supermarktId, Naam = "Jumbo", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        };

        Domain.Entities.Verspakket? savedVerspakket = null;
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(supermarkten);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock
            .Setup(c => c.Verspaketten.AddAsync(It.IsAny<Domain.Entities.Verspakket>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Verspakket, CancellationToken>((v, _) => savedVerspakket = v);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateVerspakket.Command(
            "Pasta Bolognese",
            899,
            2,
            supermarktId,
            null,
            null,
            new List<Ingredient>
            {
                new("Tomaten", 400, Domain.Entities.Eenheid.Gram, true),
                new("Gehakt", 300, Domain.Entities.Eenheid.Gram, false)
            });

        await _handler.Handle(command, CancellationToken.None);

        savedVerspakket.Should().NotBeNull();
        savedVerspakket!.Ingredienten.Should().HaveCount(2);
        var tomaten = savedVerspakket.Ingredienten.Single(i => i.Naam == "Tomaten");
        tomaten.Hoeveelheid.Should().Be(400);
        tomaten.Eenheid.Should().Be(Domain.Entities.Eenheid.Gram);
        tomaten.Inbegrepen.Should().BeTrue();
        savedVerspakket.Ingredienten.Single(i => i.Naam == "Gehakt").Inbegrepen.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CreatesVerspakketWithCorrectProperties()
    {
        var supermarktId = Guid.NewGuid();
        var supermarkten = new List<Domain.Entities.Supermarkt>
        {
            new() { Id = supermarktId, Naam = "Jumbo", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        };

        Domain.Entities.Verspakket? savedVerspakket = null;
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(supermarkten);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock
            .Setup(c => c.Verspaketten.AddAsync(It.IsAny<Domain.Entities.Verspakket>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Verspakket, CancellationToken>((v, _) => savedVerspakket = v);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateVerspakket.Command("Zomer Pakket", 999, 4, supermarktId, null);

        await _handler.Handle(command, CancellationToken.None);

        savedVerspakket.Should().NotBeNull();
        savedVerspakket!.Naam.Should().Be("Zomer Pakket");
        savedVerspakket.PrijsInCenten.Should().Be(999);
        savedVerspakket.AantalPersonen.Should().Be(4);
        savedVerspakket.SupermarktId.Should().Be(supermarktId);
    }

    [Fact]
    public async Task Handle_WithBeoordeling_CreatesVerspakketWithBeoordeling()
    {
        var supermarktId = Guid.NewGuid();
        var supermarkten = new List<Domain.Entities.Supermarkt>
        {
            new() { Id = supermarktId, Naam = "Jumbo", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        };

        Domain.Entities.Verspakket? savedVerspakket = null;
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(supermarkten);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock
            .Setup(c => c.Verspaketten.AddAsync(It.IsAny<Domain.Entities.Verspakket>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Verspakket, CancellationToken>((v, _) => savedVerspakket = v);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateVerspakket.Command(
            "Zomer Pakket",
            999,
            4,
            supermarktId,
            new Beoordeling
            {
                CijferSmaak = 8,
                CijferBereiden = 7,
                Aanbevolen = true,
                Tekst = "Lekker"
            });

        await _handler.Handle(command, CancellationToken.None);

        savedVerspakket.Should().NotBeNull();
        savedVerspakket!.Beoordelingen.Should().ContainSingle();
        var beoordeling = savedVerspakket.Beoordelingen.Single();
        beoordeling.CijferSmaak.Should().Be(8);
        beoordeling.CijferBereiden.Should().Be(7);
        beoordeling.Aanbevolen.Should().BeTrue();
        beoordeling.Tekst.Should().Be("Lekker");
    }

    [Fact]
    public async Task Handle_WithVoedingswaardeEnAllergenen_CreatesVerspakketWithThem()
    {
        var supermarktId = Guid.NewGuid();
        var supermarkten = new List<Domain.Entities.Supermarkt>
        {
            new() { Id = supermarktId, Naam = "Jumbo", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        };

        Domain.Entities.Verspakket? savedVerspakket = null;
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(supermarkten);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock
            .Setup(c => c.Verspaketten.AddAsync(It.IsAny<Domain.Entities.Verspakket>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Verspakket, CancellationToken>((v, _) => savedVerspakket = v);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateVerspakket.Command(
            "Pasta Pesto",
            899,
            2,
            supermarktId,
            null,
            Voedingswaarden:
            [
                new Voedingswaarde
                {
                    Basis = Domain.Entities.VoedingswaardeBasis.Per100Gram,
                    EnergieKj = 520,
                    EnergieKcal = 124,
                    Vetten = 4.5m,
                    WaarvanVerzadigd = 1.2m,
                    Koolhydraten = 14,
                    WaarvanSuikers = 2.1m,
                    Vezels = 2.4m,
                    Eiwitten = 5.8m,
                    Zout = 0.35m
                },
                new Voedingswaarde
                {
                    Basis = Domain.Entities.VoedingswaardeBasis.PerPortie,
                    EnergieKj = 2723,
                    EnergieKcal = 648,
                    Vetten = 21.9m,
                    WaarvanVerzadigd = 3.8m,
                    Koolhydraten = 76.0m,
                    WaarvanSuikers = 15.4m,
                    Vezels = 7.5m,
                    Eiwitten = 33.1m,
                    Zout = 1.94m
                }
            ],
            Allergenen: [Domain.Entities.Allergeen.Gluten, Domain.Entities.Allergeen.Melk, Domain.Entities.Allergeen.Pinda]);

        await _handler.Handle(command, CancellationToken.None);

        savedVerspakket.Should().NotBeNull();
        savedVerspakket!.Voedingswaarden.Should().HaveCount(2);
        var per100Gram = savedVerspakket.Voedingswaarden.Single(w => w.Basis == Domain.Entities.VoedingswaardeBasis.Per100Gram);
        per100Gram.EnergieKj.Should().Be(520);
        per100Gram.EnergieKcal.Should().Be(124);
        per100Gram.Vetten.Should().Be(4.5m);
        per100Gram.WaarvanVerzadigd.Should().Be(1.2m);
        per100Gram.Koolhydraten.Should().Be(14);
        per100Gram.WaarvanSuikers.Should().Be(2.1m);
        per100Gram.Vezels.Should().Be(2.4m);
        per100Gram.Eiwitten.Should().Be(5.8m);
        per100Gram.Zout.Should().Be(0.35m);
        var perPortie = savedVerspakket.Voedingswaarden.Single(w => w.Basis == Domain.Entities.VoedingswaardeBasis.PerPortie);
        perPortie.EnergieKj.Should().Be(2723);
        perPortie.Eiwitten.Should().Be(33.1m);
        perPortie.Zout.Should().Be(1.94m);
        savedVerspakket.Allergenen.Select(a => a.Allergeen).Should().BeEquivalentTo(new[]
        {
            Domain.Entities.Allergeen.Gluten,
            Domain.Entities.Allergeen.Melk,
            Domain.Entities.Allergeen.Pinda
        });
    }

    [Fact]
    public async Task Handle_WaarvanVerzadigdGreaterThanVetten_ThrowsValidationException()
    {
        var supermarktId = Guid.NewGuid();
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(
        [
            new() { Id = supermarktId, Naam = "Jumbo", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        ]);

        var command = new CreateVerspakket.Command(
            "Pasta Pesto",
            899,
            2,
            supermarktId,
            null,
            Voedingswaarden:
            [
                new Voedingswaarde
                {
                    Basis = Domain.Entities.VoedingswaardeBasis.Per100Gram,
                    Vetten = 2,
                    WaarvanVerzadigd = 3
                }
            ]);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("WaarvanVerzadigd mag niet groter zijn dan Vetten.");
    }

    [Fact]
    public async Task Handle_WaarvanSuikersGreaterThanKoolhydraten_ThrowsValidationException()
    {
        var supermarktId = Guid.NewGuid();
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(
        [
            new() { Id = supermarktId, Naam = "Jumbo", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        ]);

        var command = new CreateVerspakket.Command(
            "Pasta Pesto",
            899,
            2,
            supermarktId,
            null,
            Voedingswaarden:
            [
                new Voedingswaarde
                {
                    Basis = Domain.Entities.VoedingswaardeBasis.Per100Gram,
                    Koolhydraten = 1,
                    WaarvanSuikers = 2
                }
            ]);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("WaarvanSuikers mag niet groter zijn dan Koolhydraten.");
    }

    [Fact]
    public async Task Handle_WithDuplicateVoedingswaardeBasis_ThrowsValidationException()
    {
        var supermarktId = Guid.NewGuid();
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(
        [
            new() { Id = supermarktId, Naam = "Jumbo", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        ]);

        var command = new CreateVerspakket.Command(
            "Pasta Pesto",
            899,
            2,
            supermarktId,
            null,
            Voedingswaarden:
            [
                new Voedingswaarde { Basis = Domain.Entities.VoedingswaardeBasis.Per100Gram, EnergieKj = 1 },
                new Voedingswaarde { Basis = Domain.Entities.VoedingswaardeBasis.Per100Gram, EnergieKj = 2 }
            ]);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Voedingswaarden mogen per basis maar één keer voorkomen.");
    }

    [Fact]
    public async Task Handle_WithDuplicateAllergenen_ThrowsValidationException()
    {
        var supermarktId = Guid.NewGuid();
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(
        [
            new() { Id = supermarktId, Naam = "Jumbo", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        ]);

        var command = new CreateVerspakket.Command(
            "Pasta Pesto",
            899,
            2,
            supermarktId,
            null,
            Allergenen: [Domain.Entities.Allergeen.Melk, Domain.Entities.Allergeen.Melk]);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Allergenen mogen niet dubbel voorkomen.");
    }
}
