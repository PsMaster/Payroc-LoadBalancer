using System.Net;
using Payroc.LoadBalancer.Core.Models;

namespace Payroc.LoadBalancer.Core.Services
{
    public interface IServerSelectorService
    {
        IPEndPoint? GetNextServer(ServerState currentState);
    }
}
