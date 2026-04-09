
using Cortex.Mediator.DependencyInjection;
using Lutra.Application.Verspakketten;
using Lutra.Application.Interfaces;
using Lutra.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace Lutra.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCortexMediator(
                handlerAssemblyMarkerTypes: [typeof(Program), typeof(GetVerspakketten)],
                options => options.AddDefaultBehaviors()
            );

            builder.Services.AddDbContext<ILutraDbContext, LutraDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("LutraDb")));

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

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
