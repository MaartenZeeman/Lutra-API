using FluentAssertions;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Supermarkten;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Supermarkten;

public class DeleteSupermarktHandlerTests
{
    private readonly Mock<ILutraDbContext> _contextMock;
    private readonly DeleteSupermarkt.Handler _handler;

    public DeleteSupermarktHandlerTests()
    {
        _contextMock = new Mock<ILutraDbContext>();
        _handler = new DeleteSupermarkt.Handler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_WithoutVerspakketten_SoftDeletesSupermarkt()
    {
        var supermarkt = new Domain.Entities.Supermarkt
        {
            Id = Guid.NewGuid(),
            Naam = "Jumbo",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(new List<Domain.Entities.Supermarkt> { supermarkt });
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(new DeleteSupermarkt.Command(supermarkt.Id), CancellationToken.None);

        result.Should().NotBeNull();
        supermarkt.IsDeleted.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SupermarktNotFound_ThrowsNotFoundException()
    {
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(new List<Domain.Entities.Supermarkt>());
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => _handler.Handle(new DeleteSupermarkt.Command(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*was not found*");
    }

    [Fact]
    public async Task Handle_SupermarktStillHasVerspakketten_ThrowsConflictException()
    {
        var supermarkt = new Domain.Entities.Supermarkt
        {
            Id = Guid.NewGuid(),
            Naam = "Jumbo",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(new List<Domain.Entities.Supermarkt> { supermarkt });
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Naam = "Pakket",
                AantalPersonen = 2,
                SupermarktId = supermarkt.Id,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }
        });

        var act = () => _handler.Handle(new DeleteSupermarkt.Command(supermarkt.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        supermarkt.IsDeleted.Should().BeFalse();
    }
}
