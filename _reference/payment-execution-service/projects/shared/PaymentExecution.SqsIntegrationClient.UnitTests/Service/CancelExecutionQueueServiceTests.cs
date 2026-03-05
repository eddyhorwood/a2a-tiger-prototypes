using System.Text.Json;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.SqsIntegrationClient.Client;
using PaymentExecution.SqsIntegrationClient.Options;
using PaymentExecution.SqsIntegrationClient.Service;
using Exception = System.Exception;

namespace PaymentExecution.SqsIntegrationClient.UnitTests.Service;

public class CancelExecutionQueueServiceTests
{
    private readonly CancelExecutionQueueService _sut;
    private readonly Mock<ISqsClient> _sqsClient;
    private readonly Mock<ILogger<CancelExecutionQueueService>> _logger;

    public CancelExecutionQueueServiceTests()
    {
        _sqsClient = new Mock<ISqsClient>();
        _logger = new Mock<ILogger<CancelExecutionQueueService>>();
        var sqsOptions = Microsoft.Extensions.Options.Options.Create(
            new CancelExecutionQueueOptions { QueueUrl = "test-cancel-queue" });
        _sut = new CancelExecutionQueueService(sqsOptions, _sqsClient.Object, _logger.Object);
    }

    [Fact]
    public async Task GivenValidMessage_WhenSendMessageAsync_ThenReturnsSuccessResult()
    {
        // Arrange
        var expectedCorrelationId = Guid.NewGuid().ToString();
        var expectedTenantId = Guid.NewGuid().ToString();
        var expectedQueueUrl = "test-cancel-queue";
        var delaySeconds = 60;
        var executionMessage = CreateValidCancelExecutionMessage();
        var expectedMessageBody = JsonSerializer.Serialize(executionMessage);

        _sqsClient.Setup(m => m.SendMessageAsync(It.IsAny<SendMessageRequest>()))
            .ReturnsAsync(new SendMessageResponse { MessageId = "123" });

        // Act
        var result = await _sut.SendMessageAsync(executionMessage, delaySeconds, expectedCorrelationId, expectedTenantId);

        // Assert
        Assert.True(result.IsSuccess);
        _sqsClient.Verify(
            m => m.SendMessageAsync(
                It.Is<SendMessageRequest>(req =>
                    req.QueueUrl == expectedQueueUrl &&
                    req.DelaySeconds == delaySeconds &&
                    req.MessageBody == expectedMessageBody &&
                    req.MessageAttributes.Count == 2 &&
                    req.MessageAttributes.ContainsKey(ExecutionConstants.XeroCorrelationId) &&
                    req.MessageAttributes[ExecutionConstants.XeroCorrelationId].StringValue == expectedCorrelationId &&
                    req.MessageAttributes[ExecutionConstants.XeroCorrelationId].DataType == "String" &&
                    req.MessageAttributes.ContainsKey(ExecutionConstants.XeroTenantId) &&
                    req.MessageAttributes[ExecutionConstants.XeroTenantId].StringValue == expectedTenantId &&
                    req.MessageAttributes[ExecutionConstants.XeroTenantId].DataType == "String")),
            Times.Once());
    }

    [Fact]
    public async Task GivenSqsClientThrowsException_WhenSendMessageAsync_ThenReturnsFailedResult()
    {
        // Arrange
        var expectedCorrelationId = Guid.NewGuid().ToString();
        var expectedTenantId = Guid.NewGuid().ToString();
        var delaySeconds = 60;

        _sqsClient.Setup(m => m.SendMessageAsync(It.IsAny<SendMessageRequest>()))
            .ThrowsAsync(new Exception("Error sending message to queue"));

        var executionMessage = CreateValidCancelExecutionMessage();

        // Act
        var result = await _sut.SendMessageAsync(executionMessage, delaySeconds, expectedCorrelationId, expectedTenantId);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Single(result.Errors);
        Assert.IsType<PaymentExecutionError>(result.Errors.First());
        Assert.Equal(ErrorConstants.ErrorMessage.SendMessageToCancelExecutionQueueError, result.Errors.First().Message);
    }

    private static CancelExecutionQueueMessage CreateValidCancelExecutionMessage()
    {
        return new CancelExecutionQueueMessage
        {
            PaymentRequestId = Guid.NewGuid(),
            CancellationReason = CancellationReason.Abandoned,
            ProviderType = ProviderType.Stripe
        };
    }
}
