using System.Net;
using Amazon.SQS.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.NewRelicClient;
using PaymentExecution.SqsIntegrationClient.Client;
using PaymentExecution.SqsIntegrationClient.Options;
using PaymentExecution.SqsIntegrationClient.Service;
using Exception = System.Exception;

namespace PaymentExecution.SqsIntegrationClient.UnitTests.Service;

public class ExecutionQueueServiceTests
{
    private readonly ExecutionQueueService _sut;
    private readonly Mock<ISqsClient> _sqsClient;
    private readonly Mock<IMonitoringClient> _mockMonitoringClient;

    public ExecutionQueueServiceTests()
    {
        _sqsClient = new Mock<ISqsClient>();
        _mockMonitoringClient = new Mock<IMonitoringClient>();
        Mock<ILogger<ExecutionQueueService>> mockLogger = new();
        var sqsOptions = Microsoft.Extensions.Options.Options.Create(
            new ExecutionQueueOptions { QueueUrl = "test-queue", MaxNumberOfMessages = 10, LongPollingTimeoutSeconds = 20 });
        _sut = new ExecutionQueueService(sqsOptions, mockLogger.Object, _sqsClient.Object, _mockMonitoringClient.Object);
    }

    [Fact]
    public async Task GivenSqsSuccess_WhenSendMessageAsync_ReturnsMessageResponse()
    {
        StringValues expectedCorrelationId = "dff8abf3-fdb0-41d7-9332-3d3e20b9bbb2";
        StringValues expectedTenantId = "dff8abf3-fdb0-41d7-9332-3d3e20b9bbb5";
        var executionMessage = CreateValidExecutionMessage();

        _sqsClient.Setup(m => m.SendMessageAsync(It.IsAny<SendMessageRequest>()))
            .ReturnsAsync(new SendMessageResponse { MessageId = "123" });

        await _sut.SendMessageAsync(executionMessage, expectedCorrelationId, expectedTenantId);

        _sqsClient.Verify(m => m.SendMessageAsync(It.IsAny<SendMessageRequest>()), Times.Once());
    }

    [Fact]
    public async Task GivenSqsSuccess_WhenSendMessageAsync_ThenAddsAppropriateDistributedTracingHeaders()
    {
        StringValues expectedCorrelationId = "dff8abf3-fdb0-41d7-9332-3d3e20b9bbb2";
        StringValues expectedTenantId = "dff8abf3-fdb0-41d7-9332-3d3e20b9bbb5";
        var executionMessage = CreateValidExecutionMessage();

        _sqsClient.Setup(m => m.SendMessageAsync(It.IsAny<SendMessageRequest>()))
            .ReturnsAsync(new SendMessageResponse { MessageId = "123" });

        await _sut.SendMessageAsync(executionMessage, expectedCorrelationId, expectedTenantId);

        _mockMonitoringClient.Verify(
            m => m.InsertDistributedTraceHeaders(
                It.Is<SendMessageRequest>(req =>
                    req.MessageAttributes[ExecutionConstants.XeroCorrelationId].StringValue == expectedCorrelationId),
                It.IsAny<Action<SendMessageRequest, string, string>>()), Times.Once());
    }

    [Fact]
    public async Task SendMessageAsync_ThrowsException_OnErrorSendingToQueue()
    {
        var expectedCorrelationId = "dff8abf3-fdb0-41d7-9332-3d3e20b9bbb2";
        StringValues expectedTenantId = "dff8abf3-fdb0-41d7-9332-3d3e20b9bbb5";

        _sqsClient.Setup(m => m.SendMessageAsync(It.IsAny<SendMessageRequest>()))
            .ThrowsAsync(new Exception("Error sending message to queue"));

        var executionMessage = CreateValidExecutionMessage();

        await Assert.ThrowsAsync<Exception>(async () =>
            await _sut.SendMessageAsync(executionMessage, expectedCorrelationId, expectedTenantId));
    }

    [Fact]
    public async Task GivenSqsClientSuccess_WhenGetMessageAsync_ThenReturnsMessageResponse()
    {
        _sqsClient.Setup(m => m.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = [] });

        var result = await _sut.GetMessagesAsync(It.IsAny<CancellationToken>());

