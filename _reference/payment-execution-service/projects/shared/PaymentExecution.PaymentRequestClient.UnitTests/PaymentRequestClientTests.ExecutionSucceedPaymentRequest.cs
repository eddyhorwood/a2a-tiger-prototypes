using System.Net;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PaymentExecution.Common;
using PaymentExecution.PaymentRequestClient.Models.Requests;
using Polly;
using Polly.Registry;

namespace PaymentExecution.PaymentRequestClient.UnitTests;

public class PaymentRequestClientTests_ExecutionSucceedPaymentRequest
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Guid _paymentRequestId = Guid.NewGuid();
    private readonly Uri _baseUri = new Uri("https://baseurl.com/requests/");
    private readonly string _executionSuccessEndpoint = "v1/payment-requests/{request-id}/execution-succeed";
    private readonly PaymentRequestClient _client;
    private readonly Uri _expectedUri;

    public PaymentRequestClientTests_ExecutionSucceedPaymentRequest()
    {
        var mockPipelineProvider = new Mock<ResiliencePipelineProvider<string>>();
        var emptyPipeline = ResiliencePipeline.Empty;

        mockPipelineProvider
            .Setup(p => p.GetPipeline(nameof(PaymentRequestClient)))
            .Returns(emptyPipeline);
        var mockLogger = new Mock<ILogger<PaymentRequestClient>>();
        var mockOptions = new Mock<IOptions<PaymentRequestServiceOptions>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        mockOptions.Setup(o => o.Value)
            .Returns(new PaymentRequestServiceOptions
            {
                BaseUrl = _baseUri.ToString(),
                SubmitPaymentRequestEndpoint = "v1/payment-requests/{request-id}/submit",
                ExecutionSuccessPaymentRequestEndpoint = _executionSuccessEndpoint,
                FailurePaymentRequestEndpoint = "v1/payment-requests/{request-id}/fail",
                GetPaymentRequestByIdEndpoint = "v1/payment-requests/{request-id}",
                CancelExecutionInProgressPaymentRequestEndpoint = "v1/payment-requests/{request-id}/cancel-execution-in-progress"
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = _baseUri
        };

        var relativeUriWithRequestId = _executionSuccessEndpoint.Replace("{request-id}", _paymentRequestId.ToString());
        _expectedUri = new Uri(_baseUri, relativeUriWithRequestId);

        _client = new PaymentRequestClient(httpClient, mockLogger.Object, mockOptions.Object, mockPipelineProvider.Object);
    }


    [Theory]
    [InlineAutoData(HttpStatusCode.OK)]
    [InlineAutoData(HttpStatusCode.Accepted)]
    [InlineAutoData(HttpStatusCode.NoContent)]
    public async Task GivenPaymentRequestReturnsSuccessfulStatusCode_WhenExecutionSuccessPaymentRequestCalled_ThenReturnsResultOk(
        HttpStatusCode mockStatusCode, SuccessPaymentRequest mockSuccessRequest)
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = mockStatusCode });

        // Act
        var result = await _client.ExecutionSucceedPaymentRequest(_paymentRequestId, mockSuccessRequest,
            "correlation-id", "tenant-id");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Theory, AutoData]
    public async Task GivenValidRequest_WhenExecutionSuccessPaymentRequestCalled_ThenRequestUriIsConstructedCorrectly(
        SuccessPaymentRequest mockSuccessRequest)
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
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        await _client.ExecutionSucceedPaymentRequest(_paymentRequestId, mockSuccessRequest,
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
    public async Task GivenPaymentRequestReturnsNonSuccessfulStatusCode_WhenExecutionSuccessPaymentRequestCalled_ThenReturnsResultFailWithErrorWithStatusCode(
        HttpStatusCode mockedStatusCode,
        SuccessPaymentRequest mockSuccessRequest)
    {
        // Arrange
        var expectedStringContent = "Oh dear something went wrong!";
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
                Content = new StringContent(expectedStringContent)
            });

        // Act
        var response = await _client.ExecutionSucceedPaymentRequest(_paymentRequestId, mockSuccessRequest,
            "correlation-id", "tenant-id");

        // Assert
        response.IsFailed.Should().BeTrue();
        var responseStatusCode = ((PaymentExecutionError)response.Errors[0]).GetHttpStatusCode();
        responseStatusCode.Should().Be(mockedStatusCode);
    }

    [Theory, AutoData]
    public async Task GivenHttpCallThrowsException_WhenExecutionSuccessPaymentRequestCalled_ThenExceptionIsPropagated(
        SuccessPaymentRequest mockSuccessRequest)
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new System.Exception("Oh dear!!"));

        // Act
        var act = async () => await _client.ExecutionSucceedPaymentRequest(_paymentRequestId, mockSuccessRequest,
            "correlation-id", "tenant-id");

        // Assert
        await act.Should().ThrowAsync<System.Exception>().WithMessage("Oh dear!!");
    }

    [Theory, AutoData]
    public void GivenExceptionThrownGettingPipeline_WhenExecutionSucceedPaymentRequestCalled_ThenExceptionIsPropagated(SuccessPaymentRequest mockSuccessRequest)
    {
        // Arrange
        var mockPipelineProvider = new Mock<ResiliencePipelineProvider<string>>();
        mockPipelineProvider
            .Setup(p => p.GetPipeline(nameof(PaymentRequestClient)))
            .Throws(new System.Exception("Something happened getting the pipeline"));

        // Act
        var act = async () => await _client.ExecutionSucceedPaymentRequest(_paymentRequestId, mockSuccessRequest,
            "correlation-id", "tenant-id");

        // Assert
        act.Should().ThrowAsync<System.Exception>();
    }
}
