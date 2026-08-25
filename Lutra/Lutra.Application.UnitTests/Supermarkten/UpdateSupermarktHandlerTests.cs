using FluentAssertions;
using Lutra.Application.Interfaces;
using Lutra.Application.Supermarkten;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Supermarkten;

public class UpdateSupermarktHandlerTests
{
    private readonly Mock<ILutraDbContext> _contextMock;
    private readonly UpdateSupermarkt.Handler _handler;

    public UpdateSupermarktHandlerTests()
    {
        _contextMock = new Mock<ILutraDbContext>();
        _handler = new UpdateSupermarkt.Handler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidNaam_UpdatesSupermarkt()
    {
        var supermarktId = Guid.NewGuid();
        var supermarkt = new Domain.Entities.Supermarkt
        {
            Id = supermarktId,
            Naam = "Oud",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(new List<Domain.Entities.Supermarkt> { supermarkt });
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(new UpdateSupermarkt.Command(supermarktId, "Nieuw"), CancellationToken.None);

        result.Should().NotBeNull();
        supermarkt.Naam.Should().Be("Nieuw");
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SupermarktNotFound_ThrowsInvalidOperationException()
    {
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(new List<Domain.Entities.Supermarkt>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => _handler.Handle(new UpdateSupermarkt.Command(Guid.NewGuid(), "Pakket"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*was not found*");
    }

    [Fact]
    public async Task Handle_EmptyNaam_ThrowsArgumentException()
    {
        var supermarkt = new Domain.Entities.Supermarkt
        {
            Id = Guid.NewGuid(),
            Naam = "Oud",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(new List<Domain.Entities.Supermarkt> { supermarkt });
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => _handler.Handle(new UpdateSupermarkt.Command(supermarkt.Id, "  "), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}