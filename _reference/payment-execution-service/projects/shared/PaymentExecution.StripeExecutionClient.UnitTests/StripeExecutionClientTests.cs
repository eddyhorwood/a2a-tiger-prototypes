using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.UnitTests;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.StripeExecutionClient.UnitTests;

public class StripeExecutionClientTests
{
    private readonly Mock<ILogger<StripeExecutionClient>> _mockLogger;
    private readonly Mock<IStripeExecutionInternalHttpClient> _mockStripeExecutionHttpClient;
    private readonly StripeExecutionClient _client;
    private readonly JsonSerializerOptions _opts = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StripeExecutionClientTests()
    {
        _mockLogger = new Mock<ILogger<StripeExecutionClient>>();
        _mockStripeExecutionHttpClient = new Mock<IStripeExecutionInternalHttpClient>();
        _client = new StripeExecutionClient(_mockLogger.Object, _mockStripeExecutionHttpClient.Object);
    }

    [Theory]
    [AutoData]
    public async Task GivenClientReturnsSuccessfulResponse_WhenSubmitPaymentCalled_ThenReturnsSubmitPaymentResponseResult(StripeExeSubmitPaymentRequestDto submitRequest)
    {
        //Arrange
        var mockedResponseJson = await File.ReadAllTextAsync("data/submit-successful-response.json");
        var mockedResponse = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(mockedResponseJson, Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.SubmitStripeExecutionAsync(submitRequest))
            .ReturnsAsync(mockedResponse);

        //Act
        var result = await _client.SubmitPaymentAsync(submitRequest);

        //Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.IsType<StripeExeSubmitPaymentResponseDto>(result.Value);
        var expectedResponse = JsonSerializer.Deserialize<StripeExeSubmitPaymentResponseDto>(mockedResponseJson, _opts);
        Assert.Equivalent(expectedResponse, result.Value);
    }

