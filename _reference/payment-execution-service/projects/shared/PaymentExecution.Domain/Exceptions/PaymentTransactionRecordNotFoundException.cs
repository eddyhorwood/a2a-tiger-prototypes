namespace PaymentExecution.Domain.Exceptions;

public class PaymentTransactionRecordNotFoundException : Exception
{
    public PaymentTransactionRecordNotFoundException(string message) : base(message)
    {
    }
}
