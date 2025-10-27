namespace Payroc.LoadBalancer.Core.DependencyInjection.Options
{
    public class ConsulConfig
    {
        public string? ConsulAddress { get; set; }
        public string? ServiceName { get; set; }
        public string? HealthCheckUrl { get; set; }
    }
}
