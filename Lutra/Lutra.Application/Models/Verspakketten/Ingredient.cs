using Lutra.Domain.Entities;

namespace Lutra.Application.Models.Verspakketten;

/// <summary>
/// Represents an ingredient associated with a verspakket.
/// </summary>
public sealed record Ingredient(
    string Naam,
    decimal Hoeveelheid,
    Eenheid Eenheid,
    bool Inbegrepen);