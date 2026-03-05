using System.Net;
using System.Text.Json;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PaymentExecution.Common;
using PaymentExecution.PaymentRequestClient.Models;
using Polly;
using Polly.Registry;

namespace PaymentExecution.PaymentRequestClient.UnitTests;

public class PaymentRequestClientTests_GetPaymentRequestByPaymentRequestId
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Guid _paymentRequestId = Guid.NewGuid();
    private readonly Uri _baseUri = new Uri("https://baseurl.com/requests/");
    private readonly string _getPaymentRequestById = "v1/payment-requests/{request-id}";
    private readonly PaymentRequestClient _client;
    private readonly Uri _expectedUri;
    private readonly JsonSerializerOptions _opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PaymentRequestClientTests_GetPaymentRequestByPaymentRequestId()
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
                SubmitPaymentRequestEndpoint = "placeholder",
                ExecutionSuccessPaymentRequestEndpoint = "placeholder",
                FailurePaymentRequestEndpoint = "placeholder",
                GetPaymentRequestByIdEndpoint = _getPaymentRequestById,
                CancelExecutionInProgressPaymentRequestEndpoint = "placeholder"
            });

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = _baseUri
        };

        var relativeUriWithRequestId = _getPaymentRequestById.Replace("{request-id}", _paymentRequestId.ToString());
        _expectedUri = new Uri(_baseUri, relativeUriWithRequestId);

        _client = new PaymentRequestClient(httpClient, mockLogger.Object, mockOptions.Object, mockPipelineProvider.Object);
    }


    [Theory]
    [InlineAutoData(HttpStatusCode.OK)]
    [InlineAutoData(HttpStatusCode.Accepted)]
    [InlineAutoData(HttpStatusCode.NoContent)]
    public async Task GivenPaymentRequestReturnsSuccessfulStatusCode_WhenExecutionSuccessPaymentRequestCalled_ThenReturnsResultOkWithPaymentRequest(
        HttpStatusCode mockStatusCode, PaymentRequest mockPaymentRequest)
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = mockStatusCode,
                Content = new StringContent(JsonSerializer.Serialize(mockPaymentRequest, _opts))
            });

        // Act
        var result = await _client.GetPaymentRequestByPaymentRequestId(_paymentRequestId, Guid.NewGuid().ToString());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(mockPaymentRequest);
    }

    [Fact]
    public async
        Task GivenPaymentRequestReturnsSuccessfulStatusCodeAndHasResponseBodyOfUnexpectedShape_WhenExecutionSuccessPaymentRequestCalled_ThenPropagatesException()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("invalid string!")
            });

        // Act
        var act = async () => await _client.GetPaymentRequestByPaymentRequestId(_paymentRequestId, Guid.NewGuid().ToString());

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    [Theory, AutoData]
    public async Task GivenValidRequest_WhenExecutionSuccessPaymentRequestCalled_ThenRequestUriIsConstructedCorrectly(PaymentRequest mockPaymentRequest)
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockPaymentRequest, _opts))
            });

        // Act
        await _client.GetPaymentRequestByPaymentRequestId(_paymentRequestId, Guid.NewGuid().ToString());

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
        HttpStatusCode mockedStatusCode)
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri != null && req.RequestUri == _expectedUri),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = mockedStatusCode
            });

        // Act
        var response = await _client.GetPaymentRequestByPaymentRequestId(_paymentRequestId, Guid.NewGuid().ToString());

        // Assert
        response.IsFailed.Should().BeTrue();
        var responseStatusCode = ((PaymentExecutionError)response.Errors[0]).GetHttpStatusCode();
        responseStatusCode.Should().Be(mockedStatusCode);
    }

    [Fact]
    public async Task GivenHttpCallThrowsException_WhenExecutionSuccessPaymentRequestCalled_ThenExceptionIsPropagated()
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
        var act = async () => await _client.GetPaymentRequestByPaymentRequestId(_paymentRequestId, Guid.NewGuid().ToString());

        // Assert
        await act.Should().ThrowAsync<System.Exception>().WithMessage("Oh dear!!");
    }

    [Fact]
    public void GivenExceptionThrownGettingPipeline_WhenGetPaymentRequestByPaymentRequestIdCalled_ThenExceptionIsPropagated()
    {
        // Arrange
        var mockPipelineProvider = new Mock<ResiliencePipelineProvider<string>>();
        mockPipelineProvider
            .Setup(p => p.GetPipeline(nameof(PaymentRequestClient)))
            .Throws(new System.Exception("Something happened getting the pipeline"));

        // Act
        var act = async () => await _client.GetPaymentRequestByPaymentRequestId(_paymentRequestId, Guid.NewGuid().ToString());

        // Assert
        act.Should().ThrowAsync<System.Exception>();
    }
}
