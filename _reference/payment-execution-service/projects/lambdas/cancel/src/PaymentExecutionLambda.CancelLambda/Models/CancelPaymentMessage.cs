namespace PaymentExecutionLambda.CancelLambda.Models;

public record CancelPaymentMessage(Guid TenantId, Guid CorrelationId, CancelPaymentRequest CancelPaymentRequest);
