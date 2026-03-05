using System.Text.Json;
using AutoFixture.Xunit2;
using AutoMapper;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;
using PaymentExecution.Domain.Service.Strategies;
using PaymentExecution.StripeExecutionClient.Contracts;
using PaymentExecution.StripeExecutionClient.Contracts.Models;

namespace PaymentExecution.Domain.UnitTests.Services;

public class StripeIntegrationDomainServiceTests
{
    private readonly Mock<IStripeExecutionClient> _stripeExecutionClientMock = new();
    private readonly Mock<ILogger<StripeIntegrationDomainService>> _loggerMock = new();
    private readonly Mock<IStripePendingActionStrategy> _bankTransferStrategyMock = new();
    private readonly Mock<IStripePendingActionStrategy> _redirectStrategyMock = new();
    private readonly Mock<IStripePendingActionStrategy> _microdepositStrategyMock = new();
    private readonly Mock<IStripePendingActionStrategy> _payToStrategyMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly StripeIntegrationDomainService _sut;

    public StripeIntegrationDomainServiceTests()
    {
        _bankTransferStrategyMock.Setup(x => x.ActionType).Returns("display_bank_transfer_instructions");
        _redirectStrategyMock.Setup(x => x.ActionType).Returns("redirect_to_url");
        _microdepositStrategyMock.Setup(x => x.ActionType).Returns("verify_with_microdeposits");
        _payToStrategyMock.Setup(x => x.ActionType).Returns("payto_await_authorization");

        var strategies = new[]
        {
            _bankTransferStrategyMock.Object,
            _redirectStrategyMock.Object,
            _microdepositStrategyMock.Object,
            _payToStrategyMock.Object
        };

        _sut = new StripeIntegrationDomainService(
            _stripeExecutionClientMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            strategies);
    }

    [Fact]
    public void GivenStripeIntegrationDomainService_WhenProviderType_ReturnsProviderTypeOfStripe()
    {
        // Act
        var result = _sut.ProviderType;

        // Assert
        result.Should().Be(ProviderType.Stripe);
    }

    [Fact]
    public async Task GivenGetToStripeExeReturnsResultFail_WhenGetProviderState_ThenPropagatesResult()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedError = Result.Fail("Some error");
        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(mockedError);

        // Act
        var result = await _sut.GetProviderStateAsync(paymentRequestId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Should().BeEquivalentTo(mockedError);
    }

    [Theory, ContractsAutoData]
    public async Task GivenUnexpectedStripeStatusInPaymentIntentDto_WhenGetProviderState_ThenReturnsResultFail(
        StripeExePaymentIntentDto dto)
    {
        // Arrange
        var paymentIntentId = Guid.NewGuid();
        dto.Status = "unexpected-status";
        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentIntentId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Ok(dto));

        // Act
        var result = await _sut.GetProviderStateAsync(paymentIntentId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be("The stripe status is not supported");
    }

