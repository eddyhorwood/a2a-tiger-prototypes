using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Polly;
using Polly.Registry;

namespace PaymentExecution.PaymentRequestClient.UnitTests;

public class PaymentRequestClientTests_Ping
{
    [Fact]
    public async Task GivenHealthyService_WhenPingAsyncCalled_ThenOkReturned()
    {
        // Arrange 
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockMessageHandler.Object) { BaseAddress = new Uri("https://abc.123/requests/") };

        var mockLogger = new Mock<ILogger<PaymentRequestClient>>();

        var options = new Mock<IOptions<PaymentRequestServiceOptions>>();
        options.Setup(o => o.Value)
            .Returns(new PaymentRequestServiceOptions
            {
                BaseUrl = "https://abc.123/requests/",
                SubmitPaymentRequestEndpoint = "placeholder",
                ExecutionSuccessPaymentRequestEndpoint = "placeholder",
                FailurePaymentRequestEndpoint = "placeholder",
                GetPaymentRequestByIdEndpoint = "placeholder",
                CancelExecutionInProgressPaymentRequestEndpoint = "placeholder"
            });

        var mockPipelineProvider = new Mock<ResiliencePipelineProvider<string>>();
        mockPipelineProvider
            .Setup(p => p.GetPipeline(It.IsAny<string>()))
            .Returns(ResiliencePipeline.Empty);

        mockMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri!.ToString().Contains("ping")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        var client = new PaymentRequestClient(httpClient, mockLogger.Object, options.Object, mockPipelineProvider.Object);

        // Act 
        var response = await client.PingAsync(CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
