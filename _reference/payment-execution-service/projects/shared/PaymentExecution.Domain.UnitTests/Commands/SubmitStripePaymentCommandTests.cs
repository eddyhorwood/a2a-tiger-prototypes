using AutoFixture.Xunit2;
using FluentResults;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;
using PaymentExecution.FeatureFlagClient;

namespace PaymentExecution.Domain.UnitTests.Commands;

public class SubmitStripePaymentCommandTests
{
    private readonly SubmitStripePaymentCommandHandler _handler;

    private readonly Mock<ISubmitStripePaymentDomainService> _domainService;
    private readonly Mock<IFeatureFlagClient> _featureFlagClient;

    public SubmitStripePaymentCommandTests()
    {
        _domainService = new Mock<ISubmitStripePaymentDomainService>();
        _featureFlagClient = new Mock<IFeatureFlagClient>();
        _handler = new SubmitStripePaymentCommandHandler(_domainService.Object, _featureFlagClient.Object);
    }

    [Fact]
    public async Task GivenValidRequestAndFeatureFlagEnabled_WhenSubmitPaymentTransactionCommandHandler_ThenCallsBothUpdatePaymentTransactionAndSendCancelMessage()
    {
        //Arrange
        var mockPaymentRequestId = Guid.NewGuid();
        var mockOrganisationId = Guid.NewGuid();
        var mockPaymentIntentId = "pi_1234";
        var mockPaymentTransactionId = Guid.NewGuid();
        var mockProviderServiceId = Guid.NewGuid();
        var mockClientSecret = "client_secret_1234";
        var mockTtlInSeconds = 300;

        var mockDomainPaymentRequest = CreateMockDomainPaymentRequest(mockPaymentRequestId, mockOrganisationId);

        var submitStripePaymentCommand = new SubmitStripePaymentCommand()
        {
            PaymentRequestId = mockPaymentRequestId,
            PaymentMethodsMadeAvailable = ["card"],
            PaymentMethodId = mockPaymentIntentId,
            XeroCorrelationId = Guid.NewGuid().ToString(),
            XeroTenantId = Guid.NewGuid().ToString()
        };

        _domainService.Setup(s => s.SubmitToPaymentRequestAsync(submitStripePaymentCommand.PaymentRequestId))
            .ReturnsAsync(Result.Ok(mockDomainPaymentRequest));
        _domainService.Setup(s => s.CreatePaymentTransactionWithCompensationActionAsync(submitStripePaymentCommand.PaymentRequestId, mockDomainPaymentRequest.OrganisationId))
            .ReturnsAsync(Result.Ok(mockPaymentTransactionId));
        _domainService.Setup(s => s.SubmitRequestToStripeExecutionWithCompensationActionAsync(
                It.Is<PaymentRequest>(req => req.PaymentRequestId == mockDomainPaymentRequest.PaymentRequestId),
                submitStripePaymentCommand.PaymentMethodsMadeAvailable,
                submitStripePaymentCommand.PaymentMethodId))
            .ReturnsAsync(Result.Ok(new SubmittedPayment()
            {
                PaymentIntentId = mockPaymentIntentId,
                ClientSecret = mockClientSecret,
                ProviderServiceId = mockProviderServiceId,
                TtlInSeconds = mockTtlInSeconds
            }));
        _domainService.Setup(s => s.TryToUpdatePaymentTransactionWithProviderDetailsAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        _domainService.Setup(s => s.TryToSendMessageToCancelExecutionQueueAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _featureFlagClient
            .Setup(f => f.GetFeatureFlag(It.IsAny<FeatureFlagDefinition<bool>>(), null))
            .Returns(new FeatureFlag<bool> { Name = "test", Value = true });

        //Act
        var result = await _handler.Handle(submitStripePaymentCommand, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockPaymentIntentId, result.Value.PaymentIntentId);
        Assert.Equal(mockClientSecret, result.Value.ClientSecret);
        Assert.True(result.IsSuccess);

        // Verify both methods were called with correct parameters
        _domainService.Verify(s => s.TryToUpdatePaymentTransactionWithProviderDetailsAsync(
            mockPaymentIntentId,
            mockPaymentTransactionId,
            mockProviderServiceId,
            mockPaymentRequestId), Times.Once);

        _domainService.Verify(s => s.TryToSendMessageToCancelExecutionQueueAsync(
            mockPaymentRequestId,
            mockTtlInSeconds,
            submitStripePaymentCommand.XeroCorrelationId,
            submitStripePaymentCommand.XeroTenantId), Times.Once);
    }

    [Fact]
    public async Task GivenValidRequestAndFeatureFlagDisabled_WhenSubmitPaymentTransactionCommandHandler_ThenCallsTryToUpdatePaymentTransactionWithProviderDetailsAsync()
    {
        //Arrange
        var mockPaymentRequestId = Guid.NewGuid();
        var mockOrganisationId = Guid.NewGuid();
        var mockPaymentIntentId = "pi_1234";
        var mockPaymentTransactionId = Guid.NewGuid();
        var mockProviderServiceId = Guid.NewGuid();
        var mockClientSecret = "client_secret_1234";

        var mockDomainPaymentRequest = CreateMockDomainPaymentRequest(mockPaymentRequestId, mockOrganisationId);

        var submitStripePaymentCommand = new SubmitStripePaymentCommand()
        {
            PaymentRequestId = mockPaymentRequestId,
            PaymentMethodsMadeAvailable = ["card"],
            PaymentMethodId = mockPaymentIntentId,
            XeroCorrelationId = Guid.NewGuid().ToString(),
            XeroTenantId = Guid.NewGuid().ToString()
        };

        _domainService.Setup(s => s.SubmitToPaymentRequestAsync(submitStripePaymentCommand.PaymentRequestId))
            .ReturnsAsync(Result.Ok(mockDomainPaymentRequest));
        _domainService.Setup(s => s.CreatePaymentTransactionWithCompensationActionAsync(submitStripePaymentCommand.PaymentRequestId, mockDomainPaymentRequest.OrganisationId))
            .ReturnsAsync(Result.Ok(mockPaymentTransactionId));
        _domainService.Setup(s => s.SubmitRequestToStripeExecutionWithCompensationActionAsync(
                It.Is<PaymentRequest>(req => req.PaymentRequestId == mockDomainPaymentRequest.PaymentRequestId),
                submitStripePaymentCommand.PaymentMethodsMadeAvailable,
                submitStripePaymentCommand.PaymentMethodId))
            .ReturnsAsync(Result.Ok(new SubmittedPayment()
            {
                PaymentIntentId = mockPaymentIntentId,
                ClientSecret = mockClientSecret,
                ProviderServiceId = mockProviderServiceId
            }));
        _domainService.Setup(s => s.TryToUpdatePaymentTransactionWithProviderDetailsAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _featureFlagClient
            .Setup(f => f.GetFeatureFlag(It.IsAny<FeatureFlagDefinition<bool>>(), null))
            .Returns(new FeatureFlag<bool> { Name = "test", Value = false });

        //Act
        var result = await _handler.Handle(submitStripePaymentCommand, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockPaymentIntentId, result.Value.PaymentIntentId);
        Assert.Equal(mockClientSecret, result.Value.ClientSecret);
        Assert.True(result.IsSuccess);

        // Verify update payment transaction was called with correct parameters
        _domainService.Verify(s => s.TryToUpdatePaymentTransactionWithProviderDetailsAsync(
            mockPaymentIntentId,
            mockPaymentTransactionId,
            mockProviderServiceId,
            mockPaymentRequestId), Times.Once);

        // Verify send cancel message was NOT called
        _domainService.Verify(s => s.TryToSendMessageToCancelExecutionQueueAsync(
            It.IsAny<Guid>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task GivenCallToPaymentRequestFailed_WhenSubmitStripePaymentCommandHandler_ThenHandlerReturnFailedResult(SubmitStripePaymentCommand submitStripePaymentCommand)
    {
        //Arrange
        _domainService.Setup(s => s.SubmitToPaymentRequestAsync(submitStripePaymentCommand.PaymentRequestId))
            .ReturnsAsync(Result.Fail("Failed to submit payment request"));

        // Act
        var result = await _handler.Handle(submitStripePaymentCommand, CancellationToken.None);

        //Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Failed to submit payment request", result.Reasons.FirstOrDefault()?.Message);
    }

    [Theory]
    [AutoData]
    public async Task GivenCallToPaymentRequestThrownException_WhenSubmitStripePaymentCommandHandler_ThenExceptionPropagated(SubmitStripePaymentCommand submitStripePaymentCommand)
    {
        //Arrange
        _domainService.Setup(s => s.SubmitToPaymentRequestAsync(submitStripePaymentCommand.PaymentRequestId))
            .ThrowsAsync(new HttpRequestException());

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () => await _handler.Handle(submitStripePaymentCommand, CancellationToken.None));
    }

    [AutoData]
    [Theory]
    public async Task
        GivenFailedToInsertPaymentTransaction_WhenSubmitStripePaymentCommandHandler_ThenHandlerReturnFailedResult(SubmitStripePaymentCommand submitStripePaymentCommand)
    {
        var mockPaymentRequestId = submitStripePaymentCommand.PaymentRequestId;
        var mockOrganisationId = Guid.NewGuid();
        var mockDomainPaymentRequest = CreateMockDomainPaymentRequest(mockPaymentRequestId, mockOrganisationId);

        //Arrange
        _domainService.Setup(s => s.SubmitToPaymentRequestAsync(mockPaymentRequestId))
            .ReturnsAsync(Result.Ok(mockDomainPaymentRequest));

        _domainService.Setup(s => s.CreatePaymentTransactionWithCompensationActionAsync(mockPaymentRequestId, mockOrganisationId))
            .ReturnsAsync(Result.Fail("Failed to insert payment transaction"));

        // Act
        var result = await _handler.Handle(submitStripePaymentCommand, CancellationToken.None);

        //Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Failed to insert payment transaction", result.Reasons.FirstOrDefault()?.Message);
    }

    [AutoData]
    [Theory]
    public async Task GivenStripeExecutionClientFailedToSubmitPayment_WhenSubmitStripePaymentCommandHandler_ThenHandlerReturnFailedResult(
        SubmitStripePaymentCommand submitStripePaymentCommand)
    {
        var mockPaymentRequestId = submitStripePaymentCommand.PaymentRequestId;
        var mockOrganisationId = Guid.NewGuid();
        var mockDomainPaymentRequest = CreateMockDomainPaymentRequest(mockPaymentRequestId, mockOrganisationId);
        var mockPaymentTransactionId = Guid.NewGuid();

        //Arrange
        _domainService.Setup(s => s.SubmitToPaymentRequestAsync(mockPaymentRequestId))
            .ReturnsAsync(Result.Ok(mockDomainPaymentRequest));

        _domainService.Setup(s => s.CreatePaymentTransactionWithCompensationActionAsync(mockPaymentRequestId, mockOrganisationId))
            .ReturnsAsync(Result.Ok(mockPaymentTransactionId));

        _domainService.Setup(s => s.SubmitRequestToStripeExecutionWithCompensationActionAsync(
                It.Is<PaymentRequest>(req => req.PaymentRequestId == mockDomainPaymentRequest.PaymentRequestId),
                submitStripePaymentCommand.PaymentMethodsMadeAvailable,
                submitStripePaymentCommand.PaymentMethodId))
            .ReturnsAsync(Result.Fail("Failed to submit payment to Stripe"));


        // Act
        var result = await _handler.Handle(submitStripePaymentCommand, CancellationToken.None);

        //Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Failed to submit payment to Stripe", result.Reasons.FirstOrDefault()?.Message);
    }

    private static PaymentRequest CreateMockDomainPaymentRequest(Guid mockPaymentRequestId, Guid mockOrganisationId)
    {
        return new PaymentRequest()
        {
            PaymentRequestId = mockPaymentRequestId,
            OrganisationId = mockOrganisationId,
            PaymentDateUtc = DateTime.UtcNow,
            ContactId = new Guid("00000000-0000-0000-0000-000000000001"),
            Status = RequestStatus.awaitingexecution,
            BillingContactDetails = new BillingContactDetails
            {
                Email = "test@xero.com"
            },
            Amount = 1000,
            Currency = "USD",
            PaymentDescription = "Test Payment Request",
            SelectedPaymentMethod = new SelectedPaymentMethod
            {
                PaymentGatewayId = Guid.NewGuid(),
                PaymentMethodName = "card"
            },
            LineItems = new List<LineItem>(),
            SourceContext = new SourceContext()
            {
                Identifier = Guid.NewGuid(),
                Type = "TestSourceContext"
            },
            Executor = ExecutorType.paymentexecution,
            MerchantReference = "test"
        };
    }
}
