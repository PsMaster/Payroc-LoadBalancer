using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using Payroc.LoadBalancer.Core.Models;

namespace Payroc.LoadBalancer.Core.Services
{
    public sealed class LoadBalancerService : ILoadBalancerService
    {
        private readonly TcpListener _tcpListener = new TcpListener(new IPAddress([127, 0, 0, 0]), 9000);
        private readonly Channel<ControlCommand> _controlChannel;
        private readonly ILogger<LoadBalancerService> _logger;

        public LoadBalancerService(Channel<ControlCommand> channel, ILogger<LoadBalancerService> logger)
        {
            _controlChannel = channel;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //_tcpListener.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //_tcpListener.Stop();
            return Task.CompletedTask;
        }
    }
}
