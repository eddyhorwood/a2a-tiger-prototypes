using System.Net;
using FluentResults;
using PaymentExecution.PaymentRequestClient;
using PaymentExecution.PaymentRequestClient.Models;
using PaymentExecution.PaymentRequestClient.Models.Enums;
using PaymentExecution.PaymentRequestClient.Models.Requests;

namespace PaymentExecutionService.ProviderPactTests.Mocks;

public class MockPaymentRequestClient : IPaymentRequestClient
{
    private readonly DateTime _placeholderDate = DateTime.Parse("3030-03-03 03:03:03");

    public Task<Result<PaymentRequest>> SubmitPaymentRequest(Guid paymentRequestId)
    {
        return Task.FromResult(Result.Ok(new PaymentRequest()
        {
            PaymentRequestId = paymentRequestId,
            OrganisationId = Guid.NewGuid(),
            PaymentDateUtc = _placeholderDate,
            ContactId = Guid.NewGuid(),
            Status = RequestStatus.created,
            BillingContactDetails = new BillingContactDetails() { Email = "email@email.com" },
            Amount = 0,
            Currency = "AUD",
            PaymentDescription = "Desc",
            SelectedPaymentMethod =
                new SelectedPaymentMethod { PaymentGatewayId = Guid.NewGuid(), PaymentMethodName = "card" },
            LineItems = [],
            SourceContext = new SourceContext() { Identifier = Guid.NewGuid(), Type = "invoice" },
            Executor = ExecutorType.webpay,
            MerchantReference = "reference"
        }));
    }

    public Task<Result> ExecutionSucceedPaymentRequest(Guid paymentRequestId, SuccessPaymentRequest paymentRequest,
        string xeroCorrelationId,
        string xeroTenantId)
    {
        return Task.FromResult(Result.Ok());
    }

    public Task<Result> FailPaymentRequest(Guid paymentRequestId, FailurePaymentRequest paymentRequest,
        string xeroCorrelationId,
        string xeroTenantId)
    {
        return Task.FromResult(Result.Ok());
    }

    public Task FailPaymentRequest(Guid paymentRequestId, FailurePaymentRequest paymentRequest)
    {
        return Task.FromResult(Result.Ok());
    }

    public Task<Result> CancelPaymentRequest(Guid paymentRequestId, CancelPaymentRequest cancelPaymentRequest, string xeroCorrelationId,
        string xeroTenantId)
    {
        return Task.FromResult(Result.Ok());
    }

    public Task<HttpResponseMessage> PingAsync(CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK
        };
        return Task.FromResult(response);
    }

    public Task<Result<PaymentRequest>> GetPaymentRequestByPaymentRequestId(Guid paymentRequestId, string xeroCorrelationId)
    {
        var response = new PaymentRequest()
        {
            PaymentRequestId = paymentRequestId,
            OrganisationId = Guid.NewGuid(),
            PaymentDateUtc = _placeholderDate,
            ContactId = Guid.NewGuid(),
            Status = RequestStatus.created,
            BillingContactDetails = new BillingContactDetails() { Email = "test@gmail.com" },
            Amount = 100.00m,
            Currency = "USD",
            PaymentDescription = "Test payment description",
            SelectedPaymentMethod = new SelectedPaymentMethod()
            {
                PaymentGatewayId = Guid.NewGuid(),
                PaymentMethodName = "card"
            },
            LineItems = new List<LineItem>(),
            SourceContext = new SourceContext()
            {
                Identifier = Guid.NewGuid(),
                Type = "invoice"
            },
            Executor = ExecutorType.paymentexecution,
            MerchantReference = "TEST_MERCHANT_REF_001"
        };
        return Task.FromResult(Result.Ok(response));
    }
}
