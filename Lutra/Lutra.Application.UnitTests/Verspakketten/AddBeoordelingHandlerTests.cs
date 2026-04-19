using FluentAssertions;
using Lutra.Application.Interfaces;
using Lutra.Application.Verspakketten;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Verspakketten;

public class AddBeoordelingHandlerTests
{
    private readonly Mock<ILutraDbContext> _contextMock;
    private readonly AddBeoordeling.Handler _handler;

    public AddBeoordelingHandlerTests()
    {
        _contextMock = new Mock<ILutraDbContext>();
        _handler = new AddBeoordeling.Handler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_VerspakketExists_AddsBeoordeling()
    {
        var verspakketId = Guid.NewGuid();
        var verspakketten = new List<Domain.Entities.Verspakket>
        {
            new() { Id = verspakketId, Naam = "Test", AantalPersonen = 2, SupermarktId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        };

        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(verspakketten);
        _contextMock.Setup(c => c.Beoordelingen).ReturnsDbSet(new List<Domain.Entities.Beoordeling>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new AddBeoordeling.Command(verspakketId, 8, 7, true, "Lekker!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VerspakketNotFound_ThrowsInvalidOperationException()
    {
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());

        var command = new AddBeoordeling.Command(Guid.NewGuid(), 8, 7, true, null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*was not found*");
    }

    [Fact]
    public async Task Handle_VerspakketDeleted_ThrowsInvalidOperationException()
    {
        var verspakketId = Guid.NewGuid();
        var verspakketten = new List<Domain.Entities.Verspakket>
        {
            new() { Id = verspakketId, Naam = "Deleted", AantalPersonen = 2, SupermarktId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow, DeletedAt = DateTime.UtcNow }
        };

        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(verspakketten);

        var command = new AddBeoordeling.Command(verspakketId, 8, 7, true, null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
