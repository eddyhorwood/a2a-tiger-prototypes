namespace PaymentExecution.Repository.Models;

public class UpdateForSubmitFlowDto
{
    public required Guid PaymentTransactionId { get; set; }
    public required Guid ProviderServiceId { get; set; }

    /// <summary>
    /// In stripe domain, this is PaymentIntentId
    /// </summary>
    public required string PaymentProviderPaymentTransactionId { get; set; }
}
