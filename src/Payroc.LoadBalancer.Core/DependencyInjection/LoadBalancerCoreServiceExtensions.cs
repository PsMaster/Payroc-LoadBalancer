using Microsoft.Extensions.DependencyInjection;
using Payroc.LoadBalancer.Core.Models;
using Payroc.LoadBalancer.Core.Services;
using System.Threading.Channels;

namespace Payroc.LoadBalancer.Core.DependencyInjection
{
    public static class LoadBalancerCoreServiceExtensions
    {
        public static IServiceCollection RegisterLoadBalancerCoreServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ILoadBalancerService, LoadBalancerService>();
            serviceCollection.AddSingleton(Channel.CreateUnbounded<ControlCommand>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true }));
            return serviceCollection;
        }
    }
}
