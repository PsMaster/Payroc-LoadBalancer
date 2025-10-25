using Payroc.LoadBalancer.Endpoints.Requests;

namespace Payroc.LoadBalancer.Endpoints
{
    public static class ApiEndpoints
    {
        public static void MapApiEndpoints(this WebApplication app)
        {
            app.MapGet("/health", (HttpRequest _) => Results.Ok());
            app.MapPost("/control/balancing", async (ChangeBalancingModeRequest request) => {});
        }
    }
}
