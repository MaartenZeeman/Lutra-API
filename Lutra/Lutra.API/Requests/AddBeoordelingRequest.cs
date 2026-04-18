using System.ComponentModel.DataAnnotations;

namespace Lutra.API.Requests;

/// <summary>
/// Represents the data required to add a beoordeling to a verspakket.
/// </summary>
public sealed record AddBeoordelingRequest(
    [Range(1, 10)] int CijferSmaak,
    [Range(1, 10)] int CijferBereiden,
    bool Aanbevolen,
    [MaxLength(1024)] string? Tekst);