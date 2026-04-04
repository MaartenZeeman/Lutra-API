using Lutra.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<LutraDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LutraDb")));

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<LutraDbContext>();
await dbContext.Database.MigrateAsync();
