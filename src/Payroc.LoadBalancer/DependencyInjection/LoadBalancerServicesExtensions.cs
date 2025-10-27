using Payroc.LoadBalancer.Configuration;
using Payroc.LoadBalancer.Core.DependencyInjection;
using Payroc.LoadBalancer.Core.DependencyInjection.Options;
using Payroc.LoadBalancer.Core.Services;

namespace Payroc.LoadBalancer.DependencyInjection
{
    public static class LoadBalancerServicesExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var hostConfigurationOptions = new HostConfigurationOptions();
            configuration.GetSection(nameof(HostConfigurationOptions)).Bind(hostConfigurationOptions);
            serviceCollection.Configure<ConsulConfig>(configuration.GetSection(nameof(ConsulConfig)));
            serviceCollection.Configure<LoadBalancerServerOptions>(configuration.GetSection(nameof(LoadBalancerServerOptions)));

            serviceCollection.Configure<HostOptions>(o =>
            {
                o.BackgroundServiceExceptionBehavior = hostConfigurationOptions.BackgroundServiceExceptionBehavior;
                o.ShutdownTimeout = TimeSpan.FromSeconds(hostConfigurationOptions.ShutdownTimeoutInSeconds);
                o.StartupTimeout = TimeSpan.FromSeconds(hostConfigurationOptions.StartupTimeoutInSeconds);
                o.ServicesStartConcurrently = hostConfigurationOptions.ServicesStartConcurrently;
                o.ServicesStopConcurrently = hostConfigurationOptions.ServicesStopConcurrently;
            });
            serviceCollection.AddHostedService<WorkerService>();
            serviceCollection.RegisterLoadBalancerCoreServices(configuration);
            serviceCollection.AddHealthChecks();
            return serviceCollection;
        }
    }
}
