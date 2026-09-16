using FluentAssertions;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Verspakketten;
using Lutra.Domain.Entities;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.Verspakketten;

public class EnqueueImportVerspakketHandlerTests
{
    private readonly Mock<ILutraDbContext> _contextMock = new();
    private readonly EnqueueImportVerspakket.Handler _handler;

    public EnqueueImportVerspakketHandlerTests()
    {
        _contextMock.Setup(c => c.BackgroundCommandJobs).ReturnsDbSet(new List<BackgroundCommandJob>());
        _handler = new EnqueueImportVerspakket.Handler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_InvalidUrl_ThrowsValidationException()
    {
        var act = () => _handler.Handle(new EnqueueImportVerspakket.Command("not-a-url"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_NewUrl_AddsQueuedJobWithNormalizedKey()
    {
        BackgroundCommandJob? saved = null;
        _contextMock
            .Setup(c => c.BackgroundCommandJobs.AddAsync(It.IsAny<BackgroundCommandJob>(), It.IsAny<CancellationToken>()))
            .Callback<BackgroundCommandJob, CancellationToken>((job, _) => saved = job);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(
            new EnqueueImportVerspakket.Command("https://www.ah.nl/product/123?utm_source=x"),
            CancellationToken.None);

        result.Status.Should().Be(BackgroundCommandStatus.Queued);
        result.Existing.Should().BeFalse();
        result.Id.Should().NotBeEmpty();
        saved.Should().NotBeNull();
        saved!.Type.Should().Be(BackgroundCommandType.ImportVerspakket);
        saved.DeduplicationKey.Should().Be("https://www.ah.nl/product/123");
        saved.ActiveDeduplicationKey.Should().Be("https://www.ah.nl/product/123");
        saved.IsActive.Should().BeTrue();
        saved.AttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ExistingActiveJob_ReturnsExistingWithoutAdding()
    {
        var existingId = Guid.NewGuid();
        _contextMock.Setup(c => c.BackgroundCommandJobs).ReturnsDbSet(
        [
            new BackgroundCommandJob
            {
                Id = existingId,
                Type = BackgroundCommandType.ImportVerspakket,
                Payload = "{}",
                DeduplicationKey = "https://www.ah.nl/product/123",
                ActiveDeduplicationKey = "https://www.ah.nl/product/123",
                Status = BackgroundCommandStatus.Queued,
                IsActive = true,
                AttemptCount = 0,
                NextAttemptAt = DateTime.UtcNow,
                ConcurrencyStamp = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }
        ]);

        var result = await _handler.Handle(
            new EnqueueImportVerspakket.Command("https://www.ah.nl/product/123"),
            CancellationToken.None);

        result.Id.Should().Be(existingId);
        result.Existing.Should().BeTrue();
        _contextMock.Verify(
            c => c.BackgroundCommandJobs.AddAsync(It.IsAny<BackgroundCommandJob>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}