using Consul;
using System.Net;
using System.Net.Sockets;

namespace Payroc.Server.Services
{
    public class ServerRegistrationService : IHostedService
    {
        private readonly ILogger<ServerRegistrationService> _logger;
        private readonly IConsulClient _consulClient;
        private readonly IConfiguration _configuration;
        private readonly string _registrationId;

        public ServerRegistrationService(ILogger<ServerRegistrationService> logger, IConsulClient consulClient,
        IConfiguration configuration)
        {
            _consulClient = consulClient;
            _configuration = configuration;
            _logger = logger;
            var serviceName = _configuration["ConsulConfig:ServiceName"];
            var containerId = Environment.GetEnvironmentVariable("HOSTNAME") ?? Guid.NewGuid().ToString();
            _registrationId = $"{serviceName}-{containerId}";
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var servicePort = _configuration.GetValue<int>("ConsulConfig:ServicePort");
            var serviceName = _configuration["ConsulConfig:ServiceName"];
            var healthCheckUrl = _configuration["ConsulConfig:HealthCheckUrl"];

            var httpCheck = new AgentServiceCheck()
            {
                HTTP = healthCheckUrl,
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5)
            };

            string hostName = Dns.GetHostName();
            IPAddress? containerIp = (await Dns.GetHostEntryAsync(hostName, cancellationToken)).AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (containerIp == null)
            {
                _logger.LogCritical("Unable to retrieve service internal IP.");
                return;
            }

            var registration = new AgentServiceRegistration()
            {
                ID = _registrationId,
                Name = serviceName,
                Address = containerIp.ToString(),
                Port = servicePort,
                Tags = ["payroc-server-v1"],
                Checks = [httpCheck]
            };

            _logger.LogInformation($"Registering service '{registration.ID}' with Consul");
            await _consulClient.Agent.ServiceRegister(registration, cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Removing service '{_registrationId}' from Consul");
            await _consulClient.Agent.ServiceDeregister(_registrationId, cancellationToken);
        }
    }
}
