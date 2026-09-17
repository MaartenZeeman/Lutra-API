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
        List<Domain.Entities.Voedingswaarde>? existingVoedingswaarden = null,
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

        foreach (var voedingswaarde in existingVoedingswaarden ?? [])
            _verspakket.AddVoedingswaarde(voedingswaarde);

        foreach (var allergeen in existingAllergenen ?? [])
            _verspakket.AddAllergeen(allergeen);

        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(supermarkten);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket> { _verspakket });
        _contextMock.Setup(c => c.VerspakketFotos).ReturnsDbSet(existingFotos ?? []);
        _contextMock.Setup(c => c.Ingredienten).ReturnsDbSet(existingIngredienten ?? []);
        _contextMock.Setup(c => c.Voedingswaarden).ReturnsDbSet(existingVoedingswaarden ?? []);
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
    public async Task Handle_InvalidBase64Foto_ThrowsValidationException()
    {
        var (verspakketId, supermarktId) = SetupContext();

        var command = new UpdateVerspakket.Command(
            verspakketId,
            "Pakket",
            999,
            2,
            supermarktId,
            new List<VerspakketFoto> { new("not-valid-base64!!", IsMainImage: true) });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_InvalidAantalPersonen_ThrowsValidationException()
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
    public async Task Handle_InvalidIngredientNaam_ThrowsValidationException()
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
    public async Task Handle_WithVoedingswaarden_ReplacesVoedingswaarden()
    {
        var existing = new Domain.Entities.Voedingswaarde
        {
            Id = Guid.NewGuid(),
            Basis = Domain.Entities.VoedingswaardeBasis.Per100Gram,
            EnergieKj = 100,
            VerspakketId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        var (verspakketId, supermarktId) = SetupContext(existingVoedingswaarden: [existing]);
        existing.VerspakketId = verspakketId;

        List<Domain.Entities.Voedingswaarde>? removed = null;
        List<Domain.Entities.Voedingswaarde>? added = null;
        _contextMock
            .Setup(c => c.Voedingswaarden.RemoveRange(It.IsAny<IEnumerable<Domain.Entities.Voedingswaarde>>()))
            .Callback<IEnumerable<Domain.Entities.Voedingswaarde>>(x => removed = x.ToList());
        _contextMock
            .Setup(c => c.Voedingswaarden.AddRange(It.IsAny<IEnumerable<Domain.Entities.Voedingswaarde>>()))
            .Callback<IEnumerable<Domain.Entities.Voedingswaarde>>(x => added = x.ToList());

        var command = new UpdateVerspakket.Command(
            verspakketId,
            "Pakket",
            800,
            2,
            supermarktId,
            Voedingswaarden:
            [
                new Voedingswaarde
                {
                    Basis = Domain.Entities.VoedingswaardeBasis.Per100Gram,
                    EnergieKj = 476,
                    Vetten = 3.8m
                },
                new Voedingswaarde
                {
                    Basis = Domain.Entities.VoedingswaardeBasis.PerPortie,
                    EnergieKj = 2723,
                    Vetten = 21.9m
                }
            ]);

        await _handler.Handle(command, CancellationToken.None);

        removed.Should().NotBeNull();
        removed!.Should().ContainSingle().Which.Basis.Should().Be(Domain.Entities.VoedingswaardeBasis.Per100Gram);

        added.Should().NotBeNull();
        added.Should().HaveCount(2);
        added.Should().OnlyContain(w => w.VerspakketId == verspakketId);
        added!.Single(w => w.Basis == Domain.Entities.VoedingswaardeBasis.Per100Gram).EnergieKj.Should().Be(476);
        added.Single(w => w.Basis == Domain.Entities.VoedingswaardeBasis.PerPortie).EnergieKj.Should().Be(2723);
    }

    [Fact]
    public async Task Handle_NullVoedingswaarden_DoesNotTouchVoedingswaarden()
    {
        var existing = new Domain.Entities.Voedingswaarde
        {
            Id = Guid.NewGuid(),
            Basis = Domain.Entities.VoedingswaardeBasis.Per100Gram,
            EnergieKj = 100,
            VerspakketId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        var (verspakketId, supermarktId) = SetupContext(existingVoedingswaarden: [existing]);
        existing.VerspakketId = verspakketId;

        var command = new UpdateVerspakket.Command(verspakketId, "Pakket", 800, 2, supermarktId);

        await _handler.Handle(command, CancellationToken.None);

        _contextMock.Verify(
            c => c.Voedingswaarden.RemoveRange(It.IsAny<IEnumerable<Domain.Entities.Voedingswaarde>>()),
            Times.Never);
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
}
