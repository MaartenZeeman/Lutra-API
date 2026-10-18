using FluentAssertions;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Verspakketten;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Verspakketten;

public class UpdateBeoordelingHandlerTests
{
    private readonly Mock<ILutraDbContext> _contextMock;
    private readonly UpdateBeoordeling.Handler _handler;

    public UpdateBeoordelingHandlerTests()
    {
        _contextMock = new Mock<ILutraDbContext>();
        _handler = new UpdateBeoordeling.Handler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_BeoordelingExists_UpdatesValues()
    {
        var verspakketId = Guid.NewGuid();
        var beoordeling = new Domain.Entities.Beoordeling
        {
            Id = Guid.NewGuid(),
            CijferSmaak = 5,
            CijferBereiden = 5,
            Aanbevolen = false,
            Tekst = "Oud",
            VerspakketId = verspakketId,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.Beoordelingen).ReturnsDbSet(new List<Domain.Entities.Beoordeling> { beoordeling });
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(
            new UpdateBeoordeling.Command(verspakketId, beoordeling.Id, 9, 8, true, "Nieuw"),
            CancellationToken.None);

        result.Should().NotBeNull();
        beoordeling.CijferSmaak.Should().Be(9);
        beoordeling.CijferBereiden.Should().Be(8);
        beoordeling.Aanbevolen.Should().BeTrue();
        beoordeling.Tekst.Should().Be("Nieuw");
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BeoordelingNotFound_ThrowsNotFoundException()
    {
        _contextMock.Setup(c => c.Beoordelingen).ReturnsDbSet(new List<Domain.Entities.Beoordeling>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => _handler.Handle(
            new UpdateBeoordeling.Command(Guid.NewGuid(), Guid.NewGuid(), 9, 8, true, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*was not found*");
    }

    [Fact]
    public async Task Handle_BeoordelingBelongsToOtherVerspakket_ThrowsNotFoundException()
    {
        var beoordeling = new Domain.Entities.Beoordeling
        {
            Id = Guid.NewGuid(),
            CijferSmaak = 5,
            CijferBereiden = 5,
            Aanbevolen = false,
            VerspakketId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.Beoordelingen).ReturnsDbSet(new List<Domain.Entities.Beoordeling> { beoordeling });
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => _handler.Handle(
            new UpdateBeoordeling.Command(Guid.NewGuid(), beoordeling.Id, 9, 8, true, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
