using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payroc.LoadBalancer.Core.DependencyInjection.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Payroc.LoadBalancer.Core.Services
{
    public sealed class LoadBalancerService : ILoadBalancerService
    {
        private readonly TcpListener _tcpListener;
        private readonly ILogger<LoadBalancerService> _logger;
        private readonly IServerDiscoveryService _serverDiscoveryService;
        private readonly LoadBalancerServerOptions _serverOptions;
        private readonly ConsulConfig _consulConfig;
        private Task? _trafficHandlingTask;

        public LoadBalancerService(ILogger<LoadBalancerService> logger, IServerDiscoveryService serverDiscoveryService,
            IOptions<ConsulConfig> consulOptions, IOptions<LoadBalancerServerOptions> serverOptions)
        {
            _logger = logger;
            _serverDiscoveryService = serverDiscoveryService;
            _serverOptions = serverOptions.Value;
            _consulConfig = consulOptions.Value;
            _tcpListener = new TcpListener(IPAddress.Parse(_serverOptions.IpAddress!), _serverOptions.Port);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Load balancer server starting");
                _tcpListener.Start();
                _logger.LogInformation("TCP listener started on port {Port}", _serverOptions.Port);
                var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken).Token;
                _trafficHandlingTask = Task.Run(async () => await ListenForTraffic(linkedToken), linkedToken);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Server exception occured.");
                throw;
            }
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
            IPEndPoint backend = new IPEndPoint(IPAddress.Any, 8080);
            try
            {
                //TODO complete
                var server = (await _serverDiscoveryService.GetServers(_consulConfig.ServiceName ?? "payrocserver", cancellationToken)).First();
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[1024];
                    int bytesRead;
                    while (!cancellationToken.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, cancellationToken)) != 0)
                    {
                        var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        _logger.LogInformation("Received from client: {Data}", receivedData.Trim());
                        var responseData = Encoding.UTF8.GetBytes($"Echo: {receivedData}");
                        await stream.WriteAsync(responseData, cancellationToken);
                    }
                }
                // Pick backend
                // connect
            }
            catch (Exception)
            {
                _logger.LogWarning("Backend connection failed. Server:{Backend} Time:{Timestamp}", backend, DateTime.UtcNow);
            }
            finally
            {
                client.Close();
            }
        }

    }
}
