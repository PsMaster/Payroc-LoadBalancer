using Consul;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;

namespace Payroc.LoadBalancer.Core.Services
{
    public class ServerDiscoveryService : IServerDiscoveryService
    {
        private readonly IConsulClient _consulClient;
        private readonly ILogger<ServerDiscoveryService> _logger;

        public ServerDiscoveryService(IConsulClient consulClient, ILogger<ServerDiscoveryService> logger)
        {
            _consulClient = consulClient;
            _logger = logger;
        }

        public async Task<List<IPEndPoint>> GetServers(string serverType, CancellationToken cancellationToken = default)
        {
            var queryResult = await _consulClient.Health.Service(
                serverType,
                string.Empty,
                false,
                cancellationToken
            );
            if (queryResult.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError($"Service discovery failed with status code: {queryResult.StatusCode}");
                return [];
            }

            var serverList = new ConcurrentBag<IPEndPoint>();

            foreach (var serviceEntry in queryResult.Response)
            {
                var address = serviceEntry.Node.Address;
                var port = serviceEntry.Service.Port;

                if (string.IsNullOrEmpty(address)) continue;

                if (IPAddress.TryParse(address, out var ip))
                {
                    serverList.Add(new IPEndPoint(ip, port));
                }
            }

            return serverList.ToList();
        }
    }
}
