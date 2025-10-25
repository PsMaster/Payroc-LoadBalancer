using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Payroc.Server.Meters;
using Payroc.Server.Services;

namespace Payroc.Server.DependencyInjection
{
    public static class ServerServicesExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ServerMetrics>();

            serviceCollection.AddOpenTelemetry()
                .ConfigureResource(res => res.AddService(ServerMetrics.ServiceName))
                .WithMetrics(m =>
                {
                    m.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();
                    m.AddMeter(ServerMetrics.RequestsCounterName);
                    m.AddOtlpExporter();
                })
                .WithTracing(t =>
                {
                    t.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();
                    t.AddOtlpExporter();
                });

            serviceCollection.AddSingleton<IHealthStatusService, HealthStatusService>();
            serviceCollection.AddHealthChecks().AddCheck<HealthStatusChecker>("Status check", HealthStatus.Healthy);

            return serviceCollection;
        }
    }
}
