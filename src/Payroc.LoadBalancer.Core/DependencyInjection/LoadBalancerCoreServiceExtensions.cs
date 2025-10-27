using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payroc.LoadBalancer.Core.DependencyInjection.Options;
using Payroc.LoadBalancer.Core.Services;

namespace Payroc.LoadBalancer.Core.DependencyInjection
{
    public static class LoadBalancerCoreServiceExtensions
    {
        public static IServiceCollection RegisterLoadBalancerCoreServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddSingleton<ILoadBalancerService, LoadBalancerService>();
            serviceCollection.AddSingleton<IServerDiscoveryService, ServerDiscoveryService>();
            serviceCollection.AddSingleton<IConsulClient, ConsulClient>(p =>
            {
                var consulAddress = configuration["ConsulConfig:ConsulAddress"] ?? "http://consul:8500";
                return new ConsulClient(cfg =>
                {
                    cfg.Address = new Uri(consulAddress!);
                });
            });
            return serviceCollection;
        }
    }
}
