using Lutra.Domain.Entities;

namespace Lutra.Application.Verspakketten;

public sealed partial class EnqueueImportVerspakket
{
    public sealed record Response
    {
        public required Guid Id { get; init; }

        public required BackgroundCommandStatus Status { get; init; }

        public required int AttemptCount { get; init; }

        public DateTime? NextAttemptAt { get; init; }

        /// <summary>True when an active job for the same product already existed and was returned.</summary>
        public required bool Existing { get; init; }
    }
}