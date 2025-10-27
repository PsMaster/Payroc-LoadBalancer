using Payroc.LoadBalancer.DependencyInjection;
using Payroc.LoadBalancer.Endpoints;

namespace Payroc.LoadBalancer
{
    internal class Program
    {
        static async Task Main()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8080);
            });
            builder.Services.RegisterServices(builder.Configuration);

            var app = builder.Build();
            app.MapApiEndpoints();
            await app.RunAsync();
        }
    }
}
