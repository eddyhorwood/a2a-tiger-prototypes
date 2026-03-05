using System.Net;
using System.Text;
using System.Text.Json;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PaymentExecution.Common;
using PaymentExecution.PaymentRequestClient.Models;
using PaymentExecution.TestUtilities;
using Polly;
using Polly.Registry;
using ErrorMessage = PaymentExecution.PaymentRequestClient.Models.ErrorMessage;

namespace PaymentExecution.PaymentRequestClient.UnitTests;

public class PaymentRequestClientTests_SubmitPaymentRequest
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly PaymentRequestClient _client;
    private readonly Mock<ILogger<PaymentRequestClient>> _mockLogger;
    private readonly Guid _paymentRequestId = Guid.NewGuid();
    private readonly string _baseUrl = "https://api.example.com/";
    private readonly string _submitEndpoint = "v1/payment-requests/{request-id}/submit";
    private readonly string _successEndpoint = "v1/payment-requests/{request-id}/execution-succeed";
    private readonly string _failureEndpoint = "v1/payment-requests/{request-id}/fail";
    private readonly string _expectedUrl;

    public PaymentRequestClientTests_SubmitPaymentRequest()
    {
        var mockPipelineProvider = new Mock<ResiliencePipelineProvider<string>>();
        var emptyPipeline = ResiliencePipeline.Empty;

        mockPipelineProvider
            .Setup(p => p.GetPipeline(nameof(PaymentRequestClient)))
            .Returns(emptyPipeline);
        _mockLogger = new Mock<ILogger<PaymentRequestClient>>();
        Mock<IOptions<PaymentRequestServiceOptions>> mockOptions = new Mock<IOptions<PaymentRequestServiceOptions>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_baseUrl)
        };

        mockOptions.Setup(o => o.Value)
            .Returns(new PaymentRequestServiceOptions
            {
                BaseUrl = _baseUrl,
                SubmitPaymentRequestEndpoint = _submitEndpoint,
                ExecutionSuccessPaymentRequestEndpoint = _successEndpoint,
                FailurePaymentRequestEndpoint = _failureEndpoint,
                GetPaymentRequestByIdEndpoint = "placeholder",
                CancelExecutionInProgressPaymentRequestEndpoint = "placeholder"
            });
        _expectedUrl = $"{mockOptions.Object.Value.BaseUrl}{mockOptions.Object.Value.SubmitPaymentRequestEndpoint}"
            .Replace("{request-id}", _paymentRequestId.ToString());

        _client = new PaymentRequestClient(httpClient, _mockLogger.Object, mockOptions.Object, mockPipelineProvider.Object);
    }


    [Theory]
    [AutoData]
    public async Task GivenValidRequest_WhenSubmitPaymentRequestCalled_ThenReturnsSuccess(PaymentRequest paymentRequest)
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Post && req.RequestUri != null && req.RequestUri.ToString() == _expectedUrl),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonSerializer.Serialize(paymentRequest), Encoding.UTF8, "application/json") });

        // Act
        Func<Task> act = async () => await _client.SubmitPaymentRequest(_paymentRequestId);

        // Assert
        await act.Should().NotThrowAsync();
    }


    [Theory]
    [AutoData]
    public async Task GivenValidRequest_WhenSubmitPaymentRequestCalled_ThenRequestUrlIsConstructedCorrectly(PaymentRequest paymentRequest)
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
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonSerializer.Serialize(paymentRequest), Encoding.UTF8, "application/json") });

        // Act
        await _client.SubmitPaymentRequest(_paymentRequestId);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri.Should().NotBeNull();
        capturedRequest.RequestUri!.ToString().Should().Be(_expectedUrl);
    }

    [Fact]
    public async Task GivenHttpBadRequestResponse_WhenSubmitPaymentRequestCalled_ThenReturnsResultFail()
    {
        // Arrange
        var problemDetails = new ProblemDetailsExtended
        {
            Detail = "Bad Request",
            Type = "https://example.com/errors/bad-request",
            ErrorCode = "execution_submit_payment_failed",
            ProviderErrorCode = string.Empty
        };

        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent(JsonSerializer.Serialize(problemDetails), Encoding.UTF8, "application/json")
        };

        SetUpHttpHandlerToReturnResponse(httpResponse);
        var expectedLogMessage = $"Submit payment request failed with status code: {HttpStatusCode.BadRequest}";

        // Act
        var result = await _client.SubmitPaymentRequest(_paymentRequestId);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal(ErrorMessage.SubmitPaymentRequestBadRequest, result.Errors.FirstOrDefault()?.Message);
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_mockLogger, LogLevel.Error, expectedLogMessage, 1);
    }

    [Fact]
    public async Task GivenHttp500Response_WhenSubmitPaymentRequestCalled_ThenThrowsPaymentRequestException()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = new StringContent("Bad Request")
        };
        SetUpHttpHandlerToReturnResponse(httpResponse);

        // Act
        var act = async () => await _client.SubmitPaymentRequest(_paymentRequestId);

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(act);
    }

    [Fact]
    public async Task Given200ResponseWithMalFormedPaymentRequest_WhenSubmitPaymentRequestCalled_ThenDeserializationThrowsException()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("Incorrect shape request!", Encoding.UTF8, "application/json")
        };
        SetUpHttpHandlerToReturnResponse(httpResponse);

        // Act
        var act = async () => await _client.SubmitPaymentRequest(_paymentRequestId);

        // Assert
        await Assert.ThrowsAsync<JsonException>(act);
    }

    private void SetUpHttpHandlerToReturnResponse(HttpResponseMessage httpResponseMessage)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri != null && req.RequestUri.ToString() == _expectedUrl),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = httpResponseMessage.StatusCode,
                Content = httpResponseMessage.Content
            });
    }
}
