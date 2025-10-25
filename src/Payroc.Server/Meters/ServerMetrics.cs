using System.Diagnostics.Metrics;

namespace Payroc.Server.Meters
{
    public class ServerMetrics
    {
        public const string ServiceName = "payroc.server";
        public const string RequestsCounterName = "payroc.requests";

        private readonly Counter<long> _requestCounter;

        public ServerMetrics(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create(RequestsCounterName);
            _requestCounter = meter.CreateCounter<long>($"{RequestsCounterName}.executions");
        }

        public void RequestsIncrement()
        {
            _requestCounter.Add(1, new KeyValuePair<string, object?>("payroc.requests.processed", Environment.MachineName));
        }
    }
}
