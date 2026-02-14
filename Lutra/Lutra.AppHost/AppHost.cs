var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
                 .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase("database", "Lutra");

var apiService = builder.AddProject<Projects.Lutra_API>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
