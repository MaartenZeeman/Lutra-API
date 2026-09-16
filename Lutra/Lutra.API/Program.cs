
using Cortex.Mediator.DependencyInjection;
using Lutra.API.BackgroundCommands;
using Lutra.API.Middleware;
using Lutra.Application.BackgroundCommands;
using Lutra.Application.Verspakketten;
using Lutra.Application.Interfaces;
using Lutra.Infrastructure.OpenRouter;
using Lutra.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace Lutra.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalDevelopment", policy =>
                    policy.SetIsOriginAllowed(origin =>
                    {
                        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                        {
                            return false;
                        }

                        return uri.Host is "localhost" or "127.0.0.1" or "[::1]";
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });

            builder.Services.AddCortexMediator(
                handlerAssemblyMarkerTypes: [typeof(Program), typeof(GetVerspakketten)],
                options => options.AddDefaultBehaviors()
            );

            builder.Services.AddDbContext<ILutraDbContext, LutraDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("LutraDb")));

            builder.Services.Configure<OpenRouterOptions>(
                builder.Configuration.GetSection(OpenRouterOptions.SectionName));

            builder.Services.AddHttpClient("VerspakketRetail")
                .ConfigureHttpClient((serviceProvider, client) =>
                {
                    var openRouterOptions = serviceProvider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
                    client.Timeout = TimeSpan.FromSeconds(openRouterOptions.RetailTimeoutSeconds);
                })
                .ConfigurePrimaryHttpMessageHandler(() => PublicNetworkHttpHandler.Create());
            builder.Services.AddHttpClient("OpenRouter");
            builder.Services.AddTransient<IVerspakketProductExtractor, OpenRouterVerspakketExtractor>();

            builder.Services.Configure<BackgroundCommandsOptions>(
                builder.Configuration.GetSection(BackgroundCommandsOptions.SectionName));
            builder.Services.AddScoped<BackgroundCommandProcessor>();
            builder.Services.AddHostedService<BackgroundCommandWorker>();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();


            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
                app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowLocalDevelopment");

            app.UseAuthorization();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.MapControllers();

            app.Run();
        }
    }
}
