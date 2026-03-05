namespace PaymentExecutionService.Middleware.ActionCircuitBreakers;

[AttributeUsage(AttributeTargets.Method)]
public class UseCircuitBreakerAttribute : Attribute
{ }
