using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payroc.LoadBalancer.Core.DependencyInjection.Options;
using Payroc.LoadBalancer.Core.Models;
using System.Net;

namespace Payroc.LoadBalancer.Core.Services
{
    public class ServerDiscoveryService : IServerDiscoveryService
    {
        private readonly IConsulClient _consulClient;
        private readonly ILogger<ServerDiscoveryService> _logger;
        // TODO improvement implementation
        private readonly IBackendServiceHealthChecker _healthChecker;
        private readonly LoadBalancerServerOptions _serverOptions;

        public ServerDiscoveryService(IConsulClient consulClient, ILogger<ServerDiscoveryService> logger,
            IBackendServiceHealthChecker healthChecker, IOptions<LoadBalancerServerOptions> serverOptions)
        {
            _consulClient = consulClient;
            _logger = logger;
            _healthChecker = healthChecker;
            _serverOptions = serverOptions.Value;
        }

        public async Task UpdateServers(string serverType, ServerState currentState, CancellationToken cancellationToken = default)
        {
            var queryResult = await _consulClient.Health.Service(
                serverType,
                string.Empty,
                true,
                cancellationToken
            );
            if (queryResult.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Service discovery failed with status code: {QueryResultStatusCode}", queryResult.StatusCode);
                return;
            }

            var healthyEndpoints = new List<IPEndPoint>();

            foreach (var serviceEntry in queryResult.Response)
            {
                var address = serviceEntry.Service.Address;
                var port = serviceEntry.Service.Port;

                if (string.IsNullOrEmpty(address)) continue;

                if (IPAddress.TryParse(address, out var ip))
                {
                    var endpoint = new IPEndPoint(ip, port);
                    healthyEndpoints.Add(endpoint);
                    var added = currentState.ServerStateDictionary.TryAdd(endpoint, new ServerStatus(true, DateTime.UtcNow, new ServerUsageMetrics(0, 0)));
                    if (!added)
                    {
                        var exists = currentState.ServerStateDictionary.TryGetValue(endpoint, out var existing);
                        if (exists)
                        {
                            currentState.ServerStateDictionary.TryUpdate(endpoint, new ServerStatus(true, DateTime.UtcNow, existing!.Usage), existing);
                        }
                    }
                    // TODO improvement - implement manual server health checks over http
                    // var healthStatus = await _healthChecker.CheckServerHealth();
                }
            }

            var unhealthyServers = currentState.ServerStateDictionary
                .ExceptBy(healthyEndpoints, x => x.Key).ToList();

            foreach (var unhealthy in unhealthyServers)
            {
                currentState.ServerStateDictionary.TryUpdate(unhealthy.Key, unhealthy.Value with { IsHealthy = false }, unhealthy.Value);
            }

            var oldServers = unhealthyServers.Where(x => x.Value.LastHealthyTimestamp <
                                                         DateTime.UtcNow.Subtract(
                                                             TimeSpan.FromSeconds(_serverOptions
                                                                 .OldServerRemovalAgeInSeconds)))
                .ToList();
            foreach (var oldServer in oldServers)
            {
                currentState.ServerStateDictionary.TryRemove(oldServer);
            }
        }
    }
}
