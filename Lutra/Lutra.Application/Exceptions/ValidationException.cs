namespace Lutra.Application.Exceptions;

/// <summary>
/// Thrown when a use case receives invalid input. Maps to HTTP 400.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}