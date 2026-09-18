using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;

namespace Lutra.API.OpenApi;

/// <summary>
/// Configures the OpenAPI contract published by the API and consumed by the frontend.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>Document name used for the versioned contract.</summary>
    public const string DocumentName = "v1";

    /// <summary>
    /// Registers the versioned OpenAPI document with schema names that disambiguate the
    /// use-case-nested response records (all named <c>Response</c>).
    /// </summary>
    public static IServiceCollection AddLutraOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.CreateSchemaReferenceId = CreateSchemaReferenceId;
        });

        return services;
    }

    /// <summary>
    /// Prefixes nested types with their declaring type name (for example
    /// <c>GetVerspakketten.Response</c> becomes <c>GetVerspakkettenResponse</c>) and otherwise
    /// keeps the framework default so collection and inlined schemas are unaffected.
    /// </summary>
    internal static string? CreateSchemaReferenceId(JsonTypeInfo jsonTypeInfo)
    {
        var declaringTypeName = jsonTypeInfo.Type.DeclaringType?.Name;

        return declaringTypeName is null
            ? OpenApiOptions.CreateDefaultSchemaReferenceId(jsonTypeInfo)
            : $"{declaringTypeName}{jsonTypeInfo.Type.Name}";
    }
}
