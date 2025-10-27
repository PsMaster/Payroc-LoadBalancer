using Payroc.LoadBalancer.Core.Models;

namespace Payroc.LoadBalancer.Core.Services
{
    public class BackendServiceHealthChecker : IBackendServiceHealthChecker
    {
        // TODO for future improvement
        public async Task<ServerStatus> CheckServerHealth()
        {
            return new ServerStatus(true, DateTime.UtcNow, new ServerUsageMetrics(0,0));
        }
    }
}
