using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Payroc.Server.Services
{
    public interface IHealthStatusService
    {
        HealthStatus GetStatus();
        void SetStatus(HealthStatus newStatus);
    }

    public class HealthStatusService : IHealthStatusService
    {
        private HealthStatus _currentStatus = HealthStatus.Healthy;

        public HealthStatus GetStatus() => _currentStatus;

        public void SetStatus(HealthStatus newStatus)
        {
            _currentStatus = newStatus;
        }
    }
}
