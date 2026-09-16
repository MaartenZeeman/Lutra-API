namespace Lutra.Application.BackgroundCommands;

/// <summary>Payload for a queued verspakket import.</summary>
public sealed record ImportVerspakketPayload(string Url);