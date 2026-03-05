namespace PaymentExecution.PaymentRequestClient.Exception;

public class PaymentRequestException : System.Exception
{
    public PaymentRequestException(string message) : base(message)
    {
    }

    public PaymentRequestException(string message, System.Exception innerException) : base(message, innerException)
    {
    }
}
