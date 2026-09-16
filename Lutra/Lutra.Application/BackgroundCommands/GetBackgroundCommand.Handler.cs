using Cortex.Mediator.Queries;
using Lutra.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.BackgroundCommands;

public sealed partial class GetBackgroundCommand
{
    public sealed class Handler(ILutraDbContext context) : IQueryHandler<Query, Response?>
    {
        public async Task<Response?> Handle(Query request, CancellationToken cancellationToken)
        {
            return await context.BackgroundCommandJobs
                .AsNoTracking()
                .Where(j => j.Id == request.Id)
                .Select(j => new Response
                {
                    Id = j.Id,
                    Type = j.Type,
                    Status = j.Status,
                    AttemptCount = j.AttemptCount,
                    NextAttemptAt = j.NextAttemptAt,
                    StartedAt = j.StartedAt,
                    CompletedAt = j.CompletedAt,
                    ResultVerspakketId = j.ResultVerspakketId,
                    LastError = j.LastError
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}