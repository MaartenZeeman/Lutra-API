namespace Lutra.Application.Exceptions;

/// <summary>Thrown when an operation cannot be completed because required data is missing. Maps to HTTP 422.</summary>
public sealed class UnprocessableException : Exception
{
    public UnprocessableException(string message) : base(message) { }
}
