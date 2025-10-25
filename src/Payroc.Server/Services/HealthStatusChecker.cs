using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Payroc.Server.Services
{
    public class HealthStatusChecker : IHealthCheck
    {
        private readonly IHealthStatusService _healthStatusService;

        public HealthStatusChecker(IHealthStatusService healthStatusService)
        {
            _healthStatusService = healthStatusService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var status = _healthStatusService.GetStatus();

            if (status != HealthStatus.Healthy)
            {
                return await Task.FromResult(new HealthCheckResult(status));
            }

            return await Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}
