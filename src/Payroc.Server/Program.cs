using Payroc.Server.Configuration;
using Payroc.Server.DependencyInjection;
using Payroc.Server.Endpoints;

namespace Payroc.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddOpenTelemetry();
            builder.Services.RegisterServices(builder.Configuration);
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            
            var app = builder.Build();
            app.MapEndpoints();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.MapHealthChecks("/health");
            await app.RunAsync();
        }
    }
}
