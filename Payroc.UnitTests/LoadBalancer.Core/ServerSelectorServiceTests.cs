using Payroc.LoadBalancer.Core.Models;
using Payroc.LoadBalancer.Core.Services;
using System.Collections.Concurrent;
using System.Net;

namespace Payroc.UnitTests.LoadBalancer.Core
{
    public class ServerSelectorServiceTests
    {
        private ServerState CreateTestServerState(int count, bool allHealthy = true)
        {
            var dict = new ConcurrentDictionary<IPEndPoint, ServerStatus>();
            var initialMetrics = new ServerUsageMetrics(0, 0);
            var initialStatus = new ServerStatus(true, DateTime.UtcNow, initialMetrics);

            for (var i = 1; i <= count; i++)
            {
                var ip = IPAddress.Parse($"10.0.0.{i}");
                var endpoint = new IPEndPoint(ip, 80);

                var isHealthy = allHealthy || (i % 2 != 0);
                var status = isHealthy ? initialStatus : new ServerStatus(false, DateTime.UtcNow, initialMetrics);

                dict.TryAdd(endpoint, status);
            }
            return new ServerState(dict);
        }

        [Fact]
        public void GetNextServer_ReturnsNull_WhenNoHealthyServersExist()
        {
            // Arrange
            var service = new ServerSelectorService();
            var state = CreateTestServerState(3, allHealthy: true);

            var unhealthyStateDict = new ConcurrentDictionary<IPEndPoint, ServerStatus>();
            foreach (var kvp in state.ServerStateDictionary)
            {
                unhealthyStateDict.TryAdd(kvp.Key, kvp.Value with { IsHealthy = false });
            }
            var unhealthyState = new ServerState(unhealthyStateDict);

            // Act
            var result = service.GetNextServer(unhealthyState);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetNextServer_CyclesSequentially()
        {
            // Arrange
            var service = new ServerSelectorService();
            var state = CreateTestServerState(3, allHealthy: true);
            var healthyEndpoints = state.ServerStateDictionary.Keys.ToList();

            // Expected sequence of endpoints
            var expectedEndpoints = new List<IPEndPoint>
            {
                healthyEndpoints[0], healthyEndpoints[1], healthyEndpoints[2],
                healthyEndpoints[0], healthyEndpoints[1], healthyEndpoints[2]
            };

            // Act
            var actualEndpoints = new List<IPEndPoint?>();
            for (var i = 0; i < 6; i++)
            {
                actualEndpoints.Add(service.GetNextServer(state));
            }

            // Assert
            Assert.Equal(expectedEndpoints.Count, actualEndpoints.Count);
            for (var i = 0; i < expectedEndpoints.Count; i++)
            {
                Assert.Equal(expectedEndpoints[i], actualEndpoints[i]);
            }
        }

        [Fact]
        public void GetNextServer_SkipsUnhealthyServers()
        {
            // Arrange
            var service = new ServerSelectorService();
            var state = CreateTestServerState(4, allHealthy: false);
            var healthyEndpoints = state.ServerStateDictionary
                .Where(kvp => kvp.Value.IsHealthy)
                .Select(kvp => kvp.Key)
                .ToList();

            Assert.Equal(2, healthyEndpoints.Count);

            var expectedEndpoints = new List<IPEndPoint>
            {
                healthyEndpoints[0], healthyEndpoints[1],
                healthyEndpoints[0], healthyEndpoints[1]
            };

            // Act
            var actualEndpoints = new List<IPEndPoint?>();
            for (var i = 0; i < 4; i++)
            {
                actualEndpoints.Add(service.GetNextServer(state));
            }

            // Assert
            Assert.Equal(expectedEndpoints, actualEndpoints!);
        }


        [Fact]
        public void GetNextServer_UpdatesRequestsServedMetric()
        {
            // Arrange
            var service = new ServerSelectorService();
            var state = CreateTestServerState(1);
            var targetEndpoint = state.ServerStateDictionary.Keys.Single();

            // Act
            service.GetNextServer(state);
            service.GetNextServer(state);
            service.GetNextServer(state);

            // Assert
            state.ServerStateDictionary.TryGetValue(targetEndpoint, out var status);

            Assert.NotNull(status);
            Assert.Equal(3, status.Usage.RequestsServed);
        }

        [Fact]
        public async Task GetNextServer_MaintainsRoundRobin_UnderConcurrency()
        {
            // Arrange
            var service = new ServerSelectorService();
            var state = CreateTestServerState(3);
            const int totalRequests = 90;

            // Act
            var tasks = Enumerable.Range(0, totalRequests)
                .Select(_ => Task.Run(() => service.GetNextServer(state)))
                .ToArray();

            await Task.WhenAll(tasks);

            // Assert
            var totalRequestsServed = state.ServerStateDictionary.Values.Sum(s => s.Usage.RequestsServed);
            Assert.Equal(totalRequests, totalRequestsServed);

            foreach (var status in state.ServerStateDictionary.Values)
            {
                Assert.Equal(totalRequests / 3, status.Usage.RequestsServed);
            }
        }

    }
}
