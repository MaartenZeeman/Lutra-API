var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres
    .AddDatabase("LutraDb");

var migrator = builder.AddProject<Projects.Lutra_Infrastructure_Migrator>("dbmigrator")
    .WithReference(db)
    .WaitFor(db);

var apiService = builder.AddProject<Projects.Lutra_API>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitForCompletion(migrator);

// The Vue frontend proxies /api to the discovered API endpoint, so the browser keeps
// talking to the Vite origin and no CORS configuration is required.
builder.AddViteApp("frontend", "../../../lutra-Vue")
    .WithReference(apiService)
    .WithEnvironment("VITE_API_BASE_URL", apiService.GetEndpoint("https"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
