using Payroc.LoadBalancer.Core.Models;
using System.Net;

namespace Payroc.LoadBalancer.Core.Services
{
    public class ServerSelectorService : IServerSelectorService
    {
        private long _counter = 0;
        public ServerSelectorService()
        {
            
        }

        // TODO improvement - change server selection logic based on selected algorithm
        // for round-robin will just use least served requests

        public IPEndPoint? GetNextServer(ServerState currentState)
        {
            var healthyServers = currentState.ServerStateDictionary.Where(x => x.Value.IsHealthy).Select(x => x.Key).ToList();
            if (!healthyServers.Any())
            {
                return null;
            }

            var newCounterValue = Interlocked.Increment(ref _counter);
            var count = healthyServers.Count;
            var index = (int)((newCounterValue - 1) % count);
            var ipEndpoint = healthyServers[index];
            var valueRetrieved = currentState.ServerStateDictionary.TryGetValue(ipEndpoint, out var selectedServer);
            if (!valueRetrieved) return ipEndpoint;
            var usageValue = selectedServer!.Usage.RequestsServed + 1;

            currentState.ServerStateDictionary.TryUpdate(ipEndpoint,
                selectedServer with { Usage = selectedServer.Usage with { RequestsServed = usageValue } },
                selectedServer);

            return ipEndpoint;
        }
    }
}