        _sqsClient.Verify(m => m.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Once());
        Assert.IsType<ReceiveMessageResponse>(result);
    }

    [Fact]
    public async Task
        GivenSqsClientSuccess_WhenGetMessageAsync_ThenHasSentTheReceiveMessageRequestWithAppropriateMessageAttributeNamesRequested()
    {
        var expectedReceiveMessageAttributeNames = new List<string>
        {
            "Xero-Correlation-Id",
            "Xero-Tenant-Id",
            "traceparent",
            "NewRelic",
            "NEWRELIC",
            "newrelic",
            "X-NewRelic-Synthetics"
        };
        _sqsClient.Setup(m => m.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = [] });

        await _sut.GetMessagesAsync(It.IsAny<CancellationToken>());

        _sqsClient.Verify(
            m => m.ReceiveMessageAsync(
                It.Is<ReceiveMessageRequest>(req =>
                    req.MessageAttributeNames.SequenceEqual(expectedReceiveMessageAttributeNames)),
                It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GivenSqsClientThrowsError_WhenGetMessageAsync_ThenErrorIsThrown()
    {
        _sqsClient.Setup(m => m.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("oopsie :c"));

        await Assert.ThrowsAsync<Exception>(async () => await _sut.GetMessagesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GivenSqsClientSuccess_WhenDeleteMessagesAsync_ThenReturnsDeleteMessageResponse()
    {
        var mockMessageId = "message-1";
        _sqsClient.Setup(m =>
                m.DeleteMessageBatchAsync(It.IsAny<DeleteMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageBatchResponse { HttpStatusCode = HttpStatusCode.OK });

        var result = await _sut.DeleteMessagesAsync([
                new Message
                {
                    MessageId = mockMessageId
                }
            ],
            It.IsAny<CancellationToken>());

        _sqsClient.Verify(
            m => m.DeleteMessageBatchAsync(It.IsAny<DeleteMessageBatchRequest>(), It.IsAny<CancellationToken>()),
            Times.Once());
        Assert.IsType<DeleteMessageBatchResponse>(result);
        Assert.Equal(HttpStatusCode.OK, result.HttpStatusCode);
    }

    [Fact]
    public async Task GivenSqsClientThrowsError_WhenDeleteMessagesAsync_ThenErrorIsThrown()
    {
        _sqsClient.Setup(m =>
                m.DeleteMessageBatchAsync(It.IsAny<DeleteMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("oopsie :c"));

        await Assert.ThrowsAsync<Exception>(async () =>
            await _sut.DeleteMessagesAsync([
                    new Message
                    {
                        MessageId = "message-1"
                    }
                ],
                CancellationToken.None));
    }

    [Fact]
    public void GivenMessageHasAMatchingValidAttribute_WhenAcceptNewRelicDistributedTraceData_ThenReturnsData()
    {
        // Arrange
        var message = new Message
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "traceparent", new MessageAttributeValue { StringValue = "trace-data" } }
            }
        };

        // Act
        var result = _sut.AcceptNewRelicDistributedTraceData(message, "traceparent").ToList();

        // Assert
        result.Count.Should().Be(1);
        result.First().Should().Be("trace-data");
    }

    [Fact]
    public void GivenMessageAttributesAreNull_WhenAcceptNewRelicDistributedTraceData_ThenReturnsEmpty()
    {
        // Arrange
        var message = new Message { MessageAttributes = null };

        // Act
        var result = _sut.AcceptNewRelicDistributedTraceData(message, "traceparent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GivenKeyDoesNotExist_WhenAcceptNewRelicDistributedTraceData_ThenReturnsEmpty()
    {
        // Arrange
        var message = new Message
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "other-key", new MessageAttributeValue { StringValue = "other-data" } }
            }
        };

        // Act
        var result = _sut.AcceptNewRelicDistributedTraceData(message, "traceparent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GivenKeyValueIsNull_WhenAcceptNewRelicDistributedTraceData_ThenReturnsEmpty()
    {
        // Arrange
        var message = new Message
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue?> { { "traceparent", null } }
        };

        // Act
        var result = _sut.AcceptNewRelicDistributedTraceData(message, "traceparent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GivenKeyValueIsNull_WhenSetNewRelicDistributedTraceData_ThenAddsDataToTheMessageAttributes()
    {
        // Arrange
        var messageRequest = new SendMessageRequest("blah-queue", "some-body")
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue?>()
        };

        // Act
        _sut.SetNewRelicDistributedTraceData(messageRequest, "traceparent", "test");

        // Assert
        messageRequest.MessageAttributes.TryGetValue("traceparent", out var attributeValue);
        attributeValue!.StringValue.Should().Be("test");
    }

    [Fact]
    public void GivenKeyValueIsPresent_WhenSetNewRelicDistributedTraceData_ThenSkipsValueAndDoesNotThrowException()
    {
        // Arrange
        var messageRequest = new SendMessageRequest("blah-queue", "some-body")
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue?>()
            {
                { "traceparent", new MessageAttributeValue { StringValue = "existing-value" } }
            }
        };

        // Act
        _sut.SetNewRelicDistributedTraceData(messageRequest, "traceparent", "test");

        // Assert
        messageRequest.MessageAttributes.TryGetValue("traceparent", out var attributeValue);
        attributeValue!.StringValue.Should().Be("existing-value");
    }

    private static ExecutionQueueMessage CreateValidExecutionMessage()
    {
        return new ExecutionQueueMessage()
        {
            PaymentRequestId = Guid.NewGuid(),
            ProviderServiceId = Guid.NewGuid(),
            Fee = 5,
            FeeCurrency = "AUD",
            ProviderType = nameof(ProviderType.Stripe),
            PaymentProviderPaymentTransactionId = "pi_123456",
            PaymentProviderPaymentReferenceId = "ch_123456",
            Status = nameof(TerminalStatus.Succeeded)
        };
    }
}
