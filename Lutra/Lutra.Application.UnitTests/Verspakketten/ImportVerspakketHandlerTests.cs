using Cortex.Mediator;
using FluentAssertions;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Verspakketten;
using Lutra.Application.Verspakketten;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Verspakketten;

public class ImportVerspakketHandlerTests
{
    private const string ValidBase64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI6QAAAABJRU5ErkJggg==";

    private readonly Mock<ILutraDbContext> _contextMock = new();
    private readonly Mock<IVerspakketProductExtractor> _extractorMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ImportVerspakket.Handler _handler;

    public ImportVerspakketHandlerTests()
    {
        _handler = new ImportVerspakket.Handler(_contextMock.Object, _extractorMock.Object, _mediatorMock.Object);
    }

    private static Domain.Entities.Supermarkt[] AlbertHeijn() =>
    [
        new() { Id = Guid.NewGuid(), Naam = "Albert Heijn", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
    ];

    private static ExtractedVerspakket BuildExtracted(
        string naam = "Test Verspakket",
        int? aantalPersonen = 2) => new()
        {
            Naam = naam,
            SupermarktNaam = "Albert Heijn",
            PrijsInCenten = 299,
            AantalPersonen = aantalPersonen,
            Fotos = [new VerspakketFoto(ValidBase64Png, true)]
        };

    [Fact]
    public async Task Handle_InvalidUrl_ThrowsValidationException()
    {
        var act = () => _handler.Handle(new ImportVerspakket.Command("not-a-url"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _extractorMock.Verify(
            e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingBronUrl_ReturnsExistingWithoutExtraction()
    {
        var existingId = Guid.NewGuid();
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(
        [
            new Domain.Entities.Verspakket
            {
                Id = existingId,
                Naam = "Bestaand",
                AantalPersonen = 2,
                BronUrl = "https://www.ah.nl/product/123",
                SupermarktId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }
        ]);

        var result = await _handler.Handle(
            new ImportVerspakket.Command("https://www.ah.nl/product/123?utm_source=x"),
            CancellationToken.None);

        result.Id.Should().Be(existingId);
        result.Created.Should().BeFalse();
        _extractorMock.Verify(
            e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NewProduct_CreatesViaMediatorWithNormalizedBronUrl()
    {
        var supermarktId = Guid.NewGuid();
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(
        [
            new Domain.Entities.Supermarkt
            {
                Id = supermarktId,
                Naam = "Albert Heijn",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }
        ]);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _extractorMock
            .Setup(e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExtracted());

        var createdId = Guid.NewGuid();
        CreateVerspakket.Command? captured = null;
        _mediatorMock
            .Setup(m => m.SendCommandAsync<CreateVerspakket.Command, CreateVerspakket.Response>(
                It.IsAny<CreateVerspakket.Command>(), It.IsAny<CancellationToken>()))
            .Callback<CreateVerspakket.Command, CancellationToken>((command, _) => captured = command)
            .ReturnsAsync(new CreateVerspakket.Response { Id = createdId });

        var result = await _handler.Handle(
            new ImportVerspakket.Command("https://www.ah.nl/product/123?utm_source=x"),
            CancellationToken.None);

        result.Id.Should().Be(createdId);
        result.Created.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.BronUrl.Should().Be("https://www.ah.nl/product/123");
        captured.SupermarktId.Should().Be(supermarktId);
        captured.AantalPersonen.Should().Be(2);
    }

    [Fact]
    public async Task Handle_LongProductName_TruncatesInsteadOfRejecting()
    {
        var supermarktId = Guid.NewGuid();
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(
        [
            new Domain.Entities.Supermarkt
            {
                Id = supermarktId,
                Naam = "Albert Heijn",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }
        ]);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _extractorMock
            .Setup(e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExtracted(naam: "Jumbo Satépannetje Gesneden Verspakket 4 Personen met extra veel groenten"));

        CreateVerspakket.Command? captured = null;
        _mediatorMock
            .Setup(m => m.SendCommandAsync<CreateVerspakket.Command, CreateVerspakket.Response>(
                It.IsAny<CreateVerspakket.Command>(), It.IsAny<CancellationToken>()))
            .Callback<CreateVerspakket.Command, CancellationToken>((command, _) => captured = command)
            .ReturnsAsync(new CreateVerspakket.Response { Id = Guid.NewGuid() });

        await _handler.Handle(
            new ImportVerspakket.Command("https://www.ah.nl/product/123"),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Naam.Length.Should().BeLessThanOrEqualTo(VerspakketImportSanitizer.MaxNaamLength);
    }

    [Fact]
    public async Task Handle_UnknownSupermarkt_ThrowsValidationException()
    {
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(new List<Domain.Entities.Supermarkt>());
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _extractorMock
            .Setup(e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExtracted());

        var act = () => _handler.Handle(
            new ImportVerspakket.Command("https://www.ah.nl/product/123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_LegacyMatchWithoutBronUrl_BackfillsAndReturnsExisting()
    {
        var supermarktId = Guid.NewGuid();
        var legacy = new Domain.Entities.Verspakket
        {
            Id = Guid.NewGuid(),
            Naam = "Test Verspakket",
            AantalPersonen = 2,
            BronUrl = null,
            SupermarktId = supermarktId,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(
        [
            new Domain.Entities.Supermarkt
            {
                Id = supermarktId,
                Naam = "Albert Heijn",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }
        ]);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet([legacy]);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _extractorMock
            .Setup(e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExtracted());

        var result = await _handler.Handle(
            new ImportVerspakket.Command("https://www.ah.nl/product/123"),
            CancellationToken.None);

        result.Id.Should().Be(legacy.Id);
        result.Created.Should().BeFalse();
        legacy.BronUrl.Should().Be("https://www.ah.nl/product/123");
        _mediatorMock.Verify(
            m => m.SendCommandAsync<CreateVerspakket.Command, CreateVerspakket.Response>(
                It.IsAny<CreateVerspakket.Command>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MissingAantalPersonen_ThrowsUnprocessableException()
    {
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _extractorMock
            .Setup(e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExtracted(aantalPersonen: null));

        var act = () => _handler.Handle(
            new ImportVerspakket.Command("https://www.ah.nl/product/123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnprocessableException>();
    }

    [Fact]
    public async Task Handle_MultipleLegacyMatches_ThrowsConflictException()
    {
        var supermarktId = Guid.NewGuid();
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(
        [
            new Domain.Entities.Supermarkt
            {
                Id = supermarktId,
                Naam = "Albert Heijn",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }
        ]);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(
        [
            new Domain.Entities.Verspakket { Id = Guid.NewGuid(), Naam = "Test Verspakket", AantalPersonen = 2, SupermarktId = supermarktId, CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow },
            new Domain.Entities.Verspakket { Id = Guid.NewGuid(), Naam = "Test Verspakket", AantalPersonen = 2, SupermarktId = supermarktId, CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        ]);
        _extractorMock
            .Setup(e => e.ExtractAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExtracted());

        var act = () => _handler.Handle(
            new ImportVerspakket.Command("https://www.ah.nl/product/123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
