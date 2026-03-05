using FluentResults;

namespace PaymentExecution.Common;

public static class ResultExtension
{
    // Get the first error message for a specific error type
    public static string GetFirstErrorMessage(this IResultBase result)
    {
        return result.Errors
            .FirstOrDefault()?.Message ?? string.Empty;
    }
}
