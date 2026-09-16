using Cortex.Mediator;
using FluentAssertions;
using Lutra.Application.BackgroundCommands;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Verspakketten;
using Lutra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;

namespace Lutra.Application.UnitTests.BackgroundCommands;

public class BackgroundCommandProcessorTests
{
    private const string Payload = "{\"Url\":\"https://www.ah.nl/product/123\"}";

    private readonly Mock<ILutraDbContext> _contextMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();

    private static BackgroundCommandsOptions BuildOptions(int retryDelayMinutes = 5) => new()
    {
        MaxAttempts = 3,
        LeaseDurationMinutes = 20,
        RetryDelayMinutes = retryDelayMinutes
    };

    private BackgroundCommandProcessor CreateProcessor(int retryDelayMinutes = 5) =>
        new(
            _contextMock.Object,
            _mediatorMock.Object,
            Options.Create(BuildOptions(retryDelayMinutes)),
            NullLogger<BackgroundCommandProcessor>.Instance);

    private void SetupJobs(params BackgroundCommandJob[] jobs) =>
        _contextMock.Setup(c => c.BackgroundCommandJobs).ReturnsDbSet(jobs.ToList());

    private void SetupMediatorSuccess(Guid verspakketId)
    {
        _mediatorMock
            .Setup(m => m.SendCommandAsync<ImportVerspakket.Command, ImportVerspakket.Response>(
                It.IsAny<ImportVerspakket.Command>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImportVerspakket.Response { Id = verspakketId, Created = true });
    }

    private static BackgroundCommandJob Job(
        BackgroundCommandStatus status,
        DateTime? leaseExpiresAt = null,
        int attemptCount = 0,
        BackgroundCommandType type = BackgroundCommandType.ImportVerspakket,
        DateTime? nextAttemptAt = null) => new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = Payload,
            DeduplicationKey = "https://www.ah.nl/product/123",
            ActiveDeduplicationKey = "https://www.ah.nl/product/123",
            Status = status,
            IsActive = true,
            AttemptCount = attemptCount,
            NextAttemptAt = nextAttemptAt ?? DateTime.UtcNow.AddMinutes(-1),
            LeaseExpiresAt = leaseExpiresAt,
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

    [Fact]
    public async Task ProcessNextAsync_NoDueJobs_ReturnsFalse()
    {
        SetupJobs();

        var processor = CreateProcessor();

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        processed.Should().BeFalse();
        _mediatorMock.Verify(
            m => m.SendCommandAsync<ImportVerspakket.Command, ImportVerspakket.Response>(
                It.IsAny<ImportVerspakket.Command>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessNextAsync_QueuedJob_ClaimsExecutesAndSucceeds()
    {
        var job = Job(BackgroundCommandStatus.Queued);
        SetupJobs(job);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupMediatorSuccess(Guid.NewGuid());

        var processor = CreateProcessor();

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        processed.Should().BeTrue();
        job.Status.Should().Be(BackgroundCommandStatus.Succeeded);
        job.IsActive.Should().BeFalse();
        job.ActiveDeduplicationKey.Should().BeNull();
        job.AttemptCount.Should().Be(1);
        job.LeaseExpiresAt.Should().BeNull();
        job.CompletedAt.Should().NotBeNull();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessNextAsync_LeaseExpiredProcessingJob_ReclaimsAndRuns()
    {
        var job = Job(BackgroundCommandStatus.Processing, leaseExpiresAt: DateTime.UtcNow.AddMinutes(-1));
        SetupJobs(job);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupMediatorSuccess(Guid.NewGuid());

        var processor = CreateProcessor();

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        processed.Should().BeTrue();
        job.AttemptCount.Should().Be(1);
        job.Status.Should().Be(BackgroundCommandStatus.Succeeded);
    }

    [Fact]
    public async Task ProcessNextAsync_LeaseExpiredAtMaxAttempts_FailsTerminallyWithoutRunning()
    {
        var job = Job(BackgroundCommandStatus.Processing, leaseExpiresAt: DateTime.UtcNow.AddMinutes(-1), attemptCount: 3);
        SetupJobs(job);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var processor = CreateProcessor();

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        processed.Should().BeTrue();
        job.Status.Should().Be(BackgroundCommandStatus.Failed);
        job.IsActive.Should().BeFalse();
        job.LastError.Should().Be("Maximaal aantal pogingen bereikt.");
        _mediatorMock.Verify(
            m => m.SendCommandAsync<ImportVerspakket.Command, ImportVerspakket.Response>(
                It.IsAny<ImportVerspakket.Command>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessNextAsync_ClaimConflict_ReturnsTrueWithoutExecuting()
    {
        var job = Job(BackgroundCommandStatus.Queued);
        SetupJobs(job);
        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var processor = CreateProcessor();

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        processed.Should().BeTrue();
        _mediatorMock.Verify(
            m => m.SendCommandAsync<ImportVerspakket.Command, ImportVerspakket.Response>(
                It.IsAny<ImportVerspakket.Command>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessNextAsync_CompletionConcurrencyConflict_IsSwallowed()
    {
        var job = Job(BackgroundCommandStatus.Queued);
        SetupJobs(job);
        _contextMock
            .SetupSequence(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .ThrowsAsync(new DbUpdateConcurrencyException());
        SetupMediatorSuccess(Guid.NewGuid());

        var processor = CreateProcessor();

        var act = () => processor.ProcessNextAsync(CancellationToken.None);

        var processed = await act.Should().NotThrowAsync();
        processed.Subject.Should().BeTrue();
        _mediatorMock.Verify(
            m => m.SendCommandAsync<ImportVerspakket.Command, ImportVerspakket.Response>(
                It.IsAny<ImportVerspakket.Command>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessNextAsync_UnknownCommandType_FailsTerminally()
    {
        var job = Job(BackgroundCommandStatus.Queued, type: (BackgroundCommandType)999);
        SetupJobs(job);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var processor = CreateProcessor();

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        processed.Should().BeTrue();
        job.Status.Should().Be(BackgroundCommandStatus.Failed);
        job.LastError.Should().Be("Onbekend achtergrondcommando.");
        _mediatorMock.Verify(
            m => m.SendCommandAsync<ImportVerspakket.Command, ImportVerspakket.Response>(
                It.IsAny<ImportVerspakket.Command>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessNextAsync_TransientFailure_SchedulesRetryAfterConfiguredDelay()
    {
        var job = Job(BackgroundCommandStatus.Queued);
        SetupJobs(job);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mediatorMock
            .Setup(m => m.SendCommandAsync<ImportVerspakket.Command, ImportVerspakket.Response>(
                It.IsAny<ImportVerspakket.Command>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalServiceException("tijdelijk kapot"));

        var processor = CreateProcessor(retryDelayMinutes: 7);

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        processed.Should().BeTrue();
        job.Status.Should().Be(BackgroundCommandStatus.RetryScheduled);
        job.IsActive.Should().BeTrue();
        job.LeaseExpiresAt.Should().BeNull();
        job.NextAttemptAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(7), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task ProcessNextAsync_PermanentFailure_FailsTerminally()
    {
        var job = Job(BackgroundCommandStatus.Queued);
        SetupJobs(job);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mediatorMock
            .Setup(m => m.SendCommandAsync<ImportVerspakket.Command, ImportVerspakket.Response>(
                It.IsAny<ImportVerspakket.Command>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("ongeldig"));

        var processor = CreateProcessor();

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        processed.Should().BeTrue();
        job.Status.Should().Be(BackgroundCommandStatus.Failed);
        job.IsActive.Should().BeFalse();
        job.LastError.Should().Be("ongeldig");
    }
}
