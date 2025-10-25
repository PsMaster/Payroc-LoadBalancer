using OpenTelemetry.Logs;

namespace Payroc.Server.Configuration
{
    static class OpenTelemetryConfiguration
    {
        public static void AddOpenTelemetry(this WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddOpenTelemetry(log =>
            {
                log.IncludeFormattedMessage = true;
                log.IncludeScopes = true;
                log.AddOtlpExporter();
            });
        }
    }
}
