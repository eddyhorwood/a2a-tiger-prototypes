using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models.Errors;
using PaymentExecutionService.Middleware.ActionCircuitBreakers;
using PaymentExecutionService.Models;
using Xero.Accelerators.Api.Core.Observability.Correlation;

namespace PaymentExecutionService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/payments/[controller]")]
public class DchDeleteController(ILogger<DchDeleteController> logger, IMediator mediator) : BaseController
{
    [HttpPost(Name = "DCHDelete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [UseCircuitBreaker]
    [Authorize(Constants.ServiceAuthorizationPolicies.DchDelete)]
    [AllowNoXeroCorrelationId]
    public async Task<IActionResult> DchDelete([FromBody] DchDeletePayload payload)
    {
        logger.LogInformation("Received DCHDelete request");
        var command = new DeleteByOrgCommand { OrganisationId = payload.IdToDelete };
        var result = await mediator.Send(command);

        if (result.HasError<PaymentTransactionNotFoundError>())
        {
            return NotFound();
        }

        if (result.IsFailed)
        {
            // If an unrecognised failure is returned; return a 400. This triggers a retry in DCHs system.
            return GenerateUnknown400ErrorResponse(result.Errors);
        }

        return Ok();
    }
}
