namespace Lutra.Application.BackgroundCommands;

/// <summary>Configuration for the durable background command worker.</summary>
public sealed class BackgroundCommandsOptions
{
    public const string SectionName = "BackgroundCommands";

    /// <summary>When false the hosted worker does not start; jobs can still be enqueued and processed manually.</summary>
    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 10;

    public int RetryDelayMinutes { get; set; } = 5;

    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// How long a claim is valid. Must comfortably exceed the worst-case runtime of a job
    /// (OpenRouter timeout plus redirect and image fetch timeouts) so an in-flight job is not
    /// reclaimed and executed twice while it is still running.
    /// </summary>
    public int LeaseDurationMinutes { get; set; } = 20;

    public int BatchSize { get; set; } = 5;
}