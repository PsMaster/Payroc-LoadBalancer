namespace Payroc.LoadBalancer.Core.DependencyInjection.Options
{
    public class LoadBalancerServerOptions
    {
        public string? IpAddress { get; set; }
        public int Port { get; set; } = 5050;
        public int ServerDiscoveryDelayInSecond { get; set; } = 10;
        public int HealthCheckFrequencyInSeconds { get; set; } = 10;
        public int OldServerRemovalAgeInSeconds { get; set; } = 240;
    }
}
