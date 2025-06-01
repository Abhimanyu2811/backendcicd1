using Microsoft.ApplicationInsights.DataContracts;

namespace Backendapi.Services;

public interface ITelemetryService
{
    void TrackEvent(string eventName, IDictionary<string, string>? properties = null);
    void TrackException(Exception exception, IDictionary<string, string>? properties = null);
    void TrackTrace(string message, SeverityLevel severityLevel = SeverityLevel.Information);
    void TrackMetric(string name, double value, IDictionary<string, string>? properties = null);
} 