    [Theory]
    [InlineContractsAutoData("requires_payment_method", PaymentProviderStatus.Submitted)]
    [InlineContractsAutoData("requires_confirmation", PaymentProviderStatus.Submitted)]
    [InlineContractsAutoData("requires_capture", PaymentProviderStatus.RequiresAction)]
    [InlineContractsAutoData("processing", PaymentProviderStatus.Processing)]
    [InlineContractsAutoData("requires_action", PaymentProviderStatus.RequiresAction)]
    [InlineContractsAutoData("succeeded", PaymentProviderStatus.Terminal)]
    [InlineContractsAutoData("canceled", PaymentProviderStatus.Terminal)]
    public async Task GivenStripeStatus_WhenGetProviderState_ThenStatusMappedToExpectedPaymentProviderStatus(
        string stripeStatus,
        PaymentProviderStatus expectedMappedStatus,
        StripeExePaymentIntentDto mockStripeExeDto)
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        mockStripeExeDto.Status = stripeStatus;
        mockStripeExeDto.NextAction = null;
        mockStripeExeDto.LastPaymentError = null;
        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Ok(mockStripeExeDto));

        // Act
        var result = await _sut.GetProviderStateAsync(paymentRequestId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentProviderStatus.Should().Be(expectedMappedStatus);
    }

    [Theory, ContractsAutoData]
    public async Task GivenPaymentIntentWithLastPaymentError_WhenGetProviderState_ThenStatusIsMappedAsTerminal(
        StripeExePaymentIntentDto mockStripeExeDto)
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        mockStripeExeDto.Status = "requires_payment_method";
        mockStripeExeDto.NextAction = null;
        mockStripeExeDto.LastPaymentError = new StripeExeLastPaymentErrorDto()
        {
            Code = "some_error_code"
        };
        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Ok(mockStripeExeDto));

        // Act
        var result = await _sut.GetProviderStateAsync(paymentRequestId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentProviderStatus.Should().Be(PaymentProviderStatus.Terminal);
    }

    [Theory, ContractsAutoData]
    public async Task GivenDtoWithLastPaymentError_WhenGetProviderState_ThenReturnsErrorCodeInProviderState(
        StripeExePaymentIntentDto mockStripeExeDtp)
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        mockStripeExeDtp.Status = "requires_payment_method";
        mockStripeExeDtp.NextAction = null;
        var expectedErrorCode = "card_declined";
        mockStripeExeDtp.LastPaymentError = new StripeExeLastPaymentErrorDto { Code = expectedErrorCode };
        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Ok(mockStripeExeDtp));

        // Act
        var result = await _sut.GetProviderStateAsync(paymentRequestId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LastPaymentErrorCode.Should().Be(expectedErrorCode);
    }

    [Theory, ContractsAutoData]
    public async Task GivenDtoNextActionIsNull_WhenGetProviderState_ThenReturnsProviderStateWithNullPendingStatusDetails(
        Guid paymentRequestId,
        StripeExePaymentIntentDto mockStripeExeDto)
    {
        // Arrange
        mockStripeExeDto.Status = "succeeded";
        mockStripeExeDto.NextAction = null;

        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Ok(mockStripeExeDto));

        // Act
        var result = await _sut.GetProviderStateAsync(paymentRequestId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PendingStatusDetails.Should().BeNull();
    }

    [Fact]
    public async Task GivenDtoHasNextActionWithUnsupportedPaymentMethodType_WhenGetProviderState_ThenReturnsResultFail()
    {
        // Arrange
        var testPaymentRequestId = Guid.NewGuid();
        var mockStripeExeDto = new StripeExePaymentIntentDto()
        {
            Status = "requires_action",
            Id = "pi_123456789",
            Amount = 1000,
            Currency = "usd",
            PaymentMethod = new StripeExePaymentMethodDto() { Type = "INVALID_TYPE" },
            LastPaymentError = null,
            NextAction = new StripeExeNextActionDto()
            {
                Type = "redirect_to_url",
                RedirectToUrl = new StripeExeRedirectToUrlDto { Url = "https://some-url.com" }
            }
        };

        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(testPaymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Ok(mockStripeExeDto));

        // Act
        var result = await _sut.GetProviderStateAsync(testPaymentRequestId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Payment method must be provided and of a supported type");
    }

    [Theory]
    [InlineData("card", PaymentMethodType.Card)]
    [InlineData("link", PaymentMethodType.Link)]
    [InlineData("customer_balance", PaymentMethodType.CustomerBalance)]
    [InlineData("afterpay_clearpay", PaymentMethodType.AfterPay)]
    [InlineData("au_becs_debit", PaymentMethodType.AuBecsDebit)]
    [InlineData("bacs_debit", PaymentMethodType.BacsDebit)]
    [InlineData("klarna", PaymentMethodType.Klarna)]
    [InlineData("us_bank_account", PaymentMethodType.UsBankAccount)]
    [InlineData("zip", PaymentMethodType.Zip)]
    [InlineData("payto", PaymentMethodType.PayTo)]
    [InlineData("pay_by_bank", PaymentMethodType.PayByBank)]
    [InlineData("apple_pay", PaymentMethodType.ApplePay)]
    [InlineData("google_pay", PaymentMethodType.GooglePay)]
    public async Task GivenDtoHasNextAction_WhenGetProviderState_ThenMapsPaymentMethodTypeAsExpected(
        string paymentMethodType, PaymentMethodType expectedPaymentMethodType)
    {
        // Arrange
        var testPaymentRequestId = Guid.NewGuid();
        var mockStripeExeDto = new StripeExePaymentIntentDto()
        {
            Status = "requires_action",
            Id = "pi_123456789",
            Amount = 1000,
            Currency = "usd",
            PaymentMethod = new StripeExePaymentMethodDto() { Type = paymentMethodType },
            LastPaymentError = null,
            NextAction = new StripeExeNextActionDto()
            {
                Type = "redirect_to_url",
                RedirectToUrl = new StripeExeRedirectToUrlDto { Url = "https://some-url.com" }
            }
        };

        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(testPaymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Ok(mockStripeExeDto));
        _redirectStrategyMock.Setup(m => m.Map(mockStripeExeDto, mockStripeExeDto.NextAction))
            .Returns(new PendingStatusDetails
            {
                RequiresActionType = RequiresActionType.RedirectToUrl,
                HasActionValue = true,
                RedirectToUrl = new RedirectToUrl()
                {
                    RedirectUrl = "https://some-url.com"
                }
            });

        // Act
        var result = await _sut.GetProviderStateAsync(testPaymentRequestId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PendingStatusDetails.Should().NotBeNull();
        result.Value.PendingStatusDetails.PaymentMethodType.Should().Be(expectedPaymentMethodType);
    }

    [Fact]
    public async Task GivenDtoHasNextActionWithNullPaymentMethod_WhenGetProviderState_ThenReturnsResultFail()
    {
        // Arrange
        var testPaymentRequestId = Guid.NewGuid();
        var mockStripeExeDto = new StripeExePaymentIntentDto()
        {
            Status = "requires_action",
            Id = "pi_123456789",
            Amount = 1000,
            Currency = "usd",
            PaymentMethod = null,
            LastPaymentError = null,
            NextAction = new StripeExeNextActionDto()
            {
                Type = "redirect_to_url",
                RedirectToUrl = new StripeExeRedirectToUrlDto { Url = "https://some-url.com" }
            }
        };

        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(testPaymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Ok(mockStripeExeDto));

        // Act
        var result = await _sut.GetProviderStateAsync(testPaymentRequestId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Payment method must be provided and of a supported type");
    }

    [Fact]
    public async Task GivenDtoHasUnsupportedNextActionType_WhenGetProviderState_ThenReturnsUnknownPendingStatusDetails()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedPaymentMethodType = PaymentMethodType.Klarna;
        var mockStripeExeDto = new StripeExePaymentIntentDto()
        {
            Status = "requires_action",
            Id = "pi_123456789",
            Amount = 1000,
            Currency = "usd",
            PaymentMethod = new StripeExePaymentMethodDto() { Type = mockedPaymentMethodType.ToString().ToLower() },
            LastPaymentError = null,
            NextAction = new StripeExeNextActionDto()
            {
                Type = "random-type-that-we-dont-handle",
                Misc = JsonSerializer.Deserialize<JsonElement>("{\"some-property\":\"some-value\"}")
            }
        };

        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Ok(mockStripeExeDto));

        // Act
        var result = await _sut.GetProviderStateAsync(paymentRequestId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var expectedProviderState = new ProviderState
        {
            PaymentProviderStatus = PaymentProviderStatus.RequiresAction,
            PaymentProviderPaymentTransactionId = mockStripeExeDto.Id,
            ProviderType = ProviderType.Stripe,
            LastPaymentErrorCode = null,
            PendingStatusDetails = new PendingStatusDetails
            {
                RequiresActionType = RequiresActionType.Unknown,
                PaymentMethodType = PaymentMethodType.Klarna,
                HasActionValue = false,
            }
        };
        result.Value.Should().NotBeNull().And.BeEquivalentTo(expectedProviderState);
    }

    [Theory, ContractsAutoData]
    public async Task GivenNextActionStrategyMapResultFail_WhenGetProviderState_ThenPropagatesFail(
        Guid paymentRequestId,
        StripeExePaymentIntentDto mockStripeExeDto)
    {
        // Arrange
        var mockedError = Result.Fail("The mapping to pending status details failed!");
        mockStripeExeDto.Status = "requires_action";
        mockStripeExeDto.PaymentMethod = new StripeExePaymentMethodDto() { Type = nameof(PaymentMethodType.Klarna).ToLower() };
        mockStripeExeDto.NextAction = new StripeExeNextActionDto()
        {
            Type = "display_bank_transfer_instructions"
        };

        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Ok(mockStripeExeDto));
        _bankTransferStrategyMock.Setup(x => x.Map(mockStripeExeDto, mockStripeExeDto.NextAction))
            .Returns(mockedError);

        // Act
        var result = await _sut.GetProviderStateAsync(paymentRequestId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(mockedError.Errors[0]);
    }

    [Fact]
    public async Task GivenDtoHasASupportedNextAction_WhenGetProviderState_ThenReturnsExpectedProviderState()
    {
        // Arrange
        var paymentRequestId = Guid.NewGuid();
        var mockedPaymentMethodType = PaymentMethodType.Klarna;
        var mockedPaymentIntentId = "pi_239857ybn2";
        var mockedRedirectUrl = "https://some-url.com/redirect-here";
        var mockStripeExeDto = new StripeExePaymentIntentDto()
        {
            Status = "requires_action",
            Id = mockedPaymentIntentId,
            Amount = 1000,
            Currency = "usd",
            PaymentMethod = new StripeExePaymentMethodDto() { Type = mockedPaymentMethodType.ToString().ToLower() },
            LastPaymentError = null,
            NextAction = new StripeExeNextActionDto()
            {
                Type = "redirect_to_url",
                RedirectToUrl = new StripeExeRedirectToUrlDto()
                {
                    Url = mockedRedirectUrl
                }
            }
        };
        var mockedRedirectMappingResult = new PendingStatusDetails
        {
            RequiresActionType = RequiresActionType.RedirectToUrl,
            PaymentMethodType = mockedPaymentMethodType,
            HasActionValue = true,
            RedirectToUrl = new RedirectToUrl()
            {
                RedirectUrl = mockedRedirectUrl
            }
        };

        _stripeExecutionClientMock.Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Result.Ok(mockStripeExeDto));
        _redirectStrategyMock.Setup(m => m.Map(mockStripeExeDto, mockStripeExeDto.NextAction))
            .Returns(mockedRedirectMappingResult);

        // Act
        var result = await _sut.GetProviderStateAsync(paymentRequestId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var expectedProviderState = new ProviderState
        {
            PaymentProviderStatus = PaymentProviderStatus.RequiresAction,
            PaymentProviderPaymentTransactionId = mockedPaymentIntentId,
            ProviderType = ProviderType.Stripe,
            LastPaymentErrorCode = null,
            PendingStatusDetails = mockedRedirectMappingResult
        };
        result.Value.Should().NotBeNull().And.BeEquivalentTo(expectedProviderState);
    }

    [Theory, AutoData]
    public async Task GivenMapperThrowsException_WhenCancelPaymentAsync_ThenPropagatesException(
        CancelPaymentRequest mockCancelRequest)
    {
        var mockedExceptionMessage = "something has happened!";
        _mapperMock.Setup(m => m.Map<StripeExeCancelPaymentRequestDto>(mockCancelRequest))
            .Throws(new Exception(mockedExceptionMessage));

        // Ac
        var act = async () => await _sut.CancelPaymentAsync(mockCancelRequest);

        //Assert
        await act.Should().ThrowAsync<Exception>().WithMessage(mockedExceptionMessage);
    }

    [Theory, AutoData]
    public async Task GivenStripeExecutionClientReturnsResultOk_WhenCancelPaymentAsync_ThenPropagatesResult(
        CancelPaymentRequest mockCancelRequest,
        StripeExeCancelPaymentRequestDto mockCancelRequestDto)
    {
        var mockedStripeExeResult = Result.Ok("It worked! yippee!").ToResult();
        _mapperMock.Setup(m => m.Map<StripeExeCancelPaymentRequestDto>(mockCancelRequest))
            .Returns(mockCancelRequestDto);
        _stripeExecutionClientMock.Setup(m => m.CancelPaymentAsync(mockCancelRequestDto))
            .ReturnsAsync(mockedStripeExeResult);

        // Act
        var result = await _sut.CancelPaymentAsync(mockCancelRequest);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Should().Be(mockedStripeExeResult);
    }

    [Theory, AutoData]
    public async Task GivenStripeExecutionClientReturnsResultFail_WhenCancelPaymentAsync_ThenPropagatesResult(
        CancelPaymentRequest mockCancelRequest,
        StripeExeCancelPaymentRequestDto mockCancelRequestDto)
    {
        var mockedFailMessage = "it did not work :c";
        var mockedStripeExeResult = Result.Fail(mockedFailMessage);
        _mapperMock.Setup(m => m.Map<StripeExeCancelPaymentRequestDto>(mockCancelRequest))
            .Returns(mockCancelRequestDto);
        _stripeExecutionClientMock.Setup(m => m.CancelPaymentAsync(mockCancelRequestDto))
            .ReturnsAsync(mockedStripeExeResult);

        // Act
        var result = await _sut.CancelPaymentAsync(mockCancelRequest);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Should().Be(mockedStripeExeResult);
        result.Errors[0].Message.Should().Be(mockedFailMessage);
    }

    [Theory, ContractsAutoData]
    public async Task GivenHeadersProvided_WhenGetProviderStateAsync_ThenPassesHeadersToStripeExecutionService(
        Guid paymentRequestId,
        Guid correlationId,
        Guid tenantId,
        StripeExePaymentIntentDto mockStripeExeDto)
    {
        // Arrange
        mockStripeExeDto.Status = "succeeded";
        mockStripeExeDto.NextAction = null;

        _stripeExecutionClientMock
            .Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId))
            .ReturnsAsync(Result.Ok(mockStripeExeDto));

        // Act
        var result = await _sut.GetProviderStateAsync(paymentRequestId, correlationId, tenantId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _stripeExecutionClientMock.Verify(
            x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, correlationId, tenantId),
            Times.Once);
    }

    [Theory, ContractsAutoData]
    public async Task GivenNoHeadersProvided_WhenGetProviderStateAsync_ThenCallsWithNullHeaders(
        Guid paymentRequestId,
        StripeExePaymentIntentDto mockStripeExeDto)
    {
        // Arrange
        mockStripeExeDto.Status = "succeeded";
        mockStripeExeDto.NextAction = null;

        _stripeExecutionClientMock
            .Setup(x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, null, null))
            .ReturnsAsync(Result.Ok(mockStripeExeDto));

        // Act - Call without optional parameters (API scenario)
        var result = await _sut.GetProviderStateAsync(paymentRequestId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _stripeExecutionClientMock.Verify(
            x => x.GetPaymentIntentByPaymentRequestIdAsync(paymentRequestId, null, null),
            Times.Once);
    }
}
