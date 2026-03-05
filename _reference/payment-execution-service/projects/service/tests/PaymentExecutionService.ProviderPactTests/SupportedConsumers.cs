namespace PaymentExecutionService.ProviderPactTests;

public static class SupportedConsumers
{
    /// <summary>
    /// When new consumers are registered against the provider and accepted as
    /// being a target for pact contracts, they should be added here.
    /// </summary>
    public static List<string> GetPactConsumerNames()
    {
        return
        [
            "StripeExecutionWebhookWorker"
        ];
    }
}
