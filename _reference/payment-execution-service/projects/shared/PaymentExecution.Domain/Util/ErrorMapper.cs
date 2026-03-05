using System.Net;
using FluentResults;
using PaymentExecution.Common;

namespace PaymentExecution.Domain.Util;

public static class ErrorMapper
{
    public static PaymentExecutionError MapToPaymentFailedError(Result result)
    {
        if (!result.HasError<PaymentExecutionError>())
        {
            throw new NotImplementedException("Error should be of uniform type");
        }

        var error = result.Reasons[0];
        var castedError = (PaymentExecutionError)error;

        castedError.SetErrorCode(ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed);
        castedError.SetErrorType(ErrorType.PaymentFailed);

        return castedError;
    }

    public static PaymentExecutionError MapToBadPaymentRequestError(Result result)
    {
        if (!result.HasError<PaymentExecutionError>())
        {
            throw new NotImplementedException("Error should be of uniform type");
        }

        var error = result.Reasons[0];
        var castedError = (PaymentExecutionError)error;

        castedError.SetErrorCode(ErrorConstants.ErrorCode.ExecutionSubmitPaymentFailed);
        castedError.SetErrorType(ErrorType.BadPaymentRequest);

        return castedError;
    }

    public static Result MapToGetProviderError(Result result)
    {
        if (!result.HasError<PaymentExecutionError>())
        {
            return result;
        }

        return ClassifyProviderErrorWithCode(result, ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
    }

    public static Result MapToGetProviderErrorForLambda(Result result)
    {
        if (!result.HasError<PaymentExecutionError>())
        {
            return Result.Fail(new PaymentExecutionError(
                "Failed to get provider state",
                ErrorType.FailedDependency,
                ErrorConstants.ErrorCode.ExecutionGetProviderStateError));
        }

        return ClassifyProviderErrorWithCode(result, ErrorConstants.ErrorCode.ExecutionGetProviderStateError);
    }

    public static Result MapToCancelPaymentErrorForLambda(Result result)
    {
        if (!result.HasError<PaymentExecutionError>())
        {
            var originalMessage = result.Errors.FirstOrDefault()?.Message ?? "Unknown error";
            return Result.Fail(new PaymentExecutionError(
                $"Failed to cancel payment with provider: {originalMessage}",
                ErrorType.FailedDependency,
                ErrorConstants.ErrorCode.ExecutionCancellationError));
        }

        return ClassifyProviderErrorWithCode(result, ErrorConstants.ErrorCode.ExecutionCancellationError);
    }

    private static Result ClassifyProviderErrorWithCode(Result result, string errorCode)
    {
        var error = result.Reasons[0];
        var castedError = (PaymentExecutionError)error;

        castedError.SetErrorCode(errorCode);

        var responseStatusCode = castedError.GetHttpStatusCode();
        if (responseStatusCode != null &&
            (int)responseStatusCode != (int)HttpStatusCode.TooManyRequests &&
            (int)responseStatusCode >= 400 && (int)responseStatusCode < 500)
        {
            castedError.SetErrorType(ErrorType.FailedDependency);
            return Result.Fail(castedError);
        }

        castedError.SetErrorType(ErrorType.DependencyTransientError);
        return Result.Fail(castedError);
    }
}
