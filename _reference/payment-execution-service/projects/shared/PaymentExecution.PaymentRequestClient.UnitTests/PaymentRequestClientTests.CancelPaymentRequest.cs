using System.Net;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PaymentExecution.PaymentRequestClient.Models.Requests;
using PaymentExecution.TestUtilities;
using Polly;
using Polly.Registry;
namespace PaymentExecution.PaymentRequestClient.UnitTests;

public class PaymentRequestClientTests_CancelPaymentRequest
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Guid _paymentRequestId = Guid.NewGuid();
    private readonly Uri _baseUri = new Uri("https://baseurl.com/requests/");
    private readonly string _cancelExecutionInProgressPaymentRequestEndpoint = "v1/payment-requests/{request-id}/cancel-execution-in-progress";
    private readonly PaymentRequestClient _client;
    private readonly Uri _expectedUri;
    private readonly Mock<ILogger<PaymentRequestClient>> _mockLogger;

    public PaymentRequestClientTests_CancelPaymentRequest()
    {
        var mockPipelineProvider = new Mock<ResiliencePipelineProvider<string>>();
        var emptyPipeline = ResiliencePipeline.Empty;

        mockPipelineProvider
            .Setup(p => p.GetPipeline(nameof(PaymentRequestClient)))
            .Returns(emptyPipeline);
        _mockLogger = new Mock<ILogger<PaymentRequestClient>>();
        var mockOptions = new Mock<IOptions<PaymentRequestServiceOptions>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        mockOptions.Setup(o => o.Value)
            .Returns(new PaymentRequestServiceOptions
            {
                BaseUrl = _baseUri.ToString(),
                SubmitPaymentRequestEndpoint = "v1/payment-requests/{request-id}/submit",
                ExecutionSuccessPaymentRequestEndpoint = "v1/payment-requests/{request-id}/execution-succeed",
                FailurePaymentRequestEndpoint = "v1/payment-requests/{request-id}/fail",
                GetPaymentRequestByIdEndpoint = "v1/payment-requests/{request-id}",
                CancelExecutionInProgressPaymentRequestEndpoint = _cancelExecutionInProgressPaymentRequestEndpoint
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = _baseUri
        };

        var relativeUriWithRequestId = _cancelExecutionInProgressPaymentRequestEndpoint.Replace("{request-id}", _paymentRequestId.ToString());
        _expectedUri = new Uri(_baseUri, relativeUriWithRequestId);

        _client = new PaymentRequestClient(httpClient, _mockLogger.Object, mockOptions.Object, mockPipelineProvider.Object);
    }

    [Theory, AutoData]
    public async Task GivenPaymentRequestReturnsSuccessfulStatusCode_WhenCancelPaymentRequestCalled_ThenReturnsResultNoContent(
        CancelPaymentRequest mockCancelPaymentRequest)
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act
        var result = await _client.CancelPaymentRequest(_paymentRequestId, mockCancelPaymentRequest,
            "correlation-id", "tenant-id");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task GivenValidRequest_WhenCancelPaymentRequestCalled_ThenRequestUriIsConstructedCorrectly(
        CancelPaymentRequest mockCancelPaymentRequest)
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

        // Act
        await _client.CancelPaymentRequest(_paymentRequestId, mockCancelPaymentRequest,
            "correlation-id", "tenant-id");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest.RequestUri.Should().NotBeNull();
        capturedRequest.RequestUri.Should().Be(_expectedUri);
    }

    [Theory]
    [InlineAutoData(HttpStatusCode.BadRequest)]
    [InlineAutoData(HttpStatusCode.Forbidden)]
    [InlineAutoData(HttpStatusCode.InternalServerError)]
    public async Task GivenNonSuccessfulStatusCode_WhenCancelPaymentRequestCalled_ThenReturnsResultFail(
        HttpStatusCode mockedStatusCode,
        CancelPaymentRequest mockCancelPaymentRequest)
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri != null && req.RequestUri == _expectedUri),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = mockedStatusCode,
                Content = new StringContent("Bad Request")
            });

        // Act
        var result = await _client.CancelPaymentRequest(_paymentRequestId, mockCancelPaymentRequest,
            "correlation-id", "tenant-id");

        // Assert
        result.IsFailed.Should().BeTrue();
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_mockLogger, LogLevel.Error, "Failed to cancel payment request for paymentRequestId", 1);

    }

    [Theory, AutoData]
    public void GivenExceptionThrownGettingPipeline_WhenCancelPaymentRequestCalled_ThenExceptionIsPropagated(CancelPaymentRequest mockCancelRequest)
    {
        // Arrange
        var mockPipelineProvider = new Mock<ResiliencePipelineProvider<string>>();
        mockPipelineProvider
            .Setup(p => p.GetPipeline(nameof(PaymentRequestClient)))
            .Throws(new System.Exception("Something happened getting the pipeline"));

        // Act
        var act = async () => await _client.CancelPaymentRequest(_paymentRequestId, mockCancelRequest,
            "correlation-id", "tenant-id");

        // Assert
        act.Should().ThrowAsync<System.Exception>();
    }
}
