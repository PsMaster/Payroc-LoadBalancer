using Payroc.LoadBalancer.Core.Models;
using System.Net;

namespace Payroc.LoadBalancer.Core.Services
{
    public class ServerSelectorService : IServerSelectorService
    {
        
        public ServerSelectorService()
        {
            
        }

        // TODO improvement - change server selection logic based on selected algorithm
        // for round-robin will just use least served requests

        public IPEndPoint? GetNextServer(ServerState currentState)
        {
            var leastBusyHealthyServer = currentState.ServerStateDictionary.Where(x => x.Value.IsHealthy)
                .OrderBy(x => x.Value.Usage.RequestsServed).FirstOrDefault();
            if (leastBusyHealthyServer.Key == null)
            {
                return null;
            }

            var usageValue = leastBusyHealthyServer.Value.Usage.RequestsServed+1;

            currentState.ServerStateDictionary.TryUpdate(leastBusyHealthyServer.Key,
                leastBusyHealthyServer.Value with { Usage = leastBusyHealthyServer.Value.Usage with { RequestsServed = usageValue } },
                leastBusyHealthyServer.Value);
            return leastBusyHealthyServer.Key;
        }
    }
}
