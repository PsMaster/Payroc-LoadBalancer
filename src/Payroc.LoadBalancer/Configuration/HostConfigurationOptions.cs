namespace Payroc.LoadBalancer.Configuration
{
    public class HostConfigurationOptions
    {
        public int ShutdownTimeoutInSeconds { get; set; }
        public int StartupTimeoutInSeconds { get; set; }
        public bool ServicesStartConcurrently { get; set; }
        public bool ServicesStopConcurrently { get; set; }
        public BackgroundServiceExceptionBehavior BackgroundServiceExceptionBehavior { get; set; }
    }
}
