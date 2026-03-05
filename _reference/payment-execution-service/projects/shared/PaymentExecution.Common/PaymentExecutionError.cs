using System.Net;
using FluentResults;

namespace PaymentExecution.Common;
/// <summary>
/// This extends the IError interface to provide a structured way to represent errors via error type
/// </summary>
public class PaymentExecutionError : IError
{
    public string Message { get; }
    public Dictionary<string, object> Metadata { get; }
    public List<IError> Reasons { get; } = new List<IError>();

    public PaymentExecutionError(string message, HttpStatusCode httpStatusCode)
    {
        Message = message;
        Metadata = new Dictionary<string, object>
        {
            { ErrorMetadataKey.HttpStatusCode, httpStatusCode }
        };
    }

    public PaymentExecutionError(string message, string? providerErrorCode = null, HttpStatusCode? httpStatusCode = null)
    {
        Message = message;
        Metadata = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(providerErrorCode))
        {
            Metadata.Add(ErrorMetadataKey.ProviderErrorCode, providerErrorCode);
        }

        if (httpStatusCode != null)
        {
            Metadata.Add(ErrorMetadataKey.HttpStatusCode, httpStatusCode);
        }
    }

    public PaymentExecutionError(string message, ErrorType errorType, string errorCode, string? providerErrorCode = null)
    {
        Message = message;
        Metadata = new Dictionary<string, object>
        {
            { "ErrorType", errorType },
            { "ErrorCode", errorCode },
        };

        if (!string.IsNullOrWhiteSpace(providerErrorCode))
        {
            Metadata.Add("ProviderErrorCode", providerErrorCode);
        }
    }

    public string? GetErrorCode()
    {
        return Metadata.TryGetValue(ErrorMetadataKey.ErrorCode, out var errorCode) ? errorCode.ToString() : string.Empty;
    }

    public string? GetProviderErrorCode()
    {
        return Metadata.TryGetValue(ErrorMetadataKey.ProviderErrorCode, out var providerErrorCode) ? providerErrorCode.ToString() : null;
    }

    public ErrorType? GetErrorType()
    {
        return Metadata.TryGetValue(ErrorMetadataKey.ErrorType, out var errorType) ?
            (ErrorType)errorType : null;
    }

    public HttpStatusCode? GetHttpStatusCode()
    {
        return Metadata.TryGetValue(ErrorMetadataKey.HttpStatusCode, out var statusCode) ?
            (HttpStatusCode)statusCode : null;
    }

    public void SetErrorType(ErrorType errorType)
    {
        Metadata[ErrorMetadataKey.ErrorType] = errorType;
    }

    public void SetErrorCode(string errorCode)
    {
        Metadata[ErrorMetadataKey.ErrorCode] = errorCode;
    }
}

public enum ErrorType
{
    ClientError, // Can Map to HTTP 400
    BadPaymentRequest,
    PaymentFailed, // Can map to HTTP 402
    PaymentTransactionNotCancellable,
    PaymentTransactionNotFound,
    ValidationError,
    FailedDependency,
    DependencyTransientError
}
