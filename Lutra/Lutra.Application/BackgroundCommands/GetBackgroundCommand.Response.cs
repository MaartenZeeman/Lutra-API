using Lutra.Domain.Entities;

namespace Lutra.Application.BackgroundCommands;

public sealed partial class GetBackgroundCommand
{
    public sealed record Response
    {
        public required Guid Id { get; init; }

        public required BackgroundCommandType Type { get; init; }

        public required BackgroundCommandStatus Status { get; init; }

        public required int AttemptCount { get; init; }

        public DateTime? NextAttemptAt { get; init; }

        public DateTime? StartedAt { get; init; }

        public DateTime? CompletedAt { get; init; }

        public Guid? ResultVerspakketId { get; init; }

        public string? LastError { get; init; }
    }
}