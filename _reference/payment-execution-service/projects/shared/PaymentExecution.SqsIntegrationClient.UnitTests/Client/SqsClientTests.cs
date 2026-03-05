using System.Net;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using AutoFixture;
using Moq;
using PaymentExecution.Common;
using PaymentExecution.Domain.Models;
using PaymentExecution.SqsIntegrationClient.Client;
using CancellationToken = System.Threading.CancellationToken;

namespace PaymentExecution.SqsIntegrationClient.UnitTests.Client;

public class SqsClientTests
{
    private readonly Mock<IAmazonSQS> _mockSqsClient;
    private readonly SqsClient _sut;
    private readonly IFixture _fixture;

    public SqsClientTests()
    {
        _mockSqsClient = new Mock<IAmazonSQS>();
        _sut = new SqsClient(_mockSqsClient.Object);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task GivenSqsSuccess_WhenSendMessageAsync_ReturnsResponse()
    {
        var messageRequest = CreateValidSendMessageRequest();
        var response = new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK };
        _mockSqsClient.Setup(m => m.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.SendMessageAsync(messageRequest);

        Assert.Equal(HttpStatusCode.OK, result.HttpStatusCode);
    }

    [Fact]
    public async Task GivenSqsFailure_WhenSendMessageAsync_ThenThrowsException()
    {
        var messageRequest = CreateValidSendMessageRequest();
        _mockSqsClient.Setup(m => m.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        await Assert.ThrowsAsync<Exception>(() => _sut.SendMessageAsync(messageRequest));
    }

    [Fact]
    public async Task GivenSqsSuccess_WhenReceiveMessageAsync_ThenReturnsExpectedResponse()
    {
        //Arrange
        var receiveMessageRequest = CreateSqsRequest<ReceiveMessageRequest>();
        var expectedXeroCorrelationId = Guid.NewGuid().ToString();

        var expectedMessageBody = _fixture.Create<ExecutionQueueMessage>();

        var mockedReceiveMessageResponse = new ReceiveMessageResponse()
        {
            HttpStatusCode = HttpStatusCode.OK,
            Messages =
            [
                new Message
                {
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        {
                            ExecutionConstants.XeroCorrelationId, new MessageAttributeValue
                            {
                                DataType = "String", StringValue = expectedXeroCorrelationId
                            }
                        },
                        {
                            ExecutionConstants.XeroTenantId, new MessageAttributeValue()
                            {
                                DataType = "String", StringValue = Guid.NewGuid().ToString()
                            }
                        }
                    },
                    Body = JsonSerializer.Serialize(expectedMessageBody)
                }

            ]
        };

        _mockSqsClient
            .Setup(m => m.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedReceiveMessageResponse);

        //Act
        var result = await _sut.ReceiveMessageAsync(receiveMessageRequest, CancellationToken.None);

        //Assert
        Assert.IsType<ReceiveMessageResponse>(result);
        Assert.Equal(HttpStatusCode.OK, result.HttpStatusCode);
        Assert.NotNull(result.Messages.FirstOrDefault());
        Assert.Equal(expectedMessageBody, JsonSerializer.Deserialize<ExecutionQueueMessage>(result.Messages.FirstOrDefault()!.Body));
        Assert.Equal(expectedXeroCorrelationId, result.Messages.FirstOrDefault()!.MessageAttributes.FirstOrDefault().Value.StringValue);
    }

    [Fact]
    public async Task GivenSqsFailure_WhenReceiveMessageAsync_ThenErrorIsThrown()
    {
        var messageRequest = CreateSqsRequest<ReceiveMessageRequest>();
        _mockSqsClient.Setup(m => m.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("oopsie :c"));

        await Assert.ThrowsAsync<Exception>(async () => await _sut.ReceiveMessageAsync(messageRequest, CancellationToken.None));
    }

    [Fact]
    public async Task GivenSqsSuccessAndSuccessfulDelete_DeleteMessageBatchAsync_ThenReturnsSuccessfulResult()
    {
        var batchDeleteRequest = CreateSqsRequest<DeleteMessageBatchRequest>();
        var mockMessageEntryId = "message-entry-1";
        var mockedResponse = new DeleteMessageBatchResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            Successful =
            [
                new DeleteMessageBatchResultEntry
                {
                    Id = mockMessageEntryId
                }
            ]
        };
        _mockSqsClient.Setup(m => m.DeleteMessageBatchAsync(It.IsAny<DeleteMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedResponse);

        var result = await _sut.DeleteMessageBatchAsync(batchDeleteRequest, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, mockedResponse.HttpStatusCode);
        Assert.Equal(mockMessageEntryId, result.Successful.FirstOrDefault()?.Id);
    }

    [Fact]
    public async Task GivenSqsSuccessAndFailedDelete_DeleteMessageBatchAsync_ThenReturnsFailedResult()
    {
        var batchDeleteRequest = CreateSqsRequest<DeleteMessageBatchRequest>();
        var mockMessageEntryId = "message-entry-1";
        var mockedResponse = new DeleteMessageBatchResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            Failed =
            [
                new BatchResultErrorEntry
                {
                    Id = mockMessageEntryId,
                    SenderFault = true
                }
            ]
        };
        _mockSqsClient.Setup(m => m.DeleteMessageBatchAsync(It.IsAny<DeleteMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockedResponse);

        var result = await _sut.DeleteMessageBatchAsync(batchDeleteRequest, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, result.HttpStatusCode);
        Assert.Equal(mockMessageEntryId, mockedResponse.Failed.FirstOrDefault()?.Id);
        Assert.Equal(true, mockedResponse.Failed.FirstOrDefault()?.SenderFault);
    }

    [Fact]
    public async Task GivenSqsFailure_DeleteMessageBatchAsync_ThenErrorIsThrown()
    {
        var batchDeleteRequest = CreateSqsRequest<DeleteMessageBatchRequest>();
        _mockSqsClient.Setup(m => m.DeleteMessageBatchAsync(It.IsAny<DeleteMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("oopsie :c"));

        await Assert.ThrowsAsync<Exception>(() => _sut.DeleteMessageBatchAsync(batchDeleteRequest, CancellationToken.None));
    }

    private static SendMessageRequest CreateValidSendMessageRequest()
    {
        return new SendMessageRequest
        {
            QueueUrl = "my-queue-url",
            MessageBody = JsonSerializer.Serialize(new { Message = "Hello, World!" })
        };
    }

    private static T CreateSqsRequest<T>() where T : class
    {
        if (typeof(T) == typeof(ReceiveMessageRequest))
        {
            return (T)(object)new ReceiveMessageRequest()
            {
                MessageAttributeNames = ["All"],
                QueueUrl = "my-queue-url",
                MaxNumberOfMessages = 10,
            };
        }

        if (typeof(T) == typeof(DeleteMessageBatchRequest))
        {
            return (T)(object)new DeleteMessageBatchRequest()
            {
                QueueUrl = "my-queue-url",
                Entries =
                [
                    new DeleteMessageBatchRequestEntry
                    {
                        Id = "message-entry-1",
                        ReceiptHandle = "1-receipt-handle"
                    },
                    new DeleteMessageBatchRequestEntry
                    {
                        Id = "message-entry-2",
                        ReceiptHandle = "1-receipt-handle"
                    }
                ]
            };
        }

        throw new ArgumentException("Sqs request type not configured in the factory");
    }
}
