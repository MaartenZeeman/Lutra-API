using FluentAssertions;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Supermarkten;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Supermarkten;

public class CreateSupermarktHandlerTests
{
    private readonly Mock<ILutraDbContext> _contextMock;
    private readonly CreateSupermarkt.Handler _handler;

    public CreateSupermarktHandlerTests()
    {
        _contextMock = new Mock<ILutraDbContext>();
        _handler = new CreateSupermarkt.Handler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidNaam_CreatesSupermarkt()
    {
        Domain.Entities.Supermarkt? savedSupermarkt = null;
        _contextMock.Setup(c => c.Supermarkten).ReturnsDbSet(new List<Domain.Entities.Supermarkt>());
        _contextMock
            .Setup(c => c.Supermarkten.Add(It.IsAny<Domain.Entities.Supermarkt>()))
            .Callback<Domain.Entities.Supermarkt>(s => savedSupermarkt = s);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(new CreateSupermarkt.Command("Albert Heijn"), CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        savedSupermarkt.Should().NotBeNull();
        savedSupermarkt!.Naam.Should().Be("Albert Heijn");
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyNaam_ThrowsArgumentException()
    {
        var act = () => _handler.Handle(new CreateSupermarkt.Command("  "), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_NaamTooLong_ThrowsArgumentException()
    {
        var act = () => _handler.Handle(new CreateSupermarkt.Command(new string('a', 51)), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}