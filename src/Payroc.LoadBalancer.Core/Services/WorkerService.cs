using Microsoft.Extensions.Hosting;

namespace Payroc.LoadBalancer.Core.Services
{
    public class WorkerService : IHostedService
    {
        private readonly ILoadBalancerService _loadBalancerService;

        public WorkerService(ILoadBalancerService loadBalancerService)
        {
            _loadBalancerService = loadBalancerService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _loadBalancerService.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _loadBalancerService.StopAsync(cancellationToken);
        }
    }
}
