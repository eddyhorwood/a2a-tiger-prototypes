namespace PaymentExecutionService.Middleware.ActionCircuitBreakers;

public class EndpointCircuitBreakerPipelineOptions
{
    public int? SamplingDurationSeconds { get; set; }
    public float? FailureRatio { get; set; }
    public int? MinimumThroughput { get; set; }
    public double? BreakDurationSeconds { get; set; }
}
