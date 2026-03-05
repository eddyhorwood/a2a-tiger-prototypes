using FluentResults;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;

namespace PaymentExecutionLambda.CancelLambda.Util;

/// <summary>
/// Represents the decision on how to handle a message after processing
/// </summary>
public enum ErrorHandlingDecision
{
    Success,
    DeleteNonRetryableMessage,
    RetryMessage
}

/// <summary>
/// Centralized error handling for Lambda message processing
/// </summary>
public static class LambdaErrorHandler
{
    /// <summary>
    /// Classifies the result and determines how the message should be handled
    /// </summary>
    public static ErrorHandlingDecision ClassifyError(Result result)
    {
        // Success case
        if (result.IsSuccess)
        {
            return ErrorHandlingDecision.Success;
        }

        var paymentError = result.Errors.FirstOrDefault() as PaymentExecutionError;

        if (paymentError == null)
        {
            return ErrorHandlingDecision.DeleteNonRetryableMessage;
        }

        var errorType = paymentError.GetErrorType();

        return IsTransientError(errorType) ?
            ErrorHandlingDecision.RetryMessage :
            ErrorHandlingDecision.DeleteNonRetryableMessage;
    }

    private static bool IsTransientError(ErrorType? errorType)
    {
        return errorType == ErrorType.DependencyTransientError;
    }

    public static void LogErrorDecision(
        ILogger logger,
        Result result,
        ErrorHandlingDecision decision,
        string messageId)
    {
        switch (decision)
        {
            case ErrorHandlingDecision.Success:
                logger.LogDebug(
                    "Message {MessageId} processed successfully and will be deleted from queue",
                    messageId);
                break;

            case ErrorHandlingDecision.DeleteNonRetryableMessage:
                LogDeleteMessage(logger, result, messageId);
                break;

            case ErrorHandlingDecision.RetryMessage:
                LogRetryMessage(logger, result, messageId);
                break;
        }
    }

    private static void LogDeleteMessage(ILogger logger, Result result, string messageId)
    {
        if (result.Errors.FirstOrDefault() is PaymentExecutionError paymentError)
        {
            LogPaymentExecutionError(
                logger,
                LogLevel.Warning,
                messageId,
                paymentError,
                "will be deleted from queue as it is not retryable");
        }
        else
        {
            logger.LogWarning(
                "Message {MessageId} will be deleted from queue as it is not retryable. ErrorMessage: {ErrorMessage}",
                messageId,
                string.Join(", ", result.Errors.Select(e => e.Message)));
        }
    }

    private static void LogRetryMessage(ILogger logger, Result result, string messageId)
    {
        if (result.Errors.FirstOrDefault() is PaymentExecutionError paymentError)
        {
            LogPaymentExecutionError(
                logger,
                LogLevel.Error,
                messageId,
                paymentError,
                "will be retried and sent to DLQ if max retries exceeded");
        }
        else
        {
            logger.LogError(
                "Message {MessageId} will be retried and sent to DLQ if max retries exceeded. ErrorMessage: {ErrorMessage}",
                messageId,
                string.Join(", ", result.Errors.Select(e => e.Message)));
        }
    }

    private static void LogPaymentExecutionError(
        ILogger logger,
        LogLevel logLevel,
        string messageId,
        PaymentExecutionError paymentError,
        string actionMessage)
    {
        var errorType = paymentError.GetErrorType();
        var errorCode = paymentError.GetErrorCode();
        var httpStatusCode = paymentError.GetHttpStatusCode();
        var errorMessage = paymentError.Message;

        logger.Log(
            logLevel,
            "Message {MessageId} {ActionMessage}. ErrorType: {ErrorType}, ErrorCode: {ErrorCode}, HttpStatusCode: {HttpStatusCode}, ErrorMessage: {ErrorMessage}",
            messageId,
            actionMessage,
            errorType?.ToString() ?? "Unknown",
            errorCode ?? "N/A",
            httpStatusCode?.ToString() ?? "N/A",
            errorMessage);
    }
}
