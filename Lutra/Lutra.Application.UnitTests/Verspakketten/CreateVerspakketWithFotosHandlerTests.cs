using FluentAssertions;
using Lutra.Application.Interfaces;
using Lutra.Application.Models.Verspakketten;
using Lutra.Application.Verspakketten;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Verspakketten;

public class CreateVerspakketWithFotosHandlerTests
{
    private readonly Mock<ILutraDbContext> _contextMock;
    private readonly CreateVerspakket.Handler _handler;

    // 1x1 white PNG as base64
    private const string ValidBase64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI6QAAAABJRU5ErkJggg==";

    public CreateVerspakketWithFotosHandlerTests()
    {
        _contextMock = new Mock<ILutraDbContext>();
        _handler = new CreateVerspakket.Handler(_contextMock.Object);
    }

    private void SetupContext(Guid supermarktId)
    {
        var supermarkten = new List<Domain.Entities.Supermarkt>
        {
            new() { Id = supermarktId, Naam = "AH", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        };
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(supermarkten);
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_WithFotos_CreatesVerspakketWithFotos()
    {
        var supermarktId = Guid.NewGuid();
        SetupContext(supermarktId);

        Domain.Entities.Verspakket? saved = null;
        _contextMock
            .Setup(c => c.Verspaketten.AddAsync(It.IsAny<Domain.Entities.Verspakket>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Verspakket, CancellationToken>((v, _) => saved = v);

        var fotos = new List<VerspakketFoto>
        {
            new(ValidBase64Png, IsMainImage: true),
            new(ValidBase64Png, IsMainImage: false)
        };

        var command = new CreateVerspakket.Command("Lente Pakket", 999, 2, supermarktId, null, fotos);

        await _handler.Handle(command, CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.Fotos.Should().HaveCount(2);
        saved.Fotos.Count(f => f.IsMainImage).Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithoutFotos_CreatesVerspakketWithNoFotos()
    {
        var supermarktId = Guid.NewGuid();
        SetupContext(supermarktId);

        Domain.Entities.Verspakket? saved = null;
        _contextMock
            .Setup(c => c.Verspaketten.AddAsync(It.IsAny<Domain.Entities.Verspakket>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Verspakket, CancellationToken>((v, _) => saved = v);

        var command = new CreateVerspakket.Command("Herfst Pakket", 799, 2, supermarktId, null);

        await _handler.Handle(command, CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.Fotos.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_InvalidBase64Foto_ThrowsValidationException()
    {
        var supermarktId = Guid.NewGuid();
        SetupContext(supermarktId);

        var fotos = new List<VerspakketFoto> { new("not-valid-base64!!", IsMainImage: true) };
        var command = new CreateVerspakket.Command("Pakket", 999, 2, supermarktId, null, fotos);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Lutra.Application.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Handle_TooManyFotos_ThrowsValidationException()
    {
        var supermarktId = Guid.NewGuid();
        SetupContext(supermarktId);

        var fotos = Enumerable.Range(0, VerspakketFotoValidator.MaxFotos + 1)
            .Select(_ => new VerspakketFoto(ValidBase64Png, IsMainImage: false))
            .ToList();
        var command = new CreateVerspakket.Command("Pakket", 999, 2, supermarktId, null, fotos);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Lutra.Application.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Handle_FotoLargerThanMaxBytes_ThrowsValidationException()
    {
        var supermarktId = Guid.NewGuid();
        SetupContext(supermarktId);

        var oversized = Convert.ToBase64String(new byte[VerspakketFotoValidator.MaxFotoBytes + 1]);
        var fotos = new List<VerspakketFoto> { new(oversized, IsMainImage: true) };
        var command = new CreateVerspakket.Command("Pakket", 999, 2, supermarktId, null, fotos);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Lutra.Application.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Handle_TotalFotoBytesTooLarge_ThrowsValidationException()
    {
        var supermarktId = Guid.NewGuid();
        SetupContext(supermarktId);

        var fiveMiBFoto = Convert.ToBase64String(new byte[VerspakketFotoValidator.MaxFotoBytes]);
        var fotos = Enumerable.Range(0, 5)
            .Select(_ => new VerspakketFoto(fiveMiBFoto, IsMainImage: false))
            .ToList();
        var command = new CreateVerspakket.Command("Pakket", 999, 2, supermarktId, null, fotos);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Lutra.Application.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Handle_FotoBase64Decoded_StoresCorrectBytes()
    {
        var supermarktId = Guid.NewGuid();
        SetupContext(supermarktId);

        Domain.Entities.Verspakket? saved = null;
        _contextMock
            .Setup(c => c.Verspaketten.AddAsync(It.IsAny<Domain.Entities.Verspakket>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Verspakket, CancellationToken>((v, _) => saved = v);

        var fotos = new List<VerspakketFoto> { new(ValidBase64Png, IsMainImage: false) };
        var command = new CreateVerspakket.Command("Pakket", null, 1, supermarktId, null, fotos);

        await _handler.Handle(command, CancellationToken.None);

        var foto = saved!.Fotos.Single();
        foto.Data.Should().BeEquivalentTo(Convert.FromBase64String(ValidBase64Png));
    }
}
