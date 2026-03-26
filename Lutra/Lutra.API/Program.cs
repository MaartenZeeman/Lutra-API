
using Cortex.Mediator.DependencyInjection;
using Lutra.Application.Verspakketten;
using Lutra.Application.Interfaces;
using Lutra.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;

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
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
