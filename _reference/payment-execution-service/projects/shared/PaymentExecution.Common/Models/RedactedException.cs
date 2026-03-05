using PaymentExecution.Common.Extensions;
namespace PaymentExecution.Common.Models;

public class RedactedException : Exception
{
    public ExceptionType Type { get; }
    //The constructor that accepts a string for the error message.
    public RedactedException(string message, ExceptionType exceptionType) : base(message)
    {
        Type = exceptionType;
        Message = RedactionHelper.SanitizeMessage(message) + $" Original Exception Type: {Type}";
    }

    //The constructor that accepts a string and an inner exception.
    public RedactedException(string message, ExceptionType exceptionType, Exception inner) : base(message, inner)
    {
        Type = exceptionType;
        Message = RedactionHelper.SanitizeMessage(message) + $" Original Exception Type: {Type}";
    }

    public override string Message { get; }
}

public enum ExceptionType
{
    DatabaseException,
    PaymentRequestException,
    TransformMessageException,
}

