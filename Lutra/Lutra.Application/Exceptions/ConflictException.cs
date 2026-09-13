namespace Lutra.Application.Exceptions;

/// <summary>Thrown when an import conflicts with existing data. Maps to HTTP 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
