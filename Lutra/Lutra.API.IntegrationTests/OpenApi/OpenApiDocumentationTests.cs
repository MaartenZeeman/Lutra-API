using System.Net;
using System.Text.Json;
using FluentAssertions;
using Lutra.API.IntegrationTests.Infrastructure;

namespace Lutra.API.IntegrationTests.OpenApi;

/// <summary>
/// Verifies that the served OpenAPI document stays in sync with the actual API surface. The
/// document is generated from the controllers and their <c>[ProducesResponseType]</c> attributes,
/// so these tests fail whenever an endpoint, response or schema is added or changed without the
/// contract being regenerated.
/// </summary>
public class OpenApiDocumentationTests(LutraApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly string[] ExpectedPaths =
    [
        "/health",
        "/api/supermarkten",
        "/api/supermarkten/{id}",
        "/api/verspakketten",
        "/api/verspakketten/{id}",
        "/api/verspakketten/import",
        "/api/verspakketten/{id}/beoordelingen",
        "/api/background-commands/{id}"
    ];

    private static readonly string[] ExpectedSchemas =
    [
        "Verspakket",
        "VerspakketSummary",
        "Supermarkt",
        "Beoordeling",
        "Ingredient",
        "Voedingswaarde",
        "VerspakketFotoResponse",
        "Allergeen",
        "Eenheid",
        "VoedingswaardeBasis",
        "VerspakketSortField",
        "SortDirection",
        "GetVerspakkettenResponse",
        "GetVerspakketResponse",
        "CreateVerspakketResponse",
        "AddBeoordelingResponse",
        "EnqueueImportVerspakketResponse",
        "GetBackgroundCommandResponse",
        "BackgroundCommandStatus",
        "GetSupermarktenResponse",
        "CreateSupermarktResponse",
        "CreateVerspakketRequest",
        "UpdateVerspakketRequest",
        "AddBeoordelingRequest",
        "ImportVerspakketRequest",
        "SupermarktRequest",
        "VerspakketFotoRequest",
        "IngredientRequest",
        "VoedingswaardeRequest",
        "ProblemDetails"
    ];

    [Fact]
    public async Task OpenApiDocument_IsServed()
    {
        using var document = await GetDocumentAsync();

        document.RootElement.GetProperty("openapi").GetString().Should().StartWith("3.");
        document.RootElement.GetProperty("info").GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.TryGetProperty("paths", out _).Should().BeTrue();
        document.RootElement.TryGetProperty("components", out _).Should().BeTrue();
    }

    [Fact]
    public async Task OpenApiDocument_ListsEveryEndpoint()
    {
        using var document = await GetDocumentAsync();

        var paths = document.RootElement
            .GetProperty("paths")
            .EnumerateObject()
            .Select(path => path.Name)
            .ToHashSet();

        paths.Should().Contain(ExpectedPaths);
    }

    [Fact]
    public async Task OpenApiDocument_EveryOperationDeclaresResponses()
    {
        using var document = await GetDocumentAsync();

        var paths = document.RootElement.GetProperty("paths");

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject().Where(IsOperation))
            {
                var declared = operation.Value.TryGetProperty("responses", out var responses);
                declared.Should().BeTrue($"{operation.Name.ToUpperInvariant()} {path.Name} must declare responses");
                responses.EnumerateObject().Should().NotBeEmpty();
            }
        }
    }

    [Fact]
    public async Task OpenApiDocument_ListsSchemasConsumedByClients()
    {
        using var document = await GetDocumentAsync();

        var schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .EnumerateObject()
            .Select(schema => schema.Name)
            .ToHashSet();

        schemas.Should().Contain(ExpectedSchemas);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesVerspakkettenOperations()
    {
        using var document = await GetDocumentAsync();

        var verspakketten = document.RootElement.GetProperty("paths").GetProperty("/api/verspakketten");

        var get = verspakketten.GetProperty("get").GetProperty("responses");
        RefersTo(get, "200", "GetVerspakkettenResponse").Should().BeTrue();

        var create = verspakketten.GetProperty("post").GetProperty("responses");
        RefersTo(create, "201", "CreateVerspakketResponse").Should().BeTrue();
        HasResponse(create, "400").Should().BeTrue($"{nameof(create)} must document validation errors");
        HasResponse(create, "404").Should().BeTrue($"{nameof(create)} must document an unknown supermarkt");
    }

    [Fact]
    public async Task OpenApiDocument_DescribesUpdateAndBeoordelingOperations()
    {
        using var document = await GetDocumentAsync();

        var paths = document.RootElement.GetProperty("paths");

        var update = paths.GetProperty("/api/verspakketten/{id}").GetProperty("put").GetProperty("responses");
        HasResponse(update, "204").Should().BeTrue();
        HasResponse(update, "400").Should().BeTrue();
        HasResponse(update, "404").Should().BeTrue();

        var beoordeling = paths.GetProperty("/api/verspakketten/{id}/beoordelingen").GetProperty("post").GetProperty("responses");
        RefersTo(beoordeling, "201", "AddBeoordelingResponse").Should().BeTrue();
        HasResponse(beoordeling, "404").Should().BeTrue();
    }

    [Fact]
    public async Task OpenApiDocument_DescribesImportAndBackgroundOperations()
    {
        using var document = await GetDocumentAsync();

        var paths = document.RootElement.GetProperty("paths");

        var import = paths.GetProperty("/api/verspakketten/import").GetProperty("post").GetProperty("responses");
        RefersTo(import, "202", "EnqueueImportVerspakketResponse").Should().BeTrue();
        HasResponse(import, "400").Should().BeTrue();
        HasResponse(import, "429").Should().BeTrue();

        var background = paths.GetProperty("/api/background-commands/{id}").GetProperty("get").GetProperty("responses");
        RefersTo(background, "200", "GetBackgroundCommandResponse").Should().BeTrue();
        HasResponse(background, "404").Should().BeTrue();
    }

    [Fact]
    public async Task OpenApiDocument_DescribesSupermarktenOperations()
    {
        using var document = await GetDocumentAsync();

        var paths = document.RootElement.GetProperty("paths");

        var list = paths.GetProperty("/api/supermarkten").GetProperty("get").GetProperty("responses");
        RefersTo(list, "200", "GetSupermarktenResponse").Should().BeTrue();

        var create = paths.GetProperty("/api/supermarkten").GetProperty("post").GetProperty("responses");
        RefersTo(create, "201", "CreateSupermarktResponse").Should().BeTrue();
        HasResponse(create, "400").Should().BeTrue();
    }

    private static bool IsOperation(JsonProperty operation) =>
        operation.Name is "get" or "post" or "put" or "patch" or "delete" or "head" or "options";

    private static bool HasResponse(JsonElement responses, string statusCode) =>
        responses.TryGetProperty(statusCode, out _);

    private static bool RefersTo(JsonElement responses, string statusCode, string schemaName) =>
        responses.TryGetProperty(statusCode, out var status)
            && status.TryGetProperty("content", out var content)
            && content.TryGetProperty("application/json", out var mediaType)
            && mediaType.TryGetProperty("schema", out var schema)
            && schema.TryGetProperty("$ref", out var reference)
            && reference.GetString() == $"#/components/schemas/{schemaName}";

    private async Task<JsonDocument> GetDocumentAsync()
    {
        var response = await Client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json);
    }
}