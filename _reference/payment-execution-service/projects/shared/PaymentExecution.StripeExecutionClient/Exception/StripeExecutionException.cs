namespace PaymentExecution.StripeExecutionClient.Exception;

public class StripeExecutionException : System.Exception
{
    public StripeExecutionException(string message) : base(message)
    {
    }

    public StripeExecutionException(string message, System.Exception innerException) : base(message, innerException)
    {
    }
}