    /// <summary>
    /// Context: A Stripe integration error is categorised as an unsuccessful status code with a providerErrorCode
    /// This can be then interpreted and subsequently handled by Execution
    /// </summary>
    [Theory]
    [InlineAutoData(HttpStatusCode.BadRequest)]
    [InlineAutoData(HttpStatusCode.PaymentRequired)]
    [InlineAutoData(HttpStatusCode.Forbidden)]
    [InlineAutoData(HttpStatusCode.Conflict)]
    [InlineAutoData(HttpStatusCode.UnprocessableEntity)]
    [InlineAutoData(HttpStatusCode.InternalServerError)]
    public async Task GivenStripeClientReturnsStripeIntegrationError_WhenSubmitPaymentCalled_ThenReturnsResultFailWithExpectedError(
        HttpStatusCode mockedStatusCode, StripeExeSubmitPaymentRequestDto submitRequest)
    {
        //Arrange
        var expectedDetailString = "Error message from stripe";
        var expectedProviderErrorCode = "invalid-stripe-operation";
        var mockedResponseJson = new JsonObject()
        {
            ["status"] = (int)mockedStatusCode,
            ["providerErrorCode"] = expectedProviderErrorCode,
            ["detail"] = expectedDetailString
        };
        var mockedResponse = new HttpResponseMessage()
        {
            StatusCode = mockedStatusCode,
            Content = new StringContent(mockedResponseJson.ToJsonString(), Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.SubmitStripeExecutionAsync(submitRequest))
            .ReturnsAsync(mockedResponse);
        var expectedError = new PaymentExecutionError(
            expectedDetailString,
            expectedProviderErrorCode,
            mockedStatusCode);

        //Act
        var result = await _client.SubmitPaymentAsync(submitRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Reasons.FirstOrDefault();
        error.Should().BeOfType<PaymentExecutionError>()
            .Which.Should().BeEquivalentTo(expectedError);
    }

    [Theory, AutoData]
    public async Task GivenStripeClientReturnsStructuredResponseBodyWithNullDetailAndProviderErrorCode_WhenSubmitPaymentCalled_ThenReturnsErrorWithExpectedValues(
        StripeExeSubmitPaymentRequestDto submitRequest)
    {
        //Arrange
        var expectedDefaultDetailString = "Failed Stripe Execution integration";
        var mockedUnsuccessfulStatusCode = HttpStatusCode.BadRequest;
        var mockedResponseJson = new JsonObject()
        {
            ["status"] = (int)mockedUnsuccessfulStatusCode,
            ["providerErrorCode"] = null,
            ["detail"] = null
        };
        var mockedResponse = new HttpResponseMessage()
        {
            StatusCode = mockedUnsuccessfulStatusCode,
            Content = new StringContent(mockedResponseJson.ToJsonString(), Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.SubmitStripeExecutionAsync(submitRequest))
            .ReturnsAsync(mockedResponse);
        var expectedError = new PaymentExecutionError(
            expectedDefaultDetailString,
            null,
            mockedUnsuccessfulStatusCode);

        //Act
        var result = await _client.SubmitPaymentAsync(submitRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Reasons.FirstOrDefault();
        error.Should().BeOfType<PaymentExecutionError>()
            .Which.Should().BeEquivalentTo(expectedError);
    }

    [Theory]
    [InlineAutoData(HttpStatusCode.BadRequest)]
    [InlineAutoData(HttpStatusCode.PaymentRequired)]
    [InlineAutoData(HttpStatusCode.UnprocessableEntity)]
    [InlineAutoData(HttpStatusCode.Forbidden)]
    [InlineAutoData(HttpStatusCode.InternalServerError)]
    [InlineAutoData(HttpStatusCode.ServiceUnavailable)]
    public async Task GivenClientReturnsUnsuccessfulStatusCodeWithUnstructuredResponseBody_WhenSubmitPaymentCalled_ThenReturnsResultFailWithExpectedError(
        HttpStatusCode mockedStatusCode, StripeExeSubmitPaymentRequestDto submitRequest)
    {
        var mockedResponse = new HttpResponseMessage()
        {
            StatusCode = mockedStatusCode,
            Content = new StringContent("oh dear!", Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.SubmitStripeExecutionAsync(submitRequest))
            .ReturnsAsync(mockedResponse);
        var expectedError = new PaymentExecutionError(
            "Failed Stripe Execution integration. oh dear!",
            null,
            mockedStatusCode);

        //Act
        var result = await _client.SubmitPaymentAsync(submitRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Reasons.FirstOrDefault();
        error.Should().BeOfType<PaymentExecutionError>()
            .Which.Should().BeEquivalentTo(expectedError);
    }

    [Theory]
    [AutoData]
    public async Task GivenClientReturnsMalformedResponse_WhenSubmitPaymentCalled_ThenDeserializationExceptionIsThrown(StripeExeSubmitPaymentRequestDto submitRequest)
    {
        var mockedResponse = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("Unexpected response!", Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.SubmitStripeExecutionAsync(submitRequest))
            .ReturnsAsync(mockedResponse);

        //Act
        var act = async () => await _client.SubmitPaymentAsync(submitRequest);

        //Assert
        await Assert.ThrowsAsync<JsonException>(act);
    }

    [Theory]
    [AutoData]
    public async Task GivenClientReturns200Response_WhenCancelPaymentCalled_ThenReturnsSuccessResult(StripeExeCancelPaymentRequestDto cancelRequest)
    {
        //Arrange
        var mockedResponse = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.CancelStripeExecutionAsync(cancelRequest))
            .ReturnsAsync(mockedResponse);

        //Act
        var result = await _client.CancelPaymentAsync(cancelRequest);

        //Assert
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [AutoData]
    public async Task GivenClientReturnsNonSuccessfulStatusCodeWithExtendedProblemDetailsContent_WhenCancelPaymentCalled_ThenReturnsExpectedError(
        StripeExeCancelPaymentRequestDto cancelRequest, ProblemDetailsExtended mockedProblemDetails)
    {
        //Arrange
        mockedProblemDetails.Detail = "Something has happened!";
        var mockedResponse = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.UnprocessableContent,
            Content = new StringContent(JsonSerializer.Serialize(mockedProblemDetails), Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.CancelStripeExecutionAsync(cancelRequest))
            .ReturnsAsync(mockedResponse);

        //Act
        var result = await _client.CancelPaymentAsync(cancelRequest);

        //Assert
        var expectedError = new PaymentExecutionError(
            "Something has happened!",
            mockedProblemDetails.ProviderErrorCode,
            mockedResponse.StatusCode);
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(expectedError);
    }

    [Theory]
    [AutoData]
    public async Task GivenClientReturnsNonSuccessfulStatusCodeWithUnknownResponseContent_WhenCancelPaymentCalled_ThenReturnsExpectedError(
        StripeExeCancelPaymentRequestDto cancelRequest)
    {
        //Arrange
        var mockedStringContent = "Unknown content";
        var mockedResponse = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.UnprocessableContent,
            Content = new StringContent(mockedStringContent, Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.CancelStripeExecutionAsync(cancelRequest))
            .ReturnsAsync(mockedResponse);

        //Act
        var result = await _client.CancelPaymentAsync(cancelRequest);

        //Assert
        var expectedError = new PaymentExecutionError($"Failed Stripe Execution integration. {mockedStringContent}", mockedResponse.StatusCode);
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(expectedError);
    }

    [Theory, ContractsAutoData]
    public async Task GivenClientReturnsSuccessfulResponse_WhenGetPaymentIntentByPaymentRequestIdCalled_ThenReturnsPaymentIntentDtoResult(
        StripeExePaymentIntentDto mockedPaymentIntentDto,
        Guid paymentRequestId,
        Guid correlationId,
        Guid tenantId)
    {
        //Arrange
        var mockedResponseJson = JsonSerializer.Serialize(mockedPaymentIntentDto, _opts);
        var mockedResponse = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(mockedResponseJson, Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId))
            .ReturnsAsync(mockedResponse);

        //Act
        var result = await _client.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<StripeExePaymentIntentDto>();
        result.Value.Should().BeEquivalentTo(mockedPaymentIntentDto);
    }

    [Theory]
    [InlineAutoData(HttpStatusCode.BadRequest)]
    [InlineAutoData(HttpStatusCode.Unauthorized)]
    [InlineAutoData(HttpStatusCode.Forbidden)]
    [InlineAutoData(HttpStatusCode.InternalServerError)]
    [InlineAutoData(HttpStatusCode.ServiceUnavailable)]
    public async Task GivenStripeExecutionClientReturnsUnsuccessfulStatusCodeWithProviderErrorCodeAndDetail_WhenGetPaymentIntentByPaymentRequestIdCalled_ThenReturnsResultFailWithExpectedError(
        HttpStatusCode mockedUnsuccessfulStatusCode,
        Guid paymentRequestId,
        Guid correlationId,
        Guid tenantId)
    {
        //Arrange
        var mockedProviderErrorCode = "invalid_client_details";
        var mockedDetail = "Its all gone wrong";
        var expectedError = new PaymentExecutionError(
            mockedDetail,
            mockedProviderErrorCode,
            mockedUnsuccessfulStatusCode);
        var mockedResponseContent = new JsonObject()
        {
            ["providerErrorCode"] = mockedProviderErrorCode,
            ["detail"] = mockedDetail
        };
        var mockedHttpResponse = new HttpResponseMessage()
        {
            StatusCode = mockedUnsuccessfulStatusCode,
            Content = new StringContent(mockedResponseContent.ToJsonString(), Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId))
            .ReturnsAsync(mockedHttpResponse);

        //Act
        var result = await _client.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId);

        //Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Reasons.FirstOrDefault();
        error.Should().BeOfType<PaymentExecutionError>();
        error.Should().BeEquivalentTo(expectedError);
    }

    [Theory]
    [AutoData]
    public async Task
        GivenStripeExecutionClientReturnsUnsuccessfulStatusCodeWithNoProviderErrorCode_WhenGetPaymentIntentByPaymentRequestIdCalled_ThenReturnedErrorHasNullProviderCode(
        Guid paymentRequestId,
        Guid correlationId,
        Guid tenantId)
    {
        //Arrange
        var mockedUnsuccessfulStatusCode = HttpStatusCode.InternalServerError;
        var mockedResponseContent = new JsonObject()
        {
            ["detail"] = "some detail"
        };
        var mockedHttpResponse = new HttpResponseMessage()
        {
            StatusCode = mockedUnsuccessfulStatusCode,
            Content = new StringContent(mockedResponseContent.ToJsonString(), Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId))
            .ReturnsAsync(mockedHttpResponse);

        //Act
        var result = await _client.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId);

        //Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Reasons.FirstOrDefault();
        error.Should().BeOfType<PaymentExecutionError>();
        ((PaymentExecutionError)error!).GetProviderErrorCode().Should().BeNull();
    }

    [Theory]
    [InlineAutoData(HttpStatusCode.BadRequest)]
    [InlineAutoData(HttpStatusCode.Unauthorized)]
    [InlineAutoData(HttpStatusCode.Forbidden)]
    [InlineAutoData(HttpStatusCode.InternalServerError)]
    [InlineAutoData(HttpStatusCode.ServiceUnavailable)]
    public async Task GivenStripeExecutionClientReturnsUnsuccessfulStatusCodeWithUnexpectedContent_WhenGetPaymentIntentByPaymentRequestIdCalled_ThenReturnsResultFailWithGenericError(
        HttpStatusCode mockedUnsuccessfulStatusCode,
        Guid paymentRequestId,
        Guid correlationId,
        Guid tenantId)
    {
        //Arrange
        var mockedUnexpectedContent = "unexpected response form";
        var expectedErrorMessage = $"Failed Stripe Execution integration. {mockedUnexpectedContent}";
        var expectedError = new PaymentExecutionError(expectedErrorMessage, mockedUnsuccessfulStatusCode);
        var mockedHttpResponse = new HttpResponseMessage()
        {
            StatusCode = mockedUnsuccessfulStatusCode,
            Content = new StringContent(mockedUnexpectedContent, Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId))
            .ReturnsAsync(mockedHttpResponse);

        //Act
        var result = await _client.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId);

        //Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Reasons.FirstOrDefault();
        error.Should().BeOfType<PaymentExecutionError>();
        error.Should().BeEquivalentTo(expectedError);
    }

    [Theory]
    [AutoData]
    public async Task GivenStripeExecutionClientReturnsSuccessStatusCodeWithMalformedResponse_WhenGetPaymentIntentByPaymentRequestIdCalled_ThenDeserializationExceptionIsThrown(
        Guid paymentRequestId,
        Guid correlationId,
        Guid tenantId)
    {
        var mockedResponse = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("Unexpected response!", Encoding.UTF8, "application/json")
        };
        _mockStripeExecutionHttpClient.Setup(m => m.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId))
            .ReturnsAsync(mockedResponse);

        //Act
        var act = async () => await _client.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId);

        //Assert
        await act.Should().ThrowAsync<JsonException>();
    }
}
