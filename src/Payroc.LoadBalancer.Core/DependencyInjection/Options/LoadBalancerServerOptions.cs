namespace Payroc.LoadBalancer.Core.DependencyInjection.Options
{
    public class LoadBalancerServerOptions
    {
        public string? IpAddress { get; set; }
        public int Port { get; set; } = 5050;
    }
}
