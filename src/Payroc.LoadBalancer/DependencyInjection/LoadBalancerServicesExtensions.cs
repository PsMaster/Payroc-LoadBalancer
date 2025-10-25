using Payroc.LoadBalancer.Core.Services;
using Payroc.LoadBalancer.Endpoints.Requests;
using System.Threading.Channels;
using Payroc.LoadBalancer.Configuration;

namespace Payroc.LoadBalancer.DependencyInjection
{
    public static class LoadBalancerServicesExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var hostConfigurationOptions = new HostConfigurationOptions();
            configuration.GetSection(nameof(HostConfigurationOptions)).Bind(hostConfigurationOptions);
            serviceCollection.AddSingleton(Channel.CreateUnbounded<ControlCommand>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true }));
            serviceCollection.Configure<HostOptions>(o =>
            {
                o.BackgroundServiceExceptionBehavior = hostConfigurationOptions.BackgroundServiceExceptionBehavior;
                o.ShutdownTimeout = TimeSpan.FromSeconds(hostConfigurationOptions.ShutdownTimeoutInSeconds);
                o.StartupTimeout = TimeSpan.FromSeconds(hostConfigurationOptions.StartupTimeoutInSeconds);
                o.ServicesStartConcurrently = hostConfigurationOptions.ServicesStartConcurrently;
                o.ServicesStopConcurrently = hostConfigurationOptions.ServicesStopConcurrently;
            });
            serviceCollection.AddHostedService<WorkerService>();
            serviceCollection.AddHealthChecks();
            return serviceCollection;
        }
    }
}
