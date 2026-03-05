namespace PaymentExecution.PaymentRequestClient.ConsumerPactTest;

public static class PaymentRequestServiceProviderState
{
    public const string RequestInProgress = "A request is in progress with a specific state";

    public static Dictionary<string, string> ParamDictionary(Guid requestId, string state)
    {
        return new Dictionary<string, string>
        {
            { "request-id", requestId.ToString() },
            { "request-state", state }
        };
    }
}
