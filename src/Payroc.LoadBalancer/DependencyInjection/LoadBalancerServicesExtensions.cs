using Payroc.LoadBalancer.Core.Services;
using Payroc.LoadBalancer.Endpoints.Requests;
using System.Threading.Channels;
using Payroc.LoadBalancer.Configuration;
using Payroc.LoadBalancer.Core.DependencyInjection;

namespace Payroc.LoadBalancer.DependencyInjection
{
    public static class LoadBalancerServicesExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var hostConfigurationOptions = new HostConfigurationOptions();
            configuration.GetSection(nameof(HostConfigurationOptions)).Bind(hostConfigurationOptions);
            serviceCollection.Configure<HostOptions>(o =>
            {
                o.BackgroundServiceExceptionBehavior = hostConfigurationOptions.BackgroundServiceExceptionBehavior;
                o.ShutdownTimeout = TimeSpan.FromSeconds(hostConfigurationOptions.ShutdownTimeoutInSeconds);
                o.StartupTimeout = TimeSpan.FromSeconds(hostConfigurationOptions.StartupTimeoutInSeconds);
                o.ServicesStartConcurrently = hostConfigurationOptions.ServicesStartConcurrently;
                o.ServicesStopConcurrently = hostConfigurationOptions.ServicesStopConcurrently;
            });
            serviceCollection.AddHostedService<WorkerService>();
            serviceCollection.RegisterLoadBalancerCoreServices();
            serviceCollection.AddHealthChecks();
            return serviceCollection;
        }
    }
}
