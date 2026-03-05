using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using PaymentExecutionService.Extensions;
using PaymentExecutionService.Models;

namespace PaymentExecutionService.Filters;

public class WhitelistAuthorizationFilter(IOptions<WhitelistOptions> whitelistOpts, ILogger<WhitelistAuthorizationFilter> logger) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (whitelistOpts.Value == null)
        {
            logger.LogError("Whitelist has not been configured correctly");
            throw new NoNullAllowedException("Whitelist has not been correctly configured.");
        }

        var httpContext = context.HttpContext;

        var clientId = httpContext.GetClientIdFromJwtToken();
        var permittedClientIds = whitelistOpts.Value.ClientIds;
        var isClientIdWhitelisted = permittedClientIds.Contains(clientId);

        if (string.IsNullOrEmpty(clientId) || !isClientIdWhitelisted)
        {
            logger.LogWarning("Client ID '{ClientId}' attempting to access the service is not whitelisted.", clientId);
            var formattedObjectResult = CreateObjectResultWithProblemDetails();
            context.Result = formattedObjectResult;
        }
    }

    private static ObjectResult CreateObjectResultWithProblemDetails()
    {
        var statusCode = StatusCodes.Status403Forbidden;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = "Forbidden",
            Detail = "Calling client is not permitted to access this resource.",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3"
        };
        var objectResult = new ObjectResult(problemDetails) { StatusCode = statusCode };
        return objectResult;
    }
}

