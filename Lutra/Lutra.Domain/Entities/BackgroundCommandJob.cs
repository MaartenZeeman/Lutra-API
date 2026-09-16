using System.ComponentModel.DataAnnotations;

namespace Lutra.Domain.Entities;

/// <summary>
/// Durable record of a command that must run in the background and survive process restarts.
/// </summary>
public class BackgroundCommandJob : BaseEntity
{
    public required BackgroundCommandType Type { get; set; }

    /// <summary>JSON-serialized command payload.</summary>
    [MaxLength(4096)]
    public required string Payload { get; set; }

    /// <summary>Key used to avoid enqueuing the same work while a job is still active.</summary>
    [MaxLength(2048)]
    public string? DeduplicationKey { get; set; }

    /// <summary>Set while the job is active so the unique index ignores finished jobs; cleared on completion.</summary>
    [MaxLength(2048)]
    public string? ActiveDeduplicationKey { get; set; }

    public required BackgroundCommandStatus Status { get; set; }

    public required bool IsActive { get; set; }

    public required int AttemptCount { get; set; }

    public required DateTime NextAttemptAt { get; set; }

    public Guid? LeaseToken { get; set; }

    public DateTime? LeaseExpiresAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid? ResultVerspakketId { get; set; }

    [MaxLength(2048)]
    public string? LastError { get; set; }

    /// <summary>Optimistic concurrency token used to make multi-worker claiming safe.</summary>
    public required Guid ConcurrencyStamp { get; set; }
}