using System.Net;
using System.Text;
using System.Text.Json;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PaymentExecution.StripeExecutionClient.Contracts.Models;
using PaymentExecution.StripeExecutionClient.Options;
using Polly;
using Polly.Registry;

namespace PaymentExecution.StripeExecutionClient.UnitTests;

public class StripeExecutionInternalHttpClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly StripeExecutionInternalHttpClient _internalHttpClient;
    private const string BaseUrl = "https://stripe-execution-base/";
    private const string SubmitEndpoint = "v1/payments/submit";
    private const string CancelEndpoint = "v1/payments/cancel/{request-id}";
    private const string GetPaymentIntentEndpoint = "v1/payments/payment-intent?paymentRequestId={payment-request-id}";

    public StripeExecutionInternalHttpClientTests()
    {
        var mockPipelineProvider = new Mock<ResiliencePipelineProvider<string>>();
        var emptyPipeline = ResiliencePipeline.Empty;

        mockPipelineProvider
            .Setup(p => p.GetPipeline(nameof(StripeExecutionInternalHttpClient)))
            .Returns(emptyPipeline);
        var mockOptions = new Mock<IOptions<StripeExecutionServiceOptions>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(BaseUrl)
        };
        mockOptions.Setup(o => o.Value)
            .Returns(new StripeExecutionServiceOptions
            {
                BaseUrl = BaseUrl,
                SubmitEndpoint = SubmitEndpoint,
                CancelEndpoint = CancelEndpoint,
                GetPaymentIntentEndpoint = GetPaymentIntentEndpoint
            });

        _internalHttpClient = new StripeExecutionInternalHttpClient(httpClient, mockOptions.Object, mockPipelineProvider.Object);
    }

    [Theory]
    [AutoData]
    public async Task GivenValidRequest_WhenSubmitStripeExecutionAsync_ThenRequestBodyIsSerializedCorrectly(StripeExeSubmitPaymentRequestDto submitRequest, StripeExeSubmitPaymentResponseDto submitResponse)
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockedHttpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(submitResponse), Encoding.UTF8, "application/json")
        };
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockedHttpResponse);

        // Act
        await _internalHttpClient.SubmitStripeExecutionAsync(submitRequest);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Content.Should().NotBeNull();
        var requestBody = await capturedRequest.Content!.ReadAsStringAsync();
        requestBody.Should().Be(JsonSerializer.Serialize(submitRequest));
    }


    [Theory]
    [AutoData]
    public async Task GivenValidRequest_WhenSubmitStripeExecutionAsync_ThenRequestUrlIsConstructedCorrectly(StripeExeSubmitPaymentRequestDto submitRequest, StripeExeSubmitPaymentResponseDto submitPaymentResponse)
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockedHttpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(submitPaymentResponse), Encoding.UTF8,
                "application/json")
        };
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockedHttpResponse);

        // Act
        await _internalHttpClient.SubmitStripeExecutionAsync(submitRequest);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri.Should().NotBeNull();
        capturedRequest.RequestUri!.ToString().Should().Be($"{BaseUrl}{SubmitEndpoint}");
    }

    [Theory]
    [AutoData]
    public async Task GivenHttpClientThrowsAnException_WhenSubmitStripeExecutionAsync_ThenExceptionIsPropogated(StripeExeSubmitPaymentRequestDto submitRequest)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new System.Exception("Oh dear something blew up!"));

        var act = async () => await _internalHttpClient.SubmitStripeExecutionAsync(submitRequest);

        await Assert.ThrowsAsync<System.Exception>(act);
    }

    [Fact]
    public async Task GivenUnhealthyDownstreamService_WhenPingAsync_ThenUnhealthyResponseCodeReturned()
    {
        var mockedHttpResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri!.ToString().Contains("ping")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(mockedHttpResponse);

        var response = await _internalHttpClient.PingAsync(CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [AutoData]
    public async Task GivenValidRequest_WhenCancelStripeExecutionAsync_ThenRequestBodyIsSerializedCorrectly(StripeExeCancelPaymentRequestDto cancelRequest)
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockedHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            StatusCode = HttpStatusCode.OK,
        };
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockedHttpResponse);

        // Act
        await _internalHttpClient.CancelStripeExecutionAsync(cancelRequest);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Content.Should().NotBeNull();
        var requestBody = await capturedRequest.Content!.ReadAsStringAsync();
        var expectedBody = JsonSerializer.Serialize(new { cancellationReason = cancelRequest.CancellationReason });
        requestBody.Should().Be(expectedBody);
    }

    [Theory]
    [AutoData]
    public async Task GivenValidRequest_WhenCancelStripeExecutionAsync_ThenRequestUrlIsConstructedCorrectly(StripeExeCancelPaymentRequestDto cancelRequest)
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockedHttpResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockedHttpResponse);

        // Act
        await _internalHttpClient.CancelStripeExecutionAsync(cancelRequest);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri.Should().NotBeNull();
        var expectedUrl = $"{BaseUrl}v1/payments/cancel/{cancelRequest.PaymentRequestId}";
        capturedRequest.RequestUri!.ToString().Should().Be(expectedUrl);
    }

    [Theory]
    [AutoData]
    public async Task GivenHttpClientThrowsAnException_WhenCancelStripeExecutionAsync_ThenExceptionIsPropagated(StripeExeCancelPaymentRequestDto cancelRequest)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new System.Exception("Oh dear something blew up!"));

        var act = async () => await _internalHttpClient.CancelStripeExecutionAsync(cancelRequest);

        await Assert.ThrowsAsync<System.Exception>(act);
    }

    [Theory]
    [AutoData]
    public async Task GivenValidRequest_WhenGetPaymentIntentByPaymentRequestIdAsync_ThenRequestUrlIsConstructedCorrectly(
        Guid paymentRequestId,
        Guid correlationId,
        Guid tenantId)
    {
        // Arrange
        var expectedUrl = $"{BaseUrl}v1/payments/payment-intent?paymentRequestId={paymentRequestId}";
        HttpRequestMessage? capturedRequest = null;
        var mockedHttpResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockedHttpResponse);

        // Act
        await _internalHttpClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri.Should().NotBeNull();
        capturedRequest.RequestUri!.ToString().Should().Be(expectedUrl);
    }

    [Theory]
    [InlineAutoData(HttpStatusCode.OK)]
    [InlineAutoData(HttpStatusCode.BadRequest)]
    [InlineAutoData(HttpStatusCode.InternalServerError)]
    public async Task GivenValidRequest_WhenGetPaymentIntentByPaymentRequestIdAsync_ThenReturnsExpectedHttpResponse(
        HttpStatusCode mockedStatusCode,
        Guid paymentRequestId,
        Guid correlationId,
        Guid tenantId)
    {
        // Arrange
        var expectedHttpResponse = new HttpResponseMessage(mockedStatusCode);
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(expectedHttpResponse);

        // Act
        var response = await _internalHttpClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId);

        // Assert
        response.Should().BeEquivalentTo(expectedHttpResponse);
    }


    [Theory]
    [AutoData]
    public async Task GivenHttpClientThrowsAnException_WhenGetPaymentIntentByPaymentRequestIdAsync_ThenExceptionIsPropagated(Guid paymentRequestId, Guid correlationId, Guid tenantId)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new System.Exception("Oh dear something blew up!"));

        var act = async () => await _internalHttpClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId);

        await Assert.ThrowsAsync<System.Exception>(act);
    }

    [Theory]
    [AutoData]
    public async Task GivenValidRequest_WhenGetPaymentIntentByPaymentRequestIdAsync_ThenHeadersAreAddedCorrectly(
        Guid paymentRequestId,
        Guid correlationId,
        Guid tenantId)
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockedHttpResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockedHttpResponse);

        // Act
        await _internalHttpClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Should().ContainKey("Xero-Correlation-Id");
        capturedRequest.Headers.GetValues("Xero-Correlation-Id").Should().ContainSingle()
            .Which.Should().Be(correlationId.ToString());
        capturedRequest.Headers.Should().ContainKey("Xero-Tenant-Id");
        capturedRequest.Headers.GetValues("Xero-Tenant-Id").Should().ContainSingle()
            .Which.Should().Be(tenantId.ToString());
    }

    [Theory]
    [AutoData]
    public async Task GivenNoHeadersProvided_WhenGetPaymentIntentByPaymentRequestIdAsync_ThenNoHeadersAreAdded(
        Guid paymentRequestId)
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockedHttpResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri != null),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockedHttpResponse);

        // Act - Call without optional parameters (API scenario - relies on header propagation)
        await _internalHttpClient.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId);

        // Assert - Headers should NOT be added by this method (header propagation middleware will add them)
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Contains("Xero-Correlation-Id").Should().BeFalse();
        capturedRequest.Headers.Contains("Xero-Tenant-Id").Should().BeFalse();
    }
}
