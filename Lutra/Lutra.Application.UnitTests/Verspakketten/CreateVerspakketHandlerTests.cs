using FluentAssertions;
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
    public async Task Handle_SupermarktNotFound_ThrowsInvalidOperationException()
    {
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(new List<Domain.Entities.Supermarkt>());

        var command = new CreateVerspakket.Command("Lente Pakket", 1299, 2, Guid.NewGuid(), null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
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
}
