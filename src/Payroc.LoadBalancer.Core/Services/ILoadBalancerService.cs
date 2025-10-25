namespace Payroc.LoadBalancer.Core.Services
{
    public interface ILoadBalancerService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
