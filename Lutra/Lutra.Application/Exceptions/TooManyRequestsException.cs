namespace Lutra.Application.Exceptions;

/// <summary>
/// Thrown when a use case rejects a request because a resource limit or rate limit was reached.
/// Maps to HTTP 429.
/// </summary>
public sealed class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message) : base(message) { }
}