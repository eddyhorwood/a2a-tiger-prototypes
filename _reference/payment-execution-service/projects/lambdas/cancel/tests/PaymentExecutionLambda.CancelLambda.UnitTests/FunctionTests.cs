using System.Text.Json;
using Amazon.Lambda.SQSEvents;
using AutoFixture.Xunit2;
using AutoMapper;
using FluentAssertions;
using FluentResults;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.Commands;
using PaymentExecutionLambda.CancelLambda;
using PaymentExecutionLambda.CancelLambda.Mappings;
using PaymentExecutionLambda.CancelLambda.Models;

namespace PaymentExecutionLambda.CancelLambdaUnitTests;

public class FunctionTests
{
    private readonly Mock<ILogger<Function>> _mockLogger;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IMediator> _mockMediator;
    private readonly IMapper _mapper;

    public FunctionTests()
    {
        _mockLogger = new Mock<ILogger<Function>>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        Mock<IServiceScope> mockServiceScope = new();
        Mock<IServiceProvider> mockServiceProvider = new();
        _mockMediator = new Mock<IMediator>();

        _mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(mockServiceScope.Object);
        mockServiceScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockServiceProvider.Setup(x => x.GetService(typeof(IMediator))).Returns(_mockMediator.Object);
        mockServiceProvider.Setup(x => x.GetService(It.Is<Type>(t => t == typeof(IMediator))))
            .Returns(_mockMediator.Object);

        // Setup AutoMapper with the real mapping profile
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();
    }

    [Theory]
    [AutoData]
    public async Task GivenSingleValidMessage_WhenHandlerCalled_ThenProcessesMessageSuccessfully(
        Guid tenantId,
        Guid paymentRequestId)
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var sqsEvent = CreateSqsEventWithTenantId(tenantId, paymentRequestId);
        _mockMediator.Setup(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().BeEmpty();
        _mockMediator.Verify(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default), Times.Once);

