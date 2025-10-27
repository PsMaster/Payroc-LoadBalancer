using Microsoft.Extensions.Hosting;

namespace Payroc.LoadBalancer.Core.Services
{
    public class WorkerService : BackgroundService
    {
        private readonly ILoadBalancerService _loadBalancerService;

        public WorkerService(ILoadBalancerService loadBalancerService)
        {
            _loadBalancerService = loadBalancerService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await _loadBalancerService.StartAsync(cancellationToken);
        }
    }
}
