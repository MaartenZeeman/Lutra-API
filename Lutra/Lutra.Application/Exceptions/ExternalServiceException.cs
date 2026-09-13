namespace Lutra.Application.Exceptions;

/// <summary>Thrown when an external dependency (retailer page, AI provider, image host) fails. Maps to HTTP 502.</summary>
public sealed class ExternalServiceException : Exception
{
    public ExternalServiceException(string message) : base(message) { }

    public ExternalServiceException(string message, Exception innerException) : base(message, innerException) { }
}
