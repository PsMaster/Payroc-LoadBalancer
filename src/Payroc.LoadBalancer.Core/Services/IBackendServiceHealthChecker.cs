using Payroc.LoadBalancer.Core.Models;

namespace Payroc.LoadBalancer.Core.Services
{
    public interface IBackendServiceHealthChecker
    {
        Task<ServerStatus> CheckServerHealth();
    }
}
