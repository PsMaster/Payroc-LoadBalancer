using System.Net;

namespace Payroc.LoadBalancer.Core.Services
{
    public interface IServerDiscoveryService
    {
        Task<List<IPEndPoint>> GetServers(string serverType, CancellationToken cancellationToken = default);
    }
}
