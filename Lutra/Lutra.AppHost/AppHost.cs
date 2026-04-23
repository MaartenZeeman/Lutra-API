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

builder.Build().Run();
