using Microsoft.OpenApi;

namespace Lutra.API.OpenApi;

/// <summary>
/// Collapses every line ending in the generated OpenAPI contract to LF so the committed document
/// is identical no matter which platform produced it.
/// </summary>
/// <remarks>
/// XML documentation comments are copied into the contract verbatim, including the line endings of
/// the source file they were read from. Git checks the sources out with CRLF on Windows and LF on
/// Linux, so without normalization the same sources would emit different contracts and the
/// committed contract would fail the CI freshness check on one of the platforms.
/// </remarks>
internal static class OpenApiLineEndingNormalizer
{
    public static void Normalize(OpenApiDocument document)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);

        NormalizeElement(document.Info, visited);
        NormalizeElement(document.ExternalDocs, visited);
        NormalizeElement(document.Components, visited);
        NormalizeAll(document.Servers, visited);
        NormalizeAll(document.Tags, visited);
        NormalizeAll(document.Paths?.Values, visited);
    }

    private static void NormalizeAll<T>(IEnumerable<T>? elements, HashSet<object> visited)
    {
        if (elements is null)
        {
            return;
        }

        foreach (var element in elements)
        {
            NormalizeElement(element, visited);
        }
    }

    // Only the concrete model types are rewritten. Reference types (for example
    // OpenApiSchemaReference) proxy the members of the element they point at, so assigning to them
    // would materialize those members next to the $ref and change the contract.
    private static void NormalizeElement(object? element, HashSet<object> visited)
    {
        if (element is null || !visited.Add(element))
        {
            return;
        }

        switch (element)
        {
            case OpenApiInfo info:
                info.Title = Normalize(info.Title);
                info.Summary = Normalize(info.Summary);
                info.Description = Normalize(info.Description);
                break;

            case OpenApiSchema schema:
                schema.Description = Normalize(schema.Description);
                NormalizeElement(schema.Items, visited);
                NormalizeElement(schema.Not, visited);
                NormalizeElement(schema.AdditionalProperties, visited);
                NormalizeElement(schema.ExternalDocs, visited);
                NormalizeAll(schema.Properties?.Values, visited);
                NormalizeAll(schema.AllOf, visited);
                NormalizeAll(schema.OneOf, visited);
                NormalizeAll(schema.AnyOf, visited);
                break;

            case OpenApiPathItem pathItem:
                pathItem.Summary = Normalize(pathItem.Summary);
                pathItem.Description = Normalize(pathItem.Description);
                NormalizeAll(pathItem.Operations?.Values, visited);
                NormalizeAll(pathItem.Parameters, visited);
                NormalizeAll(pathItem.Servers, visited);
                break;

            case OpenApiOperation operation:
                operation.Summary = Normalize(operation.Summary);
                operation.Description = Normalize(operation.Description);
                NormalizeAll(operation.Parameters, visited);
                NormalizeElement(operation.RequestBody, visited);
                NormalizeAll(operation.Responses?.Values, visited);
                NormalizeAll(operation.Servers, visited);
                NormalizeElement(operation.ExternalDocs, visited);
                break;

            case OpenApiParameter parameter:
                parameter.Description = Normalize(parameter.Description);
                NormalizeElement(parameter.Schema, visited);
                NormalizeAll(parameter.Content?.Values, visited);
                NormalizeAll(parameter.Examples?.Values, visited);
                break;

            case OpenApiRequestBody requestBody:
                requestBody.Description = Normalize(requestBody.Description);
                NormalizeAll(requestBody.Content?.Values, visited);
                break;

            case OpenApiResponse response:
                response.Description = Normalize(response.Description);
                NormalizeAll(response.Content?.Values, visited);
                NormalizeAll(response.Headers?.Values, visited);
                NormalizeAll(response.Links?.Values, visited);
                break;

            case OpenApiHeader header:
                header.Description = Normalize(header.Description);
                NormalizeElement(header.Schema, visited);
                NormalizeAll(header.Content?.Values, visited);
                NormalizeAll(header.Examples?.Values, visited);
                break;

            case OpenApiMediaType mediaType:
                NormalizeElement(mediaType.Schema, visited);
                NormalizeAll(mediaType.Examples?.Values, visited);
                break;

            case OpenApiExample example:
                example.Summary = Normalize(example.Summary);
                example.Description = Normalize(example.Description);
                break;

            case OpenApiTag tag:
                tag.Description = Normalize(tag.Description);
                NormalizeElement(tag.ExternalDocs, visited);
                break;

            case OpenApiSecurityScheme securityScheme:
                securityScheme.Description = Normalize(securityScheme.Description);
                break;

            case OpenApiLink link:
                link.Description = Normalize(link.Description);
                break;

            case OpenApiServer server:
                server.Description = Normalize(server.Description);
                break;

            case OpenApiExternalDocs externalDocs:
                externalDocs.Description = Normalize(externalDocs.Description);
                break;

            case OpenApiComponents components:
                NormalizeAll(components.Schemas?.Values, visited);
                NormalizeAll(components.Responses?.Values, visited);
                NormalizeAll(components.Parameters?.Values, visited);
                NormalizeAll(components.Examples?.Values, visited);
                NormalizeAll(components.RequestBodies?.Values, visited);
                NormalizeAll(components.Headers?.Values, visited);
                NormalizeAll(components.SecuritySchemes?.Values, visited);
                NormalizeAll(components.Links?.Values, visited);
                NormalizeAll(components.PathItems?.Values, visited);
                break;
        }
    }

    private static string? Normalize(string? value) =>
        value?.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
