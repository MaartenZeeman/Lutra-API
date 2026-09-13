using Lutra.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Lutra.API.Middleware;

/// <summary>
/// Translates application exceptions into RFC 7807 problem responses.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (UnprocessableException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status422UnprocessableEntity, ex.Message);
        }
        catch (ExternalServiceException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status502BadGateway, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception during request processing.");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                StatusCodes.Status400BadRequest => "Bad Request",
                StatusCodes.Status404NotFound => "Not Found",
                StatusCodes.Status409Conflict => "Conflict",
                StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
                StatusCodes.Status502BadGateway => "Bad Gateway",
                _ => "Internal Server Error"
            },
            Detail = message
        });
    }
}