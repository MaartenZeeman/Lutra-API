var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddConnectionString("LutraDb");

var migrator = builder.AddProject<Projects.Lutra_Infrastructure_Migrator>("dbmigrator")
    .WithReference(db);

var apiService = builder.AddProject<Projects.Lutra_API>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitForCompletion(migrator);

builder.Build().Run();
