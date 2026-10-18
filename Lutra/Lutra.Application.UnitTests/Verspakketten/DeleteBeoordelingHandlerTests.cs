using FluentAssertions;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Verspakketten;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Verspakketten;

public class DeleteBeoordelingHandlerTests
{
    private readonly Mock<ILutraDbContext> _contextMock;
    private readonly DeleteBeoordeling.Handler _handler;

    public DeleteBeoordelingHandlerTests()
    {
        _contextMock = new Mock<ILutraDbContext>();
        _handler = new DeleteBeoordeling.Handler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_BeoordelingExists_SoftDeletesBeoordeling()
    {
        var verspakketId = Guid.NewGuid();
        var beoordeling = new Domain.Entities.Beoordeling
        {
            Id = Guid.NewGuid(),
            CijferSmaak = 8,
            CijferBereiden = 7,
            Aanbevolen = true,
            VerspakketId = verspakketId,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.Beoordelingen).ReturnsDbSet(new List<Domain.Entities.Beoordeling> { beoordeling });
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(
            new DeleteBeoordeling.Command(verspakketId, beoordeling.Id), CancellationToken.None);

        result.Should().NotBeNull();
        beoordeling.IsDeleted.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BeoordelingNotFound_ThrowsNotFoundException()
    {
        _contextMock.Setup(c => c.Beoordelingen).ReturnsDbSet(new List<Domain.Entities.Beoordeling>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => _handler.Handle(
            new DeleteBeoordeling.Command(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*was not found*");
    }

    [Fact]
    public async Task Handle_BeoordelingBelongsToOtherVerspakket_ThrowsNotFoundException()
    {
        var beoordeling = new Domain.Entities.Beoordeling
        {
            Id = Guid.NewGuid(),
            CijferSmaak = 8,
            CijferBereiden = 7,
            Aanbevolen = true,
            VerspakketId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.Beoordelingen).ReturnsDbSet(new List<Domain.Entities.Beoordeling> { beoordeling });
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => _handler.Handle(
            new DeleteBeoordeling.Command(Guid.NewGuid(), beoordeling.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
