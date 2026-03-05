// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using PaymentExecution.Common;

namespace Xero.Accelerators.Api.Core.Conventions.ErrorHandling;

public class GlobalExceptionHandler(ProblemDetailsFactory problemDetailsFactory) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = StatusCodes.Status500InternalServerError;

        if (exception is BadHttpRequestException badRequestException)
        {
            statusCode = badRequestException.StatusCode;
        }
        var errorCode = ErrorConstants.ErrorCode.ExecutionUnexpectedError;
        var requestPath = context.Request.Path;
        if (requestPath.Equals($"/{PaymentExecutionService.Constants.RouteConstants.SubmitStripePayment}", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ErrorConstants.ErrorCode.ExecutionSubmitError;
        }
        var baseProblemDetails = problemDetailsFactory.CreateProblemDetails(context, statusCode);
        var problemDetails = new ProblemDetailsExtended()
        {
            Status = baseProblemDetails.Status,
            Title = baseProblemDetails.Title,
            Type = baseProblemDetails.Type,
            ErrorCode = errorCode,
            ProviderErrorCode = null, // Set this if you have a specific provider error code
            Detail = baseProblemDetails.Detail,
            Extensions = new Dictionary<string, object?>(baseProblemDetails.Extensions)
        };

        await context.WriteProblemDetailsAsync(problemDetails);

        return true;
    }
}
