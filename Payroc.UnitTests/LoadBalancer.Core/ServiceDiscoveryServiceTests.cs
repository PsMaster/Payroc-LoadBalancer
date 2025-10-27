using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NSubstitute;
using Payroc.LoadBalancer.Core.DependencyInjection.Options;
using Payroc.LoadBalancer.Core.Models;
using Payroc.LoadBalancer.Core.Services;
using System.Collections.Concurrent;
using System.Net;

namespace Payroc.UnitTests.LoadBalancer.Core
{
    public class ServiceDiscoveryServiceTests
    {
        private readonly ServerDiscoveryService _service;
        private readonly IHealthEndpoint _mockConsulHealthEndpoint;
        private readonly LoadBalancerServerOptions _serverOptions;

        public ServiceDiscoveryServiceTests()
        {
            var mockConsulClient = Substitute.For<IConsulClient>();
            _mockConsulHealthEndpoint = Substitute.For<IHealthEndpoint>();
            mockConsulClient.Health.Returns(_mockConsulHealthEndpoint);

            var mockLogger = Substitute.For<ILogger<ServerDiscoveryService>>();
            var mockHealthChecker = Substitute.For<IBackendServiceHealthChecker>();

            _serverOptions = new LoadBalancerServerOptions { OldServerRemovalAgeInSeconds = 10 };
            var mockServerOptions = Options.Create(_serverOptions);

            _service = new ServerDiscoveryService(
                mockConsulClient,
                mockLogger,
                mockHealthChecker,
                mockServerOptions
            );
        }

        [Fact]
        public async Task UpdateServers_WhenConsulCallFails_ReturnsEmpty()
        {
            // Arrange
            var currentState = new ServerState(new ConcurrentDictionary<IPEndPoint, ServerStatus>());
            var queryResult = new QueryResult<ServiceEntry[]>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = []
            };
            _mockConsulHealthEndpoint.Service(
                    "TestService",
                    Arg.Any<string>(),
                    Arg.Any<bool>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(queryResult));

            // Act
            await _service.UpdateServers("TestService", currentState);

            // Assert
            Assert.Empty(currentState.ServerStateDictionary);
        }

        [Fact]
        public async Task UpdateServers_WhenNewHealthyServersFound_AddsToServerState()
        {
            // Arrange
            var serverType = "TestService";
            var currentState = new ServerState(new ConcurrentDictionary<IPEndPoint, ServerStatus>());
            var endpoint1 = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 8080);
            var endpoint2 = new IPEndPoint(IPAddress.Parse("10.0.0.2"), 8080);

            var consulResponse = new ServiceEntry[]
            {
                new() { Service = new AgentService { Address = endpoint1.Address.ToString(), Port = endpoint1.Port } },
                new() { Service = new AgentService { Address = endpoint2.Address.ToString(), Port = endpoint2.Port } }
            };

            var queryResult = new QueryResult<ServiceEntry[]>
                { StatusCode = HttpStatusCode.OK, Response = consulResponse };
            _mockConsulHealthEndpoint.Service(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(queryResult));

            // Act
            await _service.UpdateServers(serverType, currentState);

            // Assert
            Assert.Equal(2, currentState.ServerStateDictionary.Count);
            Assert.True(currentState.ServerStateDictionary.ContainsKey(endpoint1));
        }

        [Fact]
        public async Task UpdateServers_WhenExistingServersFound_UpdatesHealth()
        {
            // Arrange
            var endpoint = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 8080);
            var initialUsage = new ServerUsageMetrics(5, 100);
            var initialStatus = new ServerStatus(false, DateTime.UtcNow.AddMinutes(-10), initialUsage);

            var currentState = new ServerState(new ConcurrentDictionary<IPEndPoint, ServerStatus>());
            currentState.ServerStateDictionary.TryAdd(endpoint, initialStatus);

            var consulResponse = new[]
            {
                new ServiceEntry
                    { Service = new AgentService { Address = endpoint.Address.ToString(), Port = endpoint.Port } }
            };

            var queryResult = new QueryResult<ServiceEntry[]>
                { StatusCode = HttpStatusCode.OK, Response = consulResponse };
            _mockConsulHealthEndpoint.Service(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(queryResult));

            // Act
            await _service.UpdateServers("TestService", currentState);

            // Assert
            var updatedStatus = currentState.ServerStateDictionary[endpoint];
            Assert.True(updatedStatus.IsHealthy, "Existing server should be marked healthy.");
            Assert.Equal(initialUsage, updatedStatus.Usage);
        }

        [Fact]
        public async Task UpdateServers_WhenServerMissingFromConsul_MarksServerAsUnhealthy()
        {
            // Arrange
            var existingEndpoint = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 8080);
            var initialTimestamp = DateTime.UtcNow.AddMinutes(-1);
            var initialStatus = new ServerStatus(true, initialTimestamp, new ServerUsageMetrics(0, 0));

            var currentState = new ServerState(new ConcurrentDictionary<IPEndPoint, ServerStatus>());
            currentState.ServerStateDictionary.TryAdd(existingEndpoint, initialStatus);

            var queryResult = new QueryResult<ServiceEntry[]>
                { StatusCode = HttpStatusCode.OK, Response = [] };
            _mockConsulHealthEndpoint.Service(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(queryResult));

            // Act
            await _service.UpdateServers("TestService", currentState);

            // Assert
            var updatedStatus = currentState.ServerStateDictionary[existingEndpoint];
            Assert.False(updatedStatus.IsHealthy, "Missing server should be marked unhealthy.");
            Assert.Equal(initialTimestamp, updatedStatus.LastHealthyTimestamp);
        }

        [Fact]
        public async Task UpdateServers_WhenOldUnhealthyServerExists_RemovesTheServer()
        {
            // Arrange
            _serverOptions.OldServerRemovalAgeInSeconds = 10;
            var oldEndpoint = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 8080);

            var oldUnhealthyStatus = new ServerStatus(
                false,
                DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(_serverOptions.OldServerRemovalAgeInSeconds + 10)),
                new ServerUsageMetrics(0, 0));

            var currentState = new ServerState(new ConcurrentDictionary<IPEndPoint, ServerStatus>());
            currentState.ServerStateDictionary.TryAdd(oldEndpoint, oldUnhealthyStatus);

            var queryResult = new QueryResult<ServiceEntry[]>
                { StatusCode = HttpStatusCode.OK, Response = Array.Empty<ServiceEntry>() };
            _mockConsulHealthEndpoint.Service(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(queryResult));

            // Act
            await _service.UpdateServers("TestService", currentState);

            // Assert
            Assert.Empty(currentState.ServerStateDictionary);
        }
    }
}
