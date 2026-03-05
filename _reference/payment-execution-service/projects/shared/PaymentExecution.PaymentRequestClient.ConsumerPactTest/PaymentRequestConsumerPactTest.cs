using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PactNet;
using PaymentExecution.TestUtilities;
using Polly;
using Polly.Registry;

namespace PaymentExecution.PaymentRequestClient.ConsumerPactTest;

public static class PaymentRequestConsumerPactTest
{

    public static PaymentRequestClient CreatePaymentRequestClient(IConsumerContext ctx, string scope, Guid organisationId,
        Guid correlationId)
    {
        var mockPipelineProvider = new Mock<ResiliencePipelineProvider<string>>();
        mockPipelineProvider
            .Setup(p => p.GetPipeline(nameof(PaymentRequestClient)))
            .Returns(ResiliencePipeline.Empty);

        var mockOptions = new Mock<IOptions<PaymentRequestServiceOptions>>();
        mockOptions.Setup(ap => ap.Value).Returns(new PaymentRequestServiceOptions
        {
            SubmitPaymentRequestEndpoint = Constants.SubmitEndpoint,
            ExecutionSuccessPaymentRequestEndpoint = Constants.ExecutionSucceedEndpoint,
            FailurePaymentRequestEndpoint = Constants.FailureEndpoint,
            GetPaymentRequestByIdEndpoint = Constants.GetPaymentRequestByIdEndpoint,
            CancelExecutionInProgressPaymentRequestEndpoint = Constants.CancelExecutionInProgressEndpoint,
            BaseUrl = ctx.MockServerUri.ToString()
        });

        var httpClient = PactConsumerClientFactory.CreateHttpClient(ctx.MockServerUri, organisationId.ToString(),
                correlationId.ToString(), scope);

        var apiClient = new PaymentRequestClient(
            httpClient,
            Mock.Of<ILogger<PaymentRequestClient>>(),
            mockOptions.Object,
            mockPipelineProvider.Object);

        return apiClient;
    }
}
