using FluentAssertions;
using Lutra.Application.BackgroundCommands;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Verspakketten;
using Lutra.Domain.Entities;
using Microsoft.Extensions.Options;
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
        _handler = new EnqueueImportVerspakket.Handler(
            _contextMock.Object,
            Options.Create(new BackgroundCommandsOptions()));
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
    public async Task Handle_TooManyActiveJobs_ThrowsTooManyRequestsException()
    {
        _contextMock.Setup(c => c.BackgroundCommandJobs).ReturnsDbSet(
            Enumerable.Range(0, 3).Select(_ => new BackgroundCommandJob
            {
                Id = Guid.NewGuid(),
                Type = BackgroundCommandType.ImportVerspakket,
                Payload = "{}",
                DeduplicationKey = Guid.NewGuid().ToString(),
                ActiveDeduplicationKey = Guid.NewGuid().ToString(),
                Status = BackgroundCommandStatus.Queued,
                IsActive = true,
                AttemptCount = 0,
                NextAttemptAt = DateTime.UtcNow,
                ConcurrencyStamp = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }).ToList());

        var handler = new EnqueueImportVerspakket.Handler(
            _contextMock.Object,
            Options.Create(new BackgroundCommandsOptions { MaxPendingJobs = 3 }));

        var act = () => handler.Handle(
            new EnqueueImportVerspakket.Command("https://www.ah.nl/product/999"),
            CancellationToken.None);

        await act.Should().ThrowAsync<TooManyRequestsException>();
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