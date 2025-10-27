using Payroc.LoadBalancer.Core.Models;
using System.Net;

namespace Payroc.LoadBalancer.Core.Services
{
    public interface IServerDiscoveryService
    {
        Task UpdateServers(string serverType, ServerState currentState, CancellationToken cancellationToken = default);
    }
}
