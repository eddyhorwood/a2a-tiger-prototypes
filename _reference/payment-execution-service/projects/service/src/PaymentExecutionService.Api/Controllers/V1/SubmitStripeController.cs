using Asp.Versioning;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentExecution.Domain.Commands;
using PaymentExecutionService.Middleware;
using PaymentExecutionService.Middleware.ActionCircuitBreakers;
using PaymentExecutionService.Models;

namespace PaymentExecutionService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route(Constants.RouteConstants.SubmitStripePayment)]
[Authorize(Constants.ServiceAuthorizationPolicies.Submit)]
public class SubmitStripeController(IMediator mediator, IMapper mapper) : BaseController
{
    [HttpPost(Name = "SubmitStripe")]
    [UseCircuitBreaker]
    [RequiresTenantIdHeader]
    [RequiresProviderAccountIdHeader]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SubmitStripePaymentCommandResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitPayment([FromBody] SubmitStripeRequest request)
    {
        var submitStripeCommand = mapper.Map<SubmitStripePaymentCommand>(request);
        submitStripeCommand.XeroCorrelationId = XeroCorrelationId.ToString();
        submitStripeCommand.XeroTenantId = XeroTenantId.ToString();

        var response = await mediator.Send(submitStripeCommand);

        return response.IsSuccess ? Ok(response.Value) : HandleErrors(response);
    }
}
