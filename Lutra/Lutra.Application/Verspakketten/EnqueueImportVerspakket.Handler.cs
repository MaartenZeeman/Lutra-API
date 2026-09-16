using System.Text.Json;
using Cortex.Mediator.Commands;
using Lutra.Application.BackgroundCommands;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Lutra.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten;

public sealed partial class EnqueueImportVerspakket
{
    public sealed class Handler(ILutraDbContext context) : ICommandHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!VerspakketUrlNormalizer.TryNormalize(request.Url, out var normalizedUrl))
            {
                throw new ValidationException("De opgegeven URL is ongeldig. Gebruik een absolute https-URL.");
            }

            var existing = await context.BackgroundCommandJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    j => j.Type == BackgroundCommandType.ImportVerspakket
                        && j.ActiveDeduplicationKey == normalizedUrl,
                    cancellationToken);

            if (existing is not null)
            {
                return Map(existing, existingJob: true);
            }

            var now = DateTime.UtcNow;
            var job = new BackgroundCommandJob
            {
                Id = Guid.NewGuid(),
                Type = BackgroundCommandType.ImportVerspakket,
                Payload = JsonSerializer.Serialize(new ImportVerspakketPayload(normalizedUrl)),
                DeduplicationKey = normalizedUrl,
                ActiveDeduplicationKey = normalizedUrl,
                Status = BackgroundCommandStatus.Queued,
                IsActive = true,
                AttemptCount = 0,
                NextAttemptAt = now,
                ConcurrencyStamp = Guid.NewGuid(),
                CreatedAt = now,
                ModifiedAt = now
            };

            await context.BackgroundCommandJobs.AddAsync(job, cancellationToken);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // The unique index on (Type, ActiveDeduplicationKey) is the final safeguard against duplicates.
                var raced = await context.BackgroundCommandJobs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        j => j.Type == BackgroundCommandType.ImportVerspakket
                            && j.ActiveDeduplicationKey == normalizedUrl,
                        cancellationToken);

                if (raced is not null)
                {
                    return Map(raced, existingJob: true);
                }

                throw;
            }

            return Map(job, existingJob: false);
        }

        private static Response Map(BackgroundCommandJob job, bool existingJob) => new()
        {
            Id = job.Id,
            Status = job.Status,
            AttemptCount = job.AttemptCount,
            NextAttemptAt = job.NextAttemptAt,
            Existing = existingJob
        };
    }
}