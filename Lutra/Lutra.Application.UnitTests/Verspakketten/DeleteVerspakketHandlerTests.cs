using FluentAssertions;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Verspakketten;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Verspakketten;

public class DeleteVerspakketHandlerTests
{
    private readonly Mock<ILutraDbContext> _contextMock;
    private readonly DeleteVerspakket.Handler _handler;

    public DeleteVerspakketHandlerTests()
    {
        _contextMock = new Mock<ILutraDbContext>();
        _handler = new DeleteVerspakket.Handler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_VerspakketExists_SoftDeletesVerspakketAndChildren()
    {
        var verspakketId = Guid.NewGuid();
        var verspakket = new Domain.Entities.Verspakket
        {
            Id = verspakketId,
            Naam = "Pakket",
            AantalPersonen = 2,
            SupermarktId = Guid.NewGuid(),
            BronUrl = "https://www.ah.nl/product/1",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        var beoordeling = new Domain.Entities.Beoordeling
        {
            Id = Guid.NewGuid(), CijferSmaak = 8, CijferBereiden = 7, Aanbevolen = true,
            VerspakketId = verspakketId, CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        };
        verspakket.AddBeoordeling(beoordeling);

        var foto = new Domain.Entities.VerspakketFoto
        {
            Id = Guid.NewGuid(), Data = [1, 2, 3], IsMainImage = true,
            VerspakketId = verspakketId, CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        };
        verspakket.AddFoto(foto);

        var ingredient = new Domain.Entities.Ingredient
        {
            Id = Guid.NewGuid(), Naam = "Tomaten", Hoeveelheid = 400, Eenheid = Domain.Entities.Eenheid.Gram,
            Inbegrepen = true, VerspakketId = verspakketId, CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        };
        verspakket.AddIngredient(ingredient);

        var voedingswaarde = new Domain.Entities.Voedingswaarde
        {
            Id = Guid.NewGuid(), Basis = Domain.Entities.VoedingswaardeBasis.Per100Gram,
            VerspakketId = verspakketId, CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        };
        verspakket.AddVoedingswaarde(voedingswaarde);

        var allergeen = new Domain.Entities.VerspakketAllergeen
        {
            Id = Guid.NewGuid(), Allergeen = Domain.Entities.Allergeen.Gluten,
            VerspakketId = verspakketId, CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
        };
        verspakket.AddAllergeen(allergeen);

        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket> { verspakket });
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(new DeleteVerspakket.Command(verspakketId), CancellationToken.None);

        result.Should().NotBeNull();
        verspakket.IsDeleted.Should().BeTrue();
        verspakket.BronUrl.Should().BeNull("the unique BronUrl index is not filtered on deletion");
        beoordeling.IsDeleted.Should().BeTrue();
        foto.IsDeleted.Should().BeTrue();
        ingredient.IsDeleted.Should().BeTrue();
        voedingswaarde.IsDeleted.Should().BeTrue();
        allergeen.IsDeleted.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VerspakketNotFound_ThrowsNotFoundException()
    {
        _contextMock.Setup(c => c.Verspaketten).ReturnsDbSet(new List<Domain.Entities.Verspakket>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = () => _handler.Handle(new DeleteVerspakket.Command(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*was not found*");
    }
}
