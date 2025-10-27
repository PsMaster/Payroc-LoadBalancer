using System.Collections.Concurrent;
using System.Net;

namespace Payroc.LoadBalancer.Core.Models
{
    public sealed record ServerUsageMetrics(int ServerLoad, long RequestsServed);
    public sealed record ServerStatus(bool IsHealthy, DateTime LastHealthyTimestamp, ServerUsageMetrics Usage);
    public sealed record ServerState(ConcurrentDictionary<IPEndPoint, ServerStatus> ServerStateDictionary);
}
