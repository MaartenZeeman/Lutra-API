namespace Lutra.Domain.Entities;

/// <summary>Lifecycle state of a background command job.</summary>
public enum BackgroundCommandStatus
{
    Queued = 0,
    Processing = 1,
    RetryScheduled = 2,
    Succeeded = 3,
    Failed = 4
}