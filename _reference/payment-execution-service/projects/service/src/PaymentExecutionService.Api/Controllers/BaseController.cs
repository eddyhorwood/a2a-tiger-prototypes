using System.Net;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using PaymentExecution.Common;

namespace PaymentExecutionService.Controllers;

public abstract class BaseController : ControllerBase
{
    protected Guid XeroTenantId
    {
        get
        {
            var xeroTenantIdHeader = Request.Headers[Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroTenantId]
                .ToString();
            return Guid.TryParse(xeroTenantIdHeader, out var xeroTenantId) ? xeroTenantId : Guid.Empty;
        }
    }

    protected Guid XeroCorrelationId
    {
        get
        {
            var xeroCorrelationIdHeader = Request.Headers[Xero.Accelerators.Api.Core.Constants.HttpHeaders.XeroCorrelationId]
                .ToString();
            return Guid.TryParse(xeroCorrelationIdHeader, out var xeroCorrelationId) ? xeroCorrelationId : Guid.Empty;
        }
    }


    protected ActionResult GenerateUnknown400ErrorResponse(IEnumerable<IError> errors)
    {

        var problemDetails = new ProblemDetailsExtended()
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = nameof(HttpStatusCode.BadRequest),
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
            ProviderErrorCode = null,
            ErrorCode = ErrorConstants.ErrorCode.GenericExecutionError
        };

        var errorList = errors.ToList().AsEnumerable()
            .Select(error => error.Message).ToList();

        problemDetails.Extensions.Add("errors", errorList);
        return StatusCode(StatusCodes.Status400BadRequest, problemDetails);
    }

    protected ActionResult GenerateGeneric404ErrorResponse()
    {
        var problemDetails = new ProblemDetailsExtended()
        {
            Status = (int)HttpStatusCode.NotFound,
            Title = nameof(HttpStatusCode.NotFound),
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
            ProviderErrorCode = null,
            ErrorCode = ErrorConstants.ErrorCode.GenericExecutionError
        };

        return StatusCode(StatusCodes.Status404NotFound, problemDetails);
    }

    protected IActionResult HandleErrors(ResultBase result)
    {
        var error = result.Errors.FirstOrDefault();
        if (error is not PaymentExecutionError castedError)
        {
            var genericProblemDetails = new ProblemDetailsExtended()
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = nameof(HttpStatusCode.InternalServerError),
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                ErrorCode = ErrorConstants.ErrorCode.ExecutionUnexpectedError,
                Detail = "Unexpected error occurred while processing the request. Please try again later."
            };
            return StatusCode(500, genericProblemDetails);
        }

        var errorType = castedError.GetErrorType();
        if (errorType == ErrorType.PaymentFailed)
        {
            var statusCode = HttpStatusCode.PaymentRequired;
            var type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.2";
            var problemDetails = GenerateProblemDetailsFromError(castedError, statusCode, type);
            return StatusCode(StatusCodes.Status402PaymentRequired, problemDetails);
        }

        if (errorType == ErrorType.ClientError
            || errorType == ErrorType.BadPaymentRequest
            || errorType == ErrorType.PaymentTransactionNotCancellable)
        {
            var statusCode = HttpStatusCode.BadRequest;
            var type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
            var problemDetails = GenerateProblemDetailsFromError(castedError, statusCode, type);
            return BadRequest(problemDetails);
        }

        if (errorType == ErrorType.PaymentTransactionNotFound)
        {
            return GenerateGeneric404ErrorResponse();
        }

        if (errorType == ErrorType.FailedDependency)
        {
            var statusCode = HttpStatusCode.FailedDependency;
            var type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/424";
            var problemDetails = GenerateProblemDetailsFromError(castedError, statusCode, type);
            return StatusCode(StatusCodes.Status424FailedDependency, problemDetails);
        }

        if (errorType == ErrorType.DependencyTransientError)
        {
            var statusCode = HttpStatusCode.ServiceUnavailable;
            var type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4";
            var problemDetails = GenerateProblemDetailsFromError(castedError, statusCode, type);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, problemDetails);
        }

        return GenerateUnknown400ErrorResponse(result.Errors);
    }

    private static ProblemDetailsExtended GenerateProblemDetailsFromError(PaymentExecutionError error, HttpStatusCode statusCode, string type)
    {
        var problemDetails = new ProblemDetailsExtended()
        {
            Status = (int)statusCode,
            Title = statusCode.ToString(),
            Type = type,
            Detail = error.Message,
            ErrorCode = error.GetErrorCode(),
            ProviderErrorCode = error.GetProviderErrorCode()
        };

        var errorPayload = new List<string>() { error.Message };
        problemDetails.Extensions.Add("errors", errorPayload);
        return problemDetails;
    }
}
