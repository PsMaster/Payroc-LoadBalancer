using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payroc.LoadBalancer.Core.DependencyInjection.Options;
using Payroc.LoadBalancer.Core.Models;
using System.Net;
using System.Net.Sockets;

namespace Payroc.LoadBalancer.Core.Services
{
    public sealed class LoadBalancerService : ILoadBalancerService
    {
        private readonly TcpListener _tcpListener;
        private readonly ILogger<LoadBalancerService> _logger;
        private readonly IServerDiscoveryService _serverDiscoveryService;
        private readonly IServerSelectorService _serverSelectorService;
        private readonly LoadBalancerServerOptions _serverOptions;
        private readonly ServerState _currentServers;
        private readonly ConsulConfig _consulConfig;

        public LoadBalancerService(ILogger<LoadBalancerService> logger, IServerDiscoveryService serverDiscoveryService, IServerSelectorService serverSelectorService,
            IOptions<ConsulConfig> consulOptions, IOptions<LoadBalancerServerOptions> serverOptions)
        {
            _logger = logger;
            _serverDiscoveryService = serverDiscoveryService;
            _serverSelectorService = serverSelectorService;
            _serverOptions = serverOptions.Value;
            _consulConfig = consulOptions.Value;
            _tcpListener = new TcpListener(IPAddress.Parse(_serverOptions.IpAddress!), _serverOptions.Port);
            _currentServers = new ServerState([]);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Load balancer server starting");
                _tcpListener.Start();
                _logger.LogInformation("TCP listener started on port {Port}", _serverOptions.Port);
                var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken).Token;
                _ = Task.Run(async () => await ListenForTraffic(linkedToken), linkedToken);
                _ = Task.Run(async () => await DiscoverServers(linkedToken), linkedToken);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Server exception occured.");
                throw;
            }

            return Task.CompletedTask;
        }

        private async Task ListenForTraffic(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var acceptedTask = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
                    _ = HandleClientRequestAsync(acceptedTask, cancellationToken);

                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Tcp listening stopped: {Timestamp}", DateTime.UtcNow);
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error accepting client connection.");
                    continue;
                }

                _logger.LogTrace("Handling request: {Timestamp}", DateTime.UtcNow);
            }
        }

        private async Task HandleClientRequestAsync(TcpClient client, CancellationToken cancellationToken)
        {
            IPEndPoint? backendEndpoint = null; 
            try
            {
                backendEndpoint = _serverSelectorService.GetNextServer(_currentServers);
                if (backendEndpoint == null)
                {
                    _logger.LogCritical("Failed to retrieve next server. Time:{Timestamp}", DateTime.UtcNow);
                    return;
                }

                using var backendClient = new TcpClient();
                await backendClient.ConnectAsync(backendEndpoint.Address, backendEndpoint.Port, cancellationToken);
                await using var clientStream = client.GetStream();
                await using var backendStream = backendClient.GetStream();
                var clientStreamTask = clientStream.CopyToAsync(backendStream, 81920, cancellationToken);
                var backendStreamTask = backendStream.CopyToAsync(clientStream, 81920, cancellationToken);
                _logger.LogTrace("Proxy {Client} <-> {Backend}", client.Client.RemoteEndPoint, backendEndpoint);
                await Task.WhenAny(clientStreamTask, backendStreamTask);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Was not able to connect to backend server.");
                _logger.LogWarning("Backend connection failed. Server:{Backend} Time:{Timestamp}", backendEndpoint, DateTime.UtcNow);
            }
            finally
            {
                client.Close();
            }
        }

        private async Task DiscoverServers(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _serverDiscoveryService.UpdateServers(_consulConfig.ServiceName!, _currentServers, cancellationToken);
                }
                catch (Exception)
                {
                    _logger.LogWarning("Server discovery failed. Time:{Timestamp}", DateTime.UtcNow);
                }

                await Task.Delay(TimeSpan.FromSeconds(_serverOptions.ServerDiscoveryDelayInSecond), cancellationToken);
            }
        }

    }
}
