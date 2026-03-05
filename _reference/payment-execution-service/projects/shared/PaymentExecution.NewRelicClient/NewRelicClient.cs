using System.Diagnostics.CodeAnalysis;
using NewRelic.Api.Agent;
using Polly.CircuitBreaker;

namespace PaymentExecution.NewRelicClient;

public interface IMonitoringClient
{
    void AcceptDistributedTraceHeaders<T>(
        T carrier,
        Func<T, string, IEnumerable<string>> getter,
        TransportType transportType);
    void InsertDistributedTraceHeaders<T>(T carrier, Action<T, string, string> setter);

    void NotifyNewRelicOfCircuitEvent(CircuitState circuitState, string endpoint,
        TimeSpan? timeSpan = null);
}

[ExcludeFromCodeCoverage(Justification = "The New Relic Agent API does not expose a nicer testable interface, so this project serves as a shim around the simple methods we wish to invoke")]
public class NewRelicClient : IMonitoringClient
{
    public void AcceptDistributedTraceHeaders<T>(T carrier, Func<T, string, IEnumerable<string>> getter, TransportType transportType)
    {
        NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AcceptDistributedTraceHeaders(carrier, getter, transportType);
    }
    public void InsertDistributedTraceHeaders<T>(T carrier, Action<T, string, string> setter)
    {
        NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.InsertDistributedTraceHeaders(carrier, setter);
    }

    public void NotifyNewRelicOfCircuitEvent(CircuitState circuitState, string endpoint, TimeSpan? timeSpan = null)
    {
        // Record metric count
        NewRelic.Api.Agent.NewRelic.RecordMetric($"Custom/CircuitBreaker/{circuitState.ToString()}/Activations", 1);
        // Record as a custom event with details, for Alert support and showing on Dashboards
        NewRelic.Api.Agent.NewRelic.RecordCustomEvent("CircuitBreakerEvent", new Dictionary<string, object>
        {
            { "State", circuitState.ToString() },
            { "Duration", timeSpan?.TotalSeconds ?? -1 },
            { "Endpoint", endpoint }
        });
    }
}
