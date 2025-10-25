using Microsoft.Extensions.Diagnostics.HealthChecks;
using Payroc.Server.Endpoints.Requests;
using Payroc.Server.Meters;
using Payroc.Server.Services;

namespace Payroc.Server.Endpoints
{
    static class ConfigureServerEndpoints
    {
        public static void MapEndpoints(this WebApplication app)
        {
            app.MapGet("/", (ServerMetrics metrics) =>
            {
                metrics.RequestsIncrement();
                return "Doing work";
            });

            // TODO Consider securing with auth in a realistic application
            app.MapPost("/health", (HealthUpdateRequest request, IHealthStatusService statusService) =>
            {
                if (!Enum.TryParse<HealthStatus>(request.Status, true, out var newStatus))
                    return Results.BadRequest("Invalid Health Status. Must be 'Healthy', 'Degraded', or 'Unhealthy'.");

                statusService.SetStatus(newStatus);
                return Results.Ok($"Health status set to {newStatus}");
            });
        }
    }
}
