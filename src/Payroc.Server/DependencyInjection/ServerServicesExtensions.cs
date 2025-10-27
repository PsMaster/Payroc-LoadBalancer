using Consul;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Payroc.Server.Meters;
using Payroc.Server.Services;

namespace Payroc.Server.DependencyInjection
{
    public static class ServerServicesExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection serviceCollection, IConfiguration configuration)
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
            serviceCollection.AddHealthChecks().AddCheck<HealthStatusChecker>("Status check", Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);

            serviceCollection.AddSingleton<IConsulClient, ConsulClient>(p =>
            {
                var consulAddress = configuration["ConsulConfig:ConsulAddress"] ?? "http://consul:8500";
                return new ConsulClient(cfg =>
                {
                    cfg.Address = new Uri(consulAddress!);
                });
            });

            serviceCollection.AddHostedService<ServerRegistrationService>();

            return serviceCollection;
        }
    }
}
