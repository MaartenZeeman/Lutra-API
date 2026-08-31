using FluentAssertions;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Verspakketten;
using Lutra.Application.Verspakketten;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Verspakketten;

public class UpdateVerspakketHandlerTests
{
    private readonly Mock<ILutraDbContext> _contextMock;
    private readonly UpdateVerspakket.Handler _handler;
    private Domain.Entities.Verspakket _verspakket = null!;

    private const string ValidBase64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI6QAAAABJRU5ErkJggg==";

    public UpdateVerspakketHandlerTests()
    {
        _contextMock = new Mock<ILutraDbContext>();
        _handler = new UpdateVerspakket.Handler(_contextMock.Object);
    }

    private (Guid verspakketId, Guid supermarktId) SetupContext(
        List<Domain.Entities.VerspakketFoto>? existingFotos = null,
        List<Domain.Entities.Ingredient>? existingIngredienten = null,
        Domain.Entities.Voedingswaarde? existingVoedingswaarde = null,
        List<Domain.Entities.VerspakketAllergeen>? existingAllergenen = null)
    {
        var supermarktId = Guid.NewGuid();
        var verspakketId = Guid.NewGuid();

        var supermarkten = new List<Domain.Entities.Supermarkt>
        {
            new() { Id = supermarktId, Naam = "AH", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        };

        _verspakket = new Domain.Entities.Verspakket
        {
            Id = verspakketId,
            Naam = "Oud Pakket",
            PrijsInCenten = 500,
            AantalPersonen = 2,
            SupermarktId = supermarktId,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        foreach (var foto in existingFotos ?? [])
            _verspakket.AddFoto(foto);

        foreach (var ingredient in existingIngredienten ?? [])
            _verspakket.AddIngredient(ingredient);

        foreach (var allergeen in existingAllergenen ?? [])
            _verspakket.AddAllergeen(allergeen);

        _verspakket.Voedingswaarde = existingVoedingswaarde;

        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(supermarkten);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket> { _verspakket });
        _contextMock.Setup(c => c.VerspakketFotos).ReturnsDbSet(existingFotos ?? []);
        _contextMock.Setup(c => c.Ingredienten).ReturnsDbSet(existingIngredienten ?? []);
        var voedingswaarden = new List<Domain.Entities.Voedingswaarde>();
        if (existingVoedingswaarde is not null)
            voedingswaarden.Add(existingVoedingswaarde);
        _contextMock.Setup(c => c.Voedingswaarden).ReturnsDbSet(voedingswaarden);
        _contextMock.Setup(c => c.VerspakketAllergenen).ReturnsDbSet(existingAllergenen ?? []);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return (verspakketId, supermarktId);
    }

    [Fact]
    public async Task Handle_WithoutFotos_UpdatesFieldsOnly()
    {
        var (verspakketId, supermarktId) = SetupContext();

        var command = new UpdateVerspakket.Command(verspakketId, "Nieuw Pakket", 1200, 4, supermarktId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFotos_ReplacesFotos()
    {
        var oldFotoId = Guid.NewGuid();
        var existingFotos = new List<Domain.Entities.VerspakketFoto>
        {
            new()
            {
                Id = oldFotoId,
                Data = [0x00],
                IsMainImage = true,
                VerspakketId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }
        };

        var (verspakketId, supermarktId) = SetupContext(existingFotos);

        var newFotos = new List<VerspakketFoto>
        {
            new(ValidBase64Png, IsMainImage: true),
            new(ValidBase64Png, IsMainImage: false)
        };

        var command = new UpdateVerspakket.Command(verspakketId, "Pakket", 999, 2, supermarktId, newFotos);

        await _handler.Handle(command, CancellationToken.None);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullFotos_DoesNotTouchFotos()
    {
        var (verspakketId, supermarktId) = SetupContext();

        var command = new UpdateVerspakket.Command(verspakketId, "Pakket", 800, 3, supermarktId, null);

        await _handler.Handle(command, CancellationToken.None);

        // VerspakketFotos.RemoveRange should NOT be called when Fotos is null
        _contextMock.Verify(
            c => c.VerspakketFotos.RemoveRange(It.IsAny<IEnumerable<Domain.Entities.VerspakketFoto>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_VerspakketNotFound_ThrowsNotFoundException()
    {
        var supermarktId = Guid.NewGuid();
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(new List<Domain.Entities.Supermarkt>());

        var command = new UpdateVerspakket.Command(Guid.NewGuid(), "Pakket", 800, 2, supermarktId);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*was not found*");
    }

    [Fact]
    public async Task Handle_InvalidAantalPersonen_ThrowsArgumentException()
    {
        var (verspakketId, supermarktId) = SetupContext();

        var command = new UpdateVerspakket.Command(verspakketId, "Pakket", 800, 0, supermarktId);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WithIngredienten_ReplacesIngredients()
    {
        var oldIngredient = new Domain.Entities.Ingredient
        {
            Id = Guid.NewGuid(),
            Naam = "Oud",
            Hoeveelheid = 100,
            Eenheid = Domain.Entities.Eenheid.Gram,
            Inbegrepen = true,
            VerspakketId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        var (verspakketId, supermarktId) = SetupContext(existingIngredienten: [oldIngredient]);
        oldIngredient.VerspakketId = verspakketId;

        List<Domain.Entities.Ingredient>? removed = null;
        List<Domain.Entities.Ingredient>? added = null;
        _contextMock
            .Setup(c => c.Ingredienten.RemoveRange(It.IsAny<IEnumerable<Domain.Entities.Ingredient>>()))
            .Callback<IEnumerable<Domain.Entities.Ingredient>>(x => removed = x.ToList());
        _contextMock
            .Setup(c => c.Ingredienten.AddRange(It.IsAny<IEnumerable<Domain.Entities.Ingredient>>()))
            .Callback<IEnumerable<Domain.Entities.Ingredient>>(x => added = x.ToList());

        var command = new UpdateVerspakket.Command(
            verspakketId,
            "Pakket",
            999,
            2,
            supermarktId,
            null,
            new List<Ingredient>
            {
                new("Gehakt", 300, Domain.Entities.Eenheid.Gram, false),
                new("Tomaten", 400, Domain.Entities.Eenheid.Gram, true)
            });

        await _handler.Handle(command, CancellationToken.None);

        removed.Should().NotBeNull();
        removed!.Should().ContainSingle().Which.Naam.Should().Be("Oud");

        added.Should().NotBeNull();
        added.Should().HaveCount(2);
        added.Should().OnlyContain(i => i.VerspakketId == verspakketId);
        added!.Single(i => i.Naam == "Gehakt").Inbegrepen.Should().BeFalse();
        added.Single(i => i.Naam == "Tomaten").Inbegrepen.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NullIngredienten_DoesNotTouchIngredients()
    {
        var (verspakketId, supermarktId) = SetupContext();

        var command = new UpdateVerspakket.Command(verspakketId, "Pakket", 800, 3, supermarktId, null, null);

        await _handler.Handle(command, CancellationToken.None);

        _contextMock.Verify(
            c => c.Ingredienten.RemoveRange(It.IsAny<IEnumerable<Domain.Entities.Ingredient>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidIngredientNaam_ThrowsArgumentException()
    {
        var (verspakketId, supermarktId) = SetupContext();

        var command = new UpdateVerspakket.Command(
            verspakketId,
            "Pakket",
            800,
            2,
            supermarktId,
            null,
            new List<Ingredient> { new("", 300, Domain.Entities.Eenheid.Gram, false) });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WithVoedingswaarde_WhenNone_CreatesVoedingswaarde()
    {
        var (verspakketId, supermarktId) = SetupContext();

        var command = new UpdateVerspakket.Command(
            verspakketId,
            "Pakket",
            800,
            2,
            supermarktId,
            Voedingswaarde: new Voedingswaarde
            {
                EnergieKj = 350,
                EnergieKcal = 84,
                Vetten = 2.5m,
                WaarvanVerzadigd = 0.8m,
                Koolhydraten = 10,
                WaarvanSuikers = 1.5m,
                Vezels = 3,
                Eiwitten = 6,
                Zout = 0.4m
            });

        await _handler.Handle(command, CancellationToken.None);

        _verspakket.Voedingswaarde.Should().NotBeNull();
        _verspakket.Voedingswaarde!.EnergieKj.Should().Be(350);
        _verspakket.Voedingswaarde.EnergieKcal.Should().Be(84);
        _verspakket.Voedingswaarde.Vetten.Should().Be(2.5m);
        _verspakket.Voedingswaarde.WaarvanVerzadigd.Should().Be(0.8m);
        _verspakket.Voedingswaarde.Koolhydraten.Should().Be(10);
        _verspakket.Voedingswaarde.WaarvanSuikers.Should().Be(1.5m);
        _verspakket.Voedingswaarde.Vezels.Should().Be(3);
        _verspakket.Voedingswaarde.Eiwitten.Should().Be(6);
        _verspakket.Voedingswaarde.Zout.Should().Be(0.4m);
        _verspakket.Voedingswaarde.VerspakketId.Should().Be(verspakketId);
    }

    [Fact]
    public async Task Handle_WithVoedingswaarde_WhenExisting_UpdatesVoedingswaarde()
    {
        var existing = new Domain.Entities.Voedingswaarde
        {
            Id = Guid.NewGuid(),
            EnergieKj = 100,
            Vetten = 1,
            VerspakketId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        var (verspakketId, supermarktId) = SetupContext(existingVoedingswaarde: existing);
        existing.VerspakketId = verspakketId;

        var command = new UpdateVerspakket.Command(
            verspakketId,
            "Pakket",
            800,
            2,
            supermarktId,
            Voedingswaarde: new Voedingswaarde { EnergieKj = 999, Vetten = 4 });

        await _handler.Handle(command, CancellationToken.None);

        _verspakket.Voedingswaarde.Should().BeSameAs(existing);
        existing.EnergieKj.Should().Be(999);
        existing.Vetten.Should().Be(4);
        existing.EnergieKcal.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullVoedingswaarde_DoesNotTouchVoedingswaarde()
    {
        var existing = new Domain.Entities.Voedingswaarde
        {
            Id = Guid.NewGuid(),
            EnergieKj = 100,
            VerspakketId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        var (verspakketId, supermarktId) = SetupContext(existingVoedingswaarde: existing);
        existing.VerspakketId = verspakketId;

        var command = new UpdateVerspakket.Command(verspakketId, "Pakket", 800, 2, supermarktId);

        await _handler.Handle(command, CancellationToken.None);

        _verspakket.Voedingswaarde.Should().BeSameAs(existing);
        existing.EnergieKj.Should().Be(100);
    }

    [Fact]
    public async Task Handle_WithAllergenen_ReplacesAllergenen()
    {
        var existing = new Domain.Entities.VerspakketAllergeen
        {
            Id = Guid.NewGuid(),
            Allergeen = Domain.Entities.Allergeen.Gluten,
            VerspakketId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        var (verspakketId, supermarktId) = SetupContext(existingAllergenen: [existing]);
        existing.VerspakketId = verspakketId;

        List<Domain.Entities.VerspakketAllergeen>? removed = null;
        List<Domain.Entities.VerspakketAllergeen>? added = null;
        _contextMock
            .Setup(c => c.VerspakketAllergenen.RemoveRange(It.IsAny<IEnumerable<Domain.Entities.VerspakketAllergeen>>()))
            .Callback<IEnumerable<Domain.Entities.VerspakketAllergeen>>(x => removed = x.ToList());
        _contextMock
            .Setup(c => c.VerspakketAllergenen.AddRange(It.IsAny<IEnumerable<Domain.Entities.VerspakketAllergeen>>()))
            .Callback<IEnumerable<Domain.Entities.VerspakketAllergeen>>(x => added = x.ToList());

        var command = new UpdateVerspakket.Command(
            verspakketId,
            "Pakket",
            800,
            2,
            supermarktId,
            Allergenen: [Domain.Entities.Allergeen.Melk, Domain.Entities.Allergeen.Pinda]);

        await _handler.Handle(command, CancellationToken.None);

        removed.Should().NotBeNull();
        removed!.Should().ContainSingle().Which.Allergeen.Should().Be(Domain.Entities.Allergeen.Gluten);

        added.Should().NotBeNull();
        added.Should().HaveCount(2);
        added.Should().OnlyContain(a => a.VerspakketId == verspakketId);
        added!.Select(a => a.Allergeen).Should().BeEquivalentTo(new[]
        {
            Domain.Entities.Allergeen.Melk,
            Domain.Entities.Allergeen.Pinda
        });
    }

    [Fact]
    public async Task Handle_NullAllergenen_DoesNotTouchAllergenen()
    {
        var (verspakketId, supermarktId) = SetupContext();

        var command = new UpdateVerspakket.Command(verspakketId, "Pakket", 800, 3, supermarktId);

        await _handler.Handle(command, CancellationToken.None);

        _contextMock.Verify(
            c => c.VerspakketAllergenen.RemoveRange(It.IsAny<IEnumerable<Domain.Entities.VerspakketAllergeen>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WaarvanSuikersGreaterThanKoolhydraten_ThrowsValidationException()
    {
        var (verspakketId, supermarktId) = SetupContext();

        var command = new UpdateVerspakket.Command(
            verspakketId,
            "Pakket",
            800,
            2,
            supermarktId,
            Voedingswaarde: new Voedingswaarde { Koolhydraten = 1, WaarvanSuikers = 2 });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("WaarvanSuikers mag niet groter zijn dan Koolhydraten.");
    }
}
