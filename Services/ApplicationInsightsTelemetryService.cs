using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Backendapi.Services;

public class ApplicationInsightsTelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;

    public ApplicationInsightsTelemetryService(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackEvent(eventName, properties);
    }

    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackException(exception, properties);
    }

    public void TrackTrace(string message, SeverityLevel severityLevel = SeverityLevel.Information)
    {
        _telemetryClient.TrackTrace(message, severityLevel);
    }

    public void TrackMetric(string name, double value, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackMetric(name, value, properties);
    }
} 