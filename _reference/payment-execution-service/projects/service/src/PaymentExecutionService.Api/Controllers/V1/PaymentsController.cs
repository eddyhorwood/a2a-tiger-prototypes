using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using AutoMapper;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Queries;
using PaymentExecutionService.Filters;
using PaymentExecutionService.Middleware;
using PaymentExecutionService.Middleware.ActionCircuitBreakers;
using PaymentExecutionService.Models;
using PaymentExecutionService.Models.Response;

namespace PaymentExecutionService.Controllers.V1;
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public class PaymentsController(IMediator mediator, IMapper mapper) : BaseController
{
    [HttpPost("payment-requests/{paymentRequestId}/provider-executions/{providerServiceId}/complete", Name = "Complete")]
    [UseCircuitBreaker]
    [RequiresTenantIdHeader]
    [Authorize(Constants.ServiceAuthorizationPolicies.Complete)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CompletePaymentTransaction(
        Guid paymentRequestId,
        Guid providerServiceId,
        [FromBody] CompletePaymentTransactionRequest request)
    {
        var executionQueueMessage = MapRequestToExecutionQueueMessage(request, paymentRequestId, providerServiceId);

        await mediator.Send(new CompletePaymentTransactionCommand
        {
            Message = executionQueueMessage,
            XeroTenantId = XeroTenantId.ToString(),
            XeroCorrelationId = XeroCorrelationId.ToString()
        });

        return Accepted();
    }

    [HttpGet(Name = "GetPaymentTransaction")]
    [UseCircuitBreaker]
    [Authorize(Constants.ServiceAuthorizationPolicies.ReadOnly)]
    [ProducesResponseType(typeof(GetPaymentTransactionQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentTransaction(
        [FromQuery, Required] Guid paymentRequestId)
    {
        var query = new GetPaymentTransactionQuery()
        {
            PaymentRequestId = paymentRequestId
        };
        var result = await mediator.Send(query);

        if (result.IsFailed)
        {
            return GenerateUnknown400ErrorResponse(result.Errors);
        }
        if (result.Value.IsNull())
        {
            return GenerateGeneric404ErrorResponse();
        }
        return Ok(result.Value);
    }

    [HttpGet("provider-state", Name = "GetProviderState")]
    [UseCircuitBreaker]
    [RequiresTenantIdHeader]
    [LowerCaseIgnoreNullActionFilter]
    [Authorize(Constants.ServiceAuthorizationPolicies.ReadProviderState)]
    [ProducesResponseType(typeof(GetProviderStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status424FailedDependency)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetProviderState(
        [FromQuery, Required] Guid paymentRequestId)
    {
        var query = new GetProviderStateQuery()
        {
            PaymentRequestId = paymentRequestId
        };

        var result = await mediator.Send(query);
        if (result.IsFailed)
        {
            return HandleErrors(result);
        }

        var apiResponse = mapper.Map<GetProviderStateResponse>(result.Value.ProviderState);
        return Ok(apiResponse);
    }

    [HttpPost("request-cancel/{paymentRequestId}", Name = "RequestCancel")]
    [UseCircuitBreaker]
    [RequiresTenantIdHeader]
    [Authorize(Constants.ServiceAuthorizationPolicies.RequestCancel)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestCancel(
        Guid paymentRequestId,
        [FromBody, Required] RequestCancelPayload cancellationPayload)
    {
        var command = mapper.Map<RequestCancelCommand>(cancellationPayload);
        command.PaymentRequestId = paymentRequestId;
        command.XeroTenantId = XeroTenantId;
        command.XeroCorrelationId = XeroCorrelationId;

        var response = await mediator.Send(command);

        return response.IsSuccess ? Accepted() : HandleErrors(response);
    }

    [HttpPost("cancel/{paymentRequestId}", Name = "Cancel")]
    [UseCircuitBreaker]
    [RequiresTenantIdHeader]
    [Authorize(Constants.ServiceAuthorizationPolicies.Cancel)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Cancel(
        [FromRoute, Required] Guid paymentRequestId,
        [FromBody, Required] CancelPayload cancelPayload)
    {
        var command = new SynchronousCancellationCommand()
        {
            PaymentRequestId = paymentRequestId,
            CancellationReason = cancelPayload.CancellationReason,
            TenantId = XeroTenantId.ToString(),
            CorrelationId = XeroCorrelationId.ToString()
        };
        var result = await mediator.Send(command);

        return result.IsSuccess ? new NoContentResult() : HandleErrors(result);
    }

    private ExecutionQueueMessage MapRequestToExecutionQueueMessage(CompletePaymentTransactionRequest request, Guid paymentRequestId, Guid providerServiceId)
    {
        var executionQueueMessage = mapper.Map<ExecutionQueueMessage>(request);
        executionQueueMessage.PaymentRequestId = paymentRequestId;
        executionQueueMessage.ProviderServiceId = providerServiceId;

        return executionQueueMessage;
    }
}
