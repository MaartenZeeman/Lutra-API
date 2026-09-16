using System.Text.Json;
using Cortex.Mediator;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Application.Verspakketten;
using Lutra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lutra.Application.BackgroundCommands;

/// <summary>
/// Claims and executes one durable background command job per call. Jobs are claimed with an
/// optimistic concurrency token so multiple API replicas cannot execute the same job twice.
/// </summary>
public sealed class BackgroundCommandProcessor(
    ILutraDbContext context,
    IMediator mediator,
    IOptions<BackgroundCommandsOptions> options,
    ILogger<BackgroundCommandProcessor> logger)
{
    private const int MaxErrorLength = 2048;

    private readonly BackgroundCommandsOptions _options = options.Value;

    /// <summary>Processes at most one job. Returns true when a job was claimed (or a claim race was observed).</summary>
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var candidateId = await FindCandidateIdAsync(cancellationToken);

        if (candidateId is null)
        {
            return false;
        }

        var job = await ClaimAsync(candidateId.Value, cancellationToken);

        if (job is null)
        {
            return true;
        }

        await ExecuteAsync(job, cancellationToken);
        return true;
    }

    private async Task<Guid?> FindCandidateIdAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return await context.BackgroundCommandJobs
            .AsNoTracking()
            .Where(j =>
                ((j.Status == BackgroundCommandStatus.Queued || j.Status == BackgroundCommandStatus.RetryScheduled)
                    && j.NextAttemptAt <= now)
                || (j.Status == BackgroundCommandStatus.Processing
                    && j.LeaseExpiresAt != null
                    && j.LeaseExpiresAt <= now))
            .OrderBy(j => j.NextAttemptAt)
            .Select(j => (Guid?)j.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<BackgroundCommandJob?> ClaimAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await context.BackgroundCommandJobs
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (job is null)
        {
            return null;
        }

        if (job.AttemptCount >= _options.MaxAttempts)
        {
            logger.LogWarning(
                "Background job {JobId} exhausted its {MaxAttempts} attempts; failing terminally instead of reclaiming.",
                job.Id,
                _options.MaxAttempts);

            await FailTerminallyAsync(job, "Maximaal aantal pogingen bereikt.", cancellationToken);
            return null;
        }

        var now = DateTime.UtcNow;
        job.Status = BackgroundCommandStatus.Processing;
        job.AttemptCount += 1;
        job.LeaseToken = Guid.NewGuid();
        job.LeaseExpiresAt = now.AddMinutes(_options.LeaseDurationMinutes);
        job.StartedAt ??= now;
        job.ConcurrencyStamp = Guid.NewGuid();

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another worker claimed the same job first.
            logger.LogInformation("Background job {JobId} was claimed by another worker.", id);
            return null;
        }

        return job;
    }

    private async Task ExecuteAsync(BackgroundCommandJob job, CancellationToken cancellationToken)
    {
        try
        {
            Guid resultVerspakketId;

            switch (job.Type)
            {
                case BackgroundCommandType.ImportVerspakket:
                    var payload = JsonSerializer.Deserialize<ImportVerspakketPayload>(job.Payload)
                        ?? throw new UnprocessableException("Het importverzoek kon niet worden gelezen.");

                    var result = await mediator.SendCommandAsync<ImportVerspakket.Command, ImportVerspakket.Response>(
                        new ImportVerspakket.Command(payload.Url), cancellationToken);

                    resultVerspakketId = result.Id;
                    break;

                default:
                    await FailTerminallyAsync(job, "Onbekend achtergrondcommando.", cancellationToken);
                    return;
            }

            job.Status = BackgroundCommandStatus.Succeeded;
            job.IsActive = false;
            job.ActiveDeduplicationKey = null;
            job.ResultVerspakketId = resultVerspakketId;
            job.CompletedAt = DateTime.UtcNow;
            job.LastError = null;
            job.LeaseToken = null;
            job.LeaseExpiresAt = null;

            await SaveAsync(job, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Host is shutting down: leave the job Processing so its lease expires and it is reclaimed.
            throw;
        }
        catch (Exception ex)
        {
            await HandleFailureAsync(job, ex, cancellationToken);
        }
    }

    private async Task HandleFailureAsync(BackgroundCommandJob job, Exception exception, CancellationToken cancellationToken)
    {
        var message = Truncate(exception.Message, MaxErrorLength);
        var canRetry = IsRetryable(exception) && job.AttemptCount < _options.MaxAttempts;

        if (canRetry)
        {
            job.Status = BackgroundCommandStatus.RetryScheduled;
            job.IsActive = true;
            job.NextAttemptAt = DateTime.UtcNow.AddMinutes(_options.RetryDelayMinutes);
            job.LastError = message;
            job.LeaseToken = null;
            job.LeaseExpiresAt = null;
        }
        else
        {
            logger.LogWarning(
                exception,
                "Background job {JobId} failed permanently after {AttemptCount} attempt(s).",
                job.Id,
                job.AttemptCount);

            await FailTerminallyAsync(job, message, cancellationToken);
            return;
        }

        await SaveAsync(job, cancellationToken);
    }

    private async Task FailTerminallyAsync(BackgroundCommandJob job, string message, CancellationToken cancellationToken)
    {
        job.Status = BackgroundCommandStatus.Failed;
        job.IsActive = false;
        job.ActiveDeduplicationKey = null;
        job.CompletedAt = DateTime.UtcNow;
        job.LastError = Truncate(message, MaxErrorLength);
        job.LeaseToken = null;
        job.LeaseExpiresAt = null;

        await SaveAsync(job, cancellationToken);
    }

    private async Task SaveAsync(BackgroundCommandJob job, CancellationToken cancellationToken)
    {
        job.ConcurrencyStamp = Guid.NewGuid();

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A newer worker owns this job now; do not overwrite its state.
            logger.LogWarning(
                "Background job {JobId} was reclaimed by another worker; discarding local state (status {Status}, attempts {AttemptCount}).",
                job.Id,
                job.Status,
                job.AttemptCount);
        }
    }

    private static bool IsRetryable(Exception exception) =>
        exception is not (ValidationException or UnprocessableException or ConflictException or NotFoundException);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}