        // Verify that we got the expected command data
        _mockMediator.Invocations.Should().HaveCount(1);
        var sentCommand = (ProcessCancelMessageCommand)_mockMediator.Invocations[0].Arguments[0];
        sentCommand.PaymentRequestId.Should().Be(paymentRequestId);
        sentCommand.ProviderType.Should().Be("Stripe");
        sentCommand.CancellationReason.Should().Be("abandoned");
    }

    [Fact]
    public async Task GivenMessageWithValidTenantId_WhenHandlerCalled_ThenUsesParsedTenantId()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var tenantId = Guid.NewGuid();
        var sqsEvent = CreateSqsEventWithTenantId(tenantId, Guid.NewGuid());
        _mockMediator.Setup(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().BeEmpty();
        _mockMediator.Verify(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default), Times.Once);

        // Verify that we actually got the right tenant ID
        _mockMediator.Invocations.Should().HaveCount(1);
        var sentCommand = (ProcessCancelMessageCommand)_mockMediator.Invocations[0].Arguments[0];
        sentCommand.TenantId.Should().Be(tenantId);
        sentCommand.CorrelationId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GivenMessageWithCorrelationId_WhenHandlerCalled_ThenCorrelationIdIsPassedToCommand()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var tenantId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var paymentRequestId = Guid.NewGuid();
        var sqsEvent = CreateSqsEventWithTenantIdAndCorrelationId(tenantId, paymentRequestId, correlationId);
        _mockMediator.Setup(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().BeEmpty();

        _mockMediator.Invocations.Should().HaveCount(1);
        var sentCommand = (ProcessCancelMessageCommand)_mockMediator.Invocations[0].Arguments[0];
        sentCommand.TenantId.Should().Be(tenantId);
        sentCommand.CorrelationId.Should().Be(correlationId);
        sentCommand.PaymentRequestId.Should().Be(paymentRequestId);
    }

    [Fact]
    public async Task GivenMessageWithoutCorrelationId_WhenHandlerCalled_ThenNewCorrelationIdIsGenerated()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var tenantId = Guid.NewGuid();
        var sqsEvent = CreateSqsEventWithTenantId(tenantId, Guid.NewGuid()); // No correlation ID
        _mockMediator.Setup(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().BeEmpty();

        _mockMediator.Invocations.Should().HaveCount(1);
        var sentCommand = (ProcessCancelMessageCommand)_mockMediator.Invocations[0].Arguments[0];
        sentCommand.CorrelationId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GivenMessageWithInvalidCorrelationId_WhenHandlerCalled_ThenNewCorrelationIdIsGenerated()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var tenantId = Guid.NewGuid();
        var sqsEvent = CreateSqsEventWithInvalidCorrelationId(tenantId, Guid.NewGuid());
        _mockMediator.Setup(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().BeEmpty();

        _mockMediator.Invocations.Should().HaveCount(1);
        var sentCommand = (ProcessCancelMessageCommand)_mockMediator.Invocations[0].Arguments[0];
        sentCommand.CorrelationId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GivenInvalidJsonMessage_WhenHandlerCalled_ThenLogsErrorAndRemovesFromQueue()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var sqsEvent = new SQSEvent
        {
            Records =
            [
                new SQSEvent.SQSMessage
                {
                    MessageId = "test-message-id",
                    Body = "invalid-json",
                    MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
                    {
                        ["Xero-Tenant-Id"] =
                            new() { StringValue = Guid.NewGuid().ToString() }
                    }
                }
            ]
        };

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().BeEmpty();
        _mockMediator.Verify(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default), Times.Never);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to deserialize cancel message") && v.ToString()!.Contains("will be deleted as it is not retryable")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenInvalidTenantIdGuid_WhenHandlerCalled_ThenLogsErrorAndRemovesFromQueue()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var cancelRequest = new CancelPaymentRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            ProviderType = "Stripe",
            CancellationReason = "abandoned"
        };

        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "test-message-id",
                    Body = JsonSerializer.Serialize(cancelRequest),
                    MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
                    {
                        ["Xero-Tenant-Id"] = new() { StringValue = "invalid-guid-format" }
                    }
                }
            }
        };

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().BeEmpty();
        _mockMediator.Verify(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default), Times.Never);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid or missing tenant ID") && v.ToString()!.Contains("will be deleted as it is not retryable")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenMissingTenantId_WhenHandlerCalled_ThenLogsErrorAndRemovesFromQueue()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var cancelRequest = new CancelPaymentRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            ProviderType = "Stripe",
            CancellationReason = "abandoned"
        };

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                new SQSEvent.SQSMessage
                {
                    MessageId = "test-message-id",
                    Body = JsonSerializer.Serialize(cancelRequest),
                    MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>()
                }
            ]
        };

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().BeEmpty();
        _mockMediator.Verify(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default), Times.Never);

        // Verify that an error was logged for missing tenant ID
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid or missing tenant ID") && v.ToString()!.Contains("will be deleted as it is not retryable")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenMediatorReturnsTransientError_WhenHandlerCalled_ThenReturnsFailureInBatchItemFailures()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var sqsEvent = CreateSqsEventWithTenantId(Guid.NewGuid(), Guid.NewGuid());
        var transientError = new PaymentExecution.Common.PaymentExecutionError(
            "Service temporarily unavailable",
            PaymentExecution.Common.ErrorType.DependencyTransientError,
            "execution_error");
        var failureResult = Result.Fail(transientError);
        _mockMediator.Setup(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default))
            .ReturnsAsync(failureResult);

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().HaveCount(1);
        result.BatchItemFailures[0].ItemIdentifier.Should().Be("test-message-id");

        // Verify Error level log with retry message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("will be retried and sent to DLQ if max retries exceeded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenMediatorReturnsNonRetryableError_WhenHandlerCalled_ThenMessageDeletedNotRetried()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var sqsEvent = CreateSqsEventWithTenantId(Guid.NewGuid(), Guid.NewGuid());
        var nonRetryableError = new PaymentExecutionError(
            "Payment transaction not found",
            ErrorType.PaymentTransactionNotFound,
            "execution_not_found");
        var failureResult = Result.Fail(nonRetryableError);
        _mockMediator.Setup(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default))
            .ReturnsAsync(failureResult);

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().BeEmpty(); // Not retried - deleted from queue

        // Verify Warning level log with delete message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("will be deleted from queue as it is not retryable")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenMultipleMessages_WithMixedResults_ThenOnlyProcessingFailuresInBatchItemFailures()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var tenantId = Guid.NewGuid();

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                CreateValidMessage("message-1", tenantId, Guid.NewGuid()),
                CreateInvalidTenantMessage("message-2"),
                CreateValidMessage("message-3", tenantId, Guid.NewGuid())
            ]
        };

        var transientError = new PaymentExecutionError(
            "Transient failure",
            ErrorType.DependencyTransientError,
            "execution_error");

        _mockMediator.SetupSequence(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default))
            .ReturnsAsync(Result.Ok())
            .ReturnsAsync(Result.Fail(transientError));

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().HaveCount(1);
        result.BatchItemFailures[0].ItemIdentifier.Should().Be("message-3");

        _mockMediator.Verify(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default), Times.Exactly(2));
    }

    [Fact]
    public async Task GivenMultipleValidationFailures_WhenHandlerCalled_ThenAllRemovedFromQueue()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                CreateInvalidTenantMessage("message-1"),
                CreateInvalidJsonMessage("message-2"),
                CreateInvalidTenantMessage("message-3")
            ]
        };

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().BeEmpty();
        _mockMediator.Verify(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default), Times.Never);
    }

    [Fact]
    public async Task GivenMultipleMessages_WhenProcessedInParallel_ThenAllMessagesAreHandledCorrectly()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var tenantId = Guid.NewGuid();

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                CreateValidMessage("message-1", tenantId, Guid.NewGuid()),
                CreateValidMessage("message-2", tenantId, Guid.NewGuid()),
                CreateValidMessage("message-3", tenantId, Guid.NewGuid()),
                CreateValidMessage("message-4", tenantId, Guid.NewGuid()),
                CreateValidMessage("message-5", tenantId, Guid.NewGuid())
            ]
        };

        _mockMediator.Setup(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().BeEmpty();
        _mockMediator.Verify(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default), Times.Exactly(5));
    }

    [Fact]
    public async Task GivenMultipleMessages_WithMultipleFailures_ThenAllFailuresInBatchItemFailures()
    {
        // Arrange
        var function = new Function(_mockLogger.Object, _mockServiceScopeFactory.Object, _mapper);
        var tenantId = Guid.NewGuid();

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                CreateValidMessage("message-1", tenantId, Guid.NewGuid()),
                CreateValidMessage("message-2", tenantId, Guid.NewGuid()),
                CreateValidMessage("message-3", tenantId, Guid.NewGuid()),
                CreateValidMessage("message-4", tenantId, Guid.NewGuid()),
                CreateValidMessage("message-5", tenantId, Guid.NewGuid())
            ]
        };

        var transientError = new PaymentExecutionError(
            "Transient failure",
            ErrorType.DependencyTransientError,
            "execution_error");

        var callCount = 0;
        _mockMediator.Setup(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default))
            .ReturnsAsync(() =>
            {
                var currentCount = Interlocked.Increment(ref callCount);
                return currentCount == 2 || currentCount == 3 || currentCount == 5
                    ? Result.Fail(transientError)
                    : Result.Ok();
            });

        // Act
        var result = await function.Handler(sqsEvent);

        // Assert
        result.Should().NotBeNull();
        result.BatchItemFailures.Should().HaveCount(3);
        _mockMediator.Verify(x => x.Send(It.IsAny<ProcessCancelMessageCommand>(), default), Times.Exactly(5));
    }

    private static SQSEvent CreateSqsEventWithTenantId(Guid tenantId, Guid paymentRequestId)
    {
        var cancelRequest = new CancelPaymentRequest
        {
            PaymentRequestId = paymentRequestId,
            ProviderType = "Stripe",
            CancellationReason = "abandoned"
        };

        var messageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
        {
            ["Xero-Tenant-Id"] = new SQSEvent.MessageAttribute { StringValue = tenantId.ToString() }
        };

        return new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "test-message-id",
                    Body = JsonSerializer.Serialize(cancelRequest),
                    MessageAttributes = messageAttributes
                }
            }
        };
    }

    private static SQSEvent CreateSqsEventWithTenantIdAndCorrelationId(Guid tenantId, Guid paymentRequestId, Guid correlationId)
    {
        var cancelRequest = new CancelPaymentRequest
        {
            PaymentRequestId = paymentRequestId,
            ProviderType = "Stripe",
            CancellationReason = "abandoned"
        };

        var messageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
        {
            ["Xero-Tenant-Id"] = new SQSEvent.MessageAttribute { StringValue = tenantId.ToString() },
            ["Xero-Correlation-Id"] = new SQSEvent.MessageAttribute { StringValue = correlationId.ToString() }
        };

        return new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "test-message-id",
                    Body = JsonSerializer.Serialize(cancelRequest),
                    MessageAttributes = messageAttributes
                }
            }
        };
    }

    private static SQSEvent CreateSqsEventWithInvalidCorrelationId(Guid tenantId, Guid paymentRequestId)
    {
        var cancelRequest = new CancelPaymentRequest
        {
            PaymentRequestId = paymentRequestId,
            ProviderType = "Stripe",
            CancellationReason = "abandoned"
        };

        var messageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
        {
            ["Xero-Tenant-Id"] = new SQSEvent.MessageAttribute { StringValue = tenantId.ToString() },
            ["Xero-Correlation-Id"] = new SQSEvent.MessageAttribute { StringValue = "not-a-valid-guid" }
        };

        return new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "test-message-id",
                    Body = JsonSerializer.Serialize(cancelRequest),
                    MessageAttributes = messageAttributes
                }
            }
        };
    }

    private static SQSEvent.SQSMessage CreateValidMessage(string messageId, Guid tenantId, Guid paymentRequestId)
    {
        var cancelRequest = new CancelPaymentRequest
        {
            PaymentRequestId = paymentRequestId,
            ProviderType = "Stripe",
            CancellationReason = "abandoned"
        };

        return new SQSEvent.SQSMessage
        {
            MessageId = messageId,
            Body = JsonSerializer.Serialize(cancelRequest),
            MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
            {
                ["Xero-Tenant-Id"] = new SQSEvent.MessageAttribute { StringValue = tenantId.ToString() }
            }
        };
    }

    private static SQSEvent.SQSMessage CreateInvalidTenantMessage(string messageId)
    {
        var cancelRequest = new CancelPaymentRequest
        {
            PaymentRequestId = Guid.NewGuid(),
            ProviderType = "Stripe",
            CancellationReason = "abandoned"
        };

        return new SQSEvent.SQSMessage
        {
            MessageId = messageId,
            Body = JsonSerializer.Serialize(cancelRequest),
            MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
            {
                ["Xero-Tenant-Id"] = new SQSEvent.MessageAttribute { StringValue = "invalid-guid" }
            }
        };
    }

    private static SQSEvent.SQSMessage CreateInvalidJsonMessage(string messageId)
    {
        return new SQSEvent.SQSMessage
        {
            MessageId = messageId,
            Body = "invalid-json-content",
            MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
            {
                ["Xero-Tenant-Id"] = new SQSEvent.MessageAttribute { StringValue = Guid.NewGuid().ToString() }
            }
        };
    }
}
