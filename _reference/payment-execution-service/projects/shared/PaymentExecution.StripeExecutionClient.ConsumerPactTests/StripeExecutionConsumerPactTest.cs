using Microsoft.Extensions.Options;
using Moq;
using PactNet;
using PaymentExecution.StripeExecutionClient.Options;
using PaymentExecution.TestUtilities;
using Polly;
using Polly.Registry;


namespace PaymentExecution.StripeExecutionClient.ConsumerPactTests;

public static class StripeExecutionConsumerPactTest
{
    public static StripeExecutionInternalHttpClient CreateStripeExecutionClient(
        IConsumerContext ctx,
        string scope,
        Guid? organisationId,
        Guid? correlationId,
        string? providerAccountId)
    {
        var mockPipelineProvider = new Mock<ResiliencePipelineProvider<string>>();
        mockPipelineProvider
            .Setup(p => p.GetPipeline(nameof(StripeExecutionInternalHttpClient)))
            .Returns(ResiliencePipeline.Empty);

        var mockOptions = new Mock<IOptions<StripeExecutionServiceOptions>>();
        mockOptions.Setup(ap => ap.Value).Returns(new StripeExecutionServiceOptions
        {
            SubmitEndpoint = Constants.StripeSubmitEndpoint,
            BaseUrl = ctx.MockServerUri.ToString(),
            CancelEndpoint = Constants.StripeCancelEndpoint,
            GetPaymentIntentEndpoint = Constants.StripeGetPaymentIntentEndpoint
        });

        var httpClient = PactConsumerClientFactory.CreateHttpClient(ctx.MockServerUri, organisationId.ToString(),
            correlationId.ToString(), scope);

        if (!string.IsNullOrWhiteSpace(providerAccountId))
        {
            httpClient.DefaultRequestHeaders.Add(Constants.ProviderAccountId, providerAccountId);
        }

        var apiClient = new StripeExecutionInternalHttpClient(
            httpClient,
            mockOptions.Object,
            mockPipelineProvider.Object);

        return apiClient;
    }
}
