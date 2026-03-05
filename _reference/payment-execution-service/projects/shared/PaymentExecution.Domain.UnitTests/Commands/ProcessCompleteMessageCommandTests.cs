using System.Text.Json;
using Amazon.SQS.Model;
using AutoFixture.Xunit2;
using AutoMapper;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using NewRelic.Api.Agent;
using PaymentExecution.Common;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Mapping;
using PaymentExecution.Domain.Models;
using PaymentExecution.Domain.Service;
using PaymentExecution.Domain.UnitTests.TestData;
using PaymentExecution.NewRelicClient;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;
using PaymentExecution.SqsIntegrationClient.Service;
using PaymentExecution.TestUtilities;

namespace PaymentExecution.Domain.UnitTests.Commands;

public class ProcessCompleteMessageCommandTests
{
    private readonly Mock<IExecutionQueueService> _sqsService;
    private readonly Mock<IMonitoringClient> _monitoringClient;
    private readonly Mock<IPaymentTransactionRepository> _paymentTransactionRepository;
    private readonly Mock<ILogger<ProcessCompleteMessagesCommandHandler>> _logger;
    private readonly Mock<IProcessCompleteMessageDomainService> _processMessageService;
    private readonly ProcessCompleteMessagesCommandHandler _sut;

    public ProcessCompleteMessageCommandTests()
    {
        _sqsService = new Mock<IExecutionQueueService>();
        _monitoringClient = new Mock<IMonitoringClient>();
        _paymentTransactionRepository = new Mock<IPaymentTransactionRepository>();
        _logger = new Mock<ILogger<ProcessCompleteMessagesCommandHandler>>();
        _processMessageService = new Mock<IProcessCompleteMessageDomainService>();
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new DomainToRepositoryMappingProfile());
            cfg.AddProfile(new DomainToDomainMappingProfile());
            cfg.AddProfile(new DomainToServiceMappingProfile());
        }).CreateMapper();
        _sut = new ProcessCompleteMessagesCommandHandler(
            _sqsService.Object,
            _monitoringClient.Object,
            _paymentTransactionRepository.Object,
            _logger.Object,
            _processMessageService.Object,
            mapper);
    }

    [Fact]
    public async Task GivenNoMessagesReturnedFromSqs_WhenProcessMessageCommand_ThenResponseReturned()
    {
        var mockSqsResponse = new ReceiveMessageResponse()
        {
            Messages = new List<Message>()
        };
        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsResponse);

        var response = await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        Assert.Equal(0, response.SuccessfullyProcessedMessages);
        Assert.False(response.IsProcessingError);
        _processMessageService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task GivenMultipleMessagesProcessedInParallelWithNoErrors_WhenProcessMessageCommand_ThenMessagesAreProcessedSuccessfully(
        PaymentTransactionDto mockPaymentTransactionDto)
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Succeeded", "Failed", "Succeeded", "Failed", "Cancelled"
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.SetupSequence(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockPaymentTransactionDto);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Ok());
        _processMessageService.SetupSequence(m =>
                m.ShouldEventBeIgnored(It.IsAny<PaymentTransactionDto>(), It.IsAny<CompleteMessage>()))
            .Returns(false)
            .Returns(false)
            .Returns(true)
            .Returns(true)
            .Returns(false);
        _processMessageService.Setup(m => m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(),
            It.IsAny<StripeValidCompleteStatus>()))
            .ReturnsAsync(Result.Ok());
        _processMessageService.Setup(m => m.HandleFailPaymentRequestAsync(It.IsAny<CompleteMessage>()))
            .ReturnsAsync(Result.Ok());
        _processMessageService.Setup(m => m.HandleExecutionSucceedPaymentRequestAsync(It.IsAny<CompleteMessage>()))
            .ReturnsAsync(Result.Ok());
        _sqsService.Setup(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteMessageBatchResultWithSuccessfulEntries(mockMessageStatuses.Count));

        //Act
        var response = await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        //Assert
        Assert.Equal(mockMessageStatuses.Count, response.SuccessfullyProcessedMessages);
        Assert.False(response.IsProcessingError);
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task GivenMessageInSucceededStatusAndNoProcessingError_WhenProcessMessageCommand_ThenMessageProcessedSuccessfully(
        PaymentTransactionDto mockedPaymentTransactionDto)
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Succeeded",
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.SetupSequence(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockedPaymentTransactionDto);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Ok());
        _processMessageService.SetupSequence(m =>
                m.ShouldEventBeIgnored(It.IsAny<PaymentTransactionDto>(), It.IsAny<CompleteMessage>()))
            .Returns(false);
        _processMessageService.Setup(m => m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(),
            It.IsAny<StripeValidCompleteStatus>()))
            .ReturnsAsync(Result.Ok());
        _processMessageService.Setup(m => m.HandleExecutionSucceedPaymentRequestAsync(It.IsAny<CompleteMessage>()))
            .ReturnsAsync(Result.Ok());
        _sqsService.Setup(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteMessageBatchResultWithSuccessfulEntries(mockMessageStatuses.Count));

        //Act
        var response = await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        //Assert
        Assert.Equal(mockMessageStatuses.Count, response.SuccessfullyProcessedMessages);
        Assert.False(response.IsProcessingError);
        _processMessageService.Verify(m => m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(), It.IsAny<StripeValidCompleteStatus>()), Times.Once);
        _processMessageService.Verify(m => m.HandleExecutionSucceedPaymentRequestAsync(It.IsAny<CompleteMessage>()), Times.Once);
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task GivenMessageInFailedStatusAndNoProcessingError_WhenProcessMessageCommand_ThenMessageProcessedSuccessfully(
        PaymentTransactionDto mockedPaymentTransactionDto)
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Failed",
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.SetupSequence(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockedPaymentTransactionDto);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Ok());
        _processMessageService.SetupSequence(m =>
                m.ShouldEventBeIgnored(It.IsAny<PaymentTransactionDto>(), It.IsAny<CompleteMessage>()))
            .Returns(false);
        _processMessageService.Setup(m => m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(),
            It.IsAny<StripeValidCompleteStatus>()))
            .ReturnsAsync(Result.Ok());
        _processMessageService.Setup(m => m.HandleFailPaymentRequestAsync(It.IsAny<CompleteMessage>()))
            .ReturnsAsync(Result.Ok());
        _sqsService.Setup(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteMessageBatchResultWithSuccessfulEntries(mockMessageStatuses.Count));

        //Act
        var response = await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        //Assert
        Assert.Equal(mockMessageStatuses.Count, response.SuccessfullyProcessedMessages);
        Assert.False(response.IsProcessingError);
        _processMessageService.Verify(m => m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(), It.IsAny<StripeValidCompleteStatus>()), Times.Once);
        _processMessageService.Verify(m => m.HandleFailPaymentRequestAsync(It.IsAny<CompleteMessage>()), Times.Once);
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task GivenMessageInCancelledStatusAndNoProcessingError_WhenProcessMessageCommand_ThenMessageProcessedSuccessfully(
        PaymentTransactionDto mockedPaymentTransactionDto)
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Cancelled",
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.SetupSequence(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockedPaymentTransactionDto);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Ok());
        _processMessageService.SetupSequence(m =>
                m.ShouldEventBeIgnored(It.IsAny<PaymentTransactionDto>(), It.IsAny<CompleteMessage>()))
            .Returns(false);
        _processMessageService.Setup(m => m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(),
            It.IsAny<StripeValidCompleteStatus>()))
            .ReturnsAsync(Result.Ok());
        _processMessageService.Setup(m => m.HandleCancelPaymentRequestAsync(It.IsAny<CompleteMessage>()))
            .ReturnsAsync(Result.Ok());
        _sqsService.Setup(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteMessageBatchResultWithSuccessfulEntries(mockMessageStatuses.Count));

        //Act
        var response = await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        //Assert
        Assert.Equal(mockMessageStatuses.Count, response.SuccessfullyProcessedMessages);
        Assert.False(response.IsProcessingError);
        _processMessageService.Verify(m => m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(), It.IsAny<StripeValidCompleteStatus>()), Times.Once);
        _processMessageService.Verify(m => m.HandleCancelPaymentRequestAsync(It.IsAny<CompleteMessage>()), Times.Once);
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenNoErrorsOrInValidMessageBodies_WhenProcessMessageCommand_ThenDistributedTracingIsRetrievedForEachMessage()
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Succeeded", "Succeeded"
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);

        var mockDatabaseStatuses = new List<string>()
        {
            "in-progress", "in-progress"
        };
        var mockRepositoryGetResponse = CreatePaymentTransactionDtosFromMockDbStatus(mockDatabaseStatuses);

        var mockSqsDeleteResponse = CreateDeleteMessageBatchResultWithSuccessfulEntries(mockMessageStatuses.Count);
        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.SetupSequence(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockRepositoryGetResponse[0])
            .ReturnsAsync(mockRepositoryGetResponse[1]);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Ok());
        _processMessageService.Setup(m => m.ShouldEventBeIgnored(It.IsAny<PaymentTransactionDto>(), It.IsAny<CompleteMessage>()))
            .Returns(false);
        _processMessageService.Setup(m => m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(),
                It.IsAny<StripeValidCompleteStatus>()))
            .ReturnsAsync(Result.Ok());
        _processMessageService.Setup(m => m.HandleExecutionSucceedPaymentRequestAsync(It.IsAny<CompleteMessage>()))
            .ReturnsAsync(Result.Ok());
        _sqsService.Setup(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSqsDeleteResponse);

        //Act
        var result = await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        //Assert
        Assert.Equal(mockMessageStatuses.Count, result.SuccessfullyProcessedMessages);
        _monitoringClient.Verify(m => m.AcceptDistributedTraceHeaders(It.Is<Message>(msg => msg.MessageId == mockSqsGetResponse.Messages[0].MessageId), It.IsAny<Func<Message, string, IEnumerable<string>>>(), TransportType.Queue), Times.Once);
        _monitoringClient.Verify(m => m.AcceptDistributedTraceHeaders(It.Is<Message>(msg => msg.MessageId == mockSqsGetResponse.Messages[1].MessageId), It.IsAny<Func<Message, string, IEnumerable<string>>>(), TransportType.Queue), Times.Once);

    }

    [Fact]
    public async Task GivenNoXeroCorrelationIdButValidBody_WhenProcessMessageCommand_ThenWarningLoggedButExecutionContinues()
    {
        //Arrange
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(new List<string>() { "Succeeded" });
        mockSqsGetResponse.Messages[0].MessageAttributes.Remove(ExecutionConstants.XeroCorrelationId);

        var mockRepositoryGetResponse = CreatePaymentTransactionDtosFromMockDbStatus(new List<string>() { "Succeeded" });
        var mockSqsDeleteResponse = CreateDeleteMessageBatchResultWithSuccessfulEntries(1);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.SetupSequence(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockRepositoryGetResponse[0]);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Ok());
        _processMessageService.Setup(m => m.ShouldEventBeIgnored(It.IsAny<PaymentTransactionDto>(), It.IsAny<CompleteMessage>()))
            .Returns(false);
        _processMessageService.Setup(m => m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(),
                It.IsAny<StripeValidCompleteStatus>()))
            .ReturnsAsync(Result.Ok());
        _processMessageService.Setup(m => m.HandleExecutionSucceedPaymentRequestAsync(It.IsAny<CompleteMessage>()))
            .ReturnsAsync(Result.Ok());

        _sqsService.Setup(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSqsDeleteResponse);

        //Act
        var response = await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        //Assert
        Assert.Equal(1, response.SuccessfullyProcessedMessages);
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_logger, LogLevel.Information,
            $"No Xero-Correlation-Id was found for Message with ID {mockSqsGetResponse.Messages[0].MessageId}. New Xero-Correlation-Id added", 1);
    }

    [Fact]
    public async Task GivenNoXeroTenantIdButValidBody_WhenProcessMessageCommand_ThenErrorLoggedAndExecutionFails()
    {
        //Arrange
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(new List<string>() { "Succeeded" });
        mockSqsGetResponse.Messages[0].MessageAttributes.Remove(ExecutionConstants.XeroTenantId);


        var mockRepositoryGetResponse = CreatePaymentTransactionDtosFromMockDbStatus(new List<string>() { "Succeeded" });
        var mockSqsDeleteResponse = CreateDeleteMessageBatchResultWithSuccessfulEntries(1);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.SetupSequence(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockRepositoryGetResponse[0]);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Ok());
        _processMessageService.Setup(m => m.ShouldEventBeIgnored(It.IsAny<PaymentTransactionDto>(), It.IsAny<CompleteMessage>()))
            .Returns(false);
        _processMessageService.Setup(m => m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(),
                It.IsAny<StripeValidCompleteStatus>()))
            .ReturnsAsync(Result.Ok());
        _processMessageService.Setup(m => m.HandleExecutionSucceedPaymentRequestAsync(It.IsAny<CompleteMessage>()))
            .ReturnsAsync(Result.Ok());
        _sqsService.Setup(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSqsDeleteResponse);

        //Act
        var response = await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        //Assert
        Assert.Equal(0, response.SuccessfullyProcessedMessages);
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_logger, LogLevel.Error, "Event does not have tenantId which is a required property for MessageId", 1);
    }

    [Theory]
    [ClassData(typeof(InvalidMessageBodyTestData))]
    public async Task GivenInvalidMessageBody_WhenProcessMessageCommand_ThenErrorLoggedAndProcessingStopped(Message messageWithInvalidBody)
    {
        //Arrange
        var expectedLogMessage = "An error occured attempting to transform message to domain";

        var mockSqsGetResponse = new ReceiveMessageResponse();
        mockSqsGetResponse.Messages.Add(messageWithInvalidBody);

        var mockDatabaseStatuses = new List<string>()
        {
            "in-progress"
        };
        var mockRepositoryGetResponses = CreatePaymentTransactionDtosFromMockDbStatus(mockDatabaseStatuses);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.SetupSequence(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockRepositoryGetResponses[0]);

        //Act
        await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        //Assert
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_logger, LogLevel.Error, expectedLogMessage, 1);
        _sqsService.Verify(m => m.GetMessagesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _processMessageService.VerifyNoOtherCalls();
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenErrorGettingRecordFromDb_WhenProcessMessageCommand_ThenMessageProcessingStoppedAndNotDeletedFromQueue()
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Succeeded", "Failed"
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);
        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Fail("Something went wrong retrieving the Payment Transaction from the DB"));

        //Act
        await _sut.Handle(new ProcessCompleteMessagesCommand(), CancellationToken.None);

        //Assert
        _processMessageService.VerifyNoOtherCalls();
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoPaymentRequestFoundInDb_WhenProcessMessageCommand_ThenCriticalErrorLogged()
    {
        var mockMessageStatuses = new List<string>()
        {
            "Succeeded"
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync((PaymentTransactionDto?)null);

        //Act
        await _sut.Handle(new ProcessCompleteMessagesCommand(), CancellationToken.None);

        //Assert
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_logger, LogLevel.Critical, "No database record found for PaymentRequestId", 1);
    }

    [Fact]
    public async Task GivenNoPaymentRequestFoundInDb_WhenProcessMessageCommand_ThenMessageProcessingStoppedAndNotDeletedFromQueue()
    {
        var mockMessageStatuses = new List<string>()
        {
            "Succeeded"
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync((PaymentTransactionDto?)null);

        //Act
        await _sut.Handle(new ProcessCompleteMessagesCommand(), CancellationToken.None);

        //Assert
        _processMessageService.VerifyNoOtherCalls();
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenExceptionIsThrownDuringProcessing_WhenProcessMessageCommand_ThenErrorLoggedAndMessageProcessingStopped()
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Succeeded"
        };
        var expectedLogPrefix = "Unexpected error processing message";
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .Throws(new Exception("Unexpected error during processing"));

        //Act
        await _sut.Handle(new ProcessCompleteMessagesCommand(), CancellationToken.None);

        //Assert
        LoggerAssertions.VerifyLogMessagesWithPrefixAtLevel(_logger, LogLevel.Error, expectedLogPrefix, 1);
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task GivenDbRecordIsInErrorState_WhenProcessCompleteMessageCommand_ThenMessageProcessingStoppedAndNotDeletedFromQueue(PaymentTransactionDto mockedPaymentTransactionDto)
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Succeeded"
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.SetupSequence(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockedPaymentTransactionDto);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Fail("Message in error state"));

        //Act
        await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        //Assert
        _paymentTransactionRepository.Verify(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()), Times.Once);
        _processMessageService.Verify(m => m.EvaluateIfRecordIsInErrorState(mockedPaymentTransactionDto));
        _processMessageService.VerifyNoOtherCalls();
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task GivenDbRecordIsInStaleState_WhenProcessCompleteMessageCommand_ThenMessageProcessingFinishedAndDeletedFromTheQueue(PaymentTransactionDto mockedPaymentTransactionDto)
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Succeeded"
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);
        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.SetupSequence(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockedPaymentTransactionDto);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Ok);
        _processMessageService
            .Setup(m => m.ShouldEventBeIgnored(It.IsAny<PaymentTransactionDto>(), It.IsAny<CompleteMessage>()))
            .Returns(true);
        _sqsService.Setup(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageBatchResponse());

        //Act
        await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        //Assert
        _paymentTransactionRepository.Verify(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()), Times.Once);
        _processMessageService.Verify(m => m.EvaluateIfRecordIsInErrorState(mockedPaymentTransactionDto), Times.Once);
        _processMessageService.Verify(m => m.ShouldEventBeIgnored(mockedPaymentTransactionDto, It.IsAny<CompleteMessage>()), Times.Once);
        _processMessageService.VerifyNoOtherCalls();
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenErrorOccursWhenUpdatingDb_WhenProcessCompleteMessageCommand_ThenMessageProcessingStoppedAndNotDeletedFromQueue()
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Succeeded"
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);
        var mockDatabaseStatuses = new List<string>()
        {
            "processing"
        };
        var mockRepositoryGetResponse = CreatePaymentTransactionDtosFromMockDbStatus(mockDatabaseStatuses);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockRepositoryGetResponse[0]);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Ok());
        _processMessageService.Setup(m => m.ShouldEventBeIgnored(It.IsAny<PaymentTransactionDto>(), It.IsAny<CompleteMessage>()))
            .Returns(false);
        _processMessageService.Setup(m =>
                m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(), It.IsAny<StripeValidCompleteStatus>()))
            .ReturnsAsync(Result.Fail("Something has happened attempting to update the DB"));

        //Act
        await _sut.Handle(new ProcessCompleteMessagesCommand(), new CancellationToken());

        //Assert
        _processMessageService.Verify(m => m.HandleExecutionSucceedPaymentRequestAsync(It.IsAny<CompleteMessage>()), Times.Never);
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task GivenErrorExecutionSucceedingPaymentRequest_WhenProcessMessageCommand_ThenMessageNotDeletedFromQueue(
        PaymentTransactionDto mockedPaymentTransactionDto)
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Succeeded"
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses);

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockedPaymentTransactionDto);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Ok());
        _processMessageService.Setup(m => m.ShouldEventBeIgnored(It.IsAny<PaymentTransactionDto>(), It.IsAny<CompleteMessage>()))
            .Returns(false);
        _processMessageService.Setup(m =>
                m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(), It.IsAny<StripeValidCompleteStatus>()))
            .ReturnsAsync(Result.Ok());
        _processMessageService.Setup(m => m.HandleExecutionSucceedPaymentRequestAsync(It.IsAny<CompleteMessage>()))
            .ReturnsAsync(Result.Fail("Something went wrong attempting to update the Payment Request"));
        _sqsService.Setup(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageBatchResponse());

        //Act
        var response = await _sut.Handle(new ProcessCompleteMessagesCommand(), CancellationToken.None);

        //Assert
        Assert.True(response.IsProcessingError);
        _processMessageService.Verify(m => m.HandleExecutionSucceedPaymentRequestAsync(It.IsAny<CompleteMessage>()), Times.Once);
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task GivenErrorFailingPaymentRequest_WhenProcessMessageCommand_ThenMessageNotDeletedFromQueue(PaymentTransactionDto mockedPaymentTransactionDto)
    {
        //Arrange
        var mockMessageStatuses = new List<string>()
        {
            "Failed"
        };
        var mockSqsGetResponse = CreateReceiveMessageResponseWithValidBodyAndStatus(mockMessageStatuses); ;

        _sqsService.Setup(m => m.GetMessagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockSqsGetResponse);
        _paymentTransactionRepository.Setup(m => m.GetPaymentTransactionsByPaymentRequestId(It.IsAny<Guid>()))
            .ReturnsAsync(mockedPaymentTransactionDto);
        _processMessageService.Setup(m =>
                m.EvaluateIfRecordIsInErrorState(It.IsAny<PaymentTransactionDto>()))
            .Returns(Result.Ok());
        _processMessageService.Setup(m => m.ShouldEventBeIgnored(It.IsAny<PaymentTransactionDto>(), It.IsAny<CompleteMessage>()))
            .Returns(false);
        _processMessageService.Setup(m =>
                m.HandleUpdateDbAsync(It.IsAny<CompleteMessage>(), It.IsAny<StripeValidCompleteStatus>()))
            .ReturnsAsync(Result.Ok());
        _processMessageService.Setup(m => m.HandleFailPaymentRequestAsync(It.IsAny<CompleteMessage>()))
            .ReturnsAsync(Result.Fail("Something went wrong attempting to update the Payment Request"));
        _sqsService.Setup(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageBatchResponse());

        //Act
        var response = await _sut.Handle(new ProcessCompleteMessagesCommand(), CancellationToken.None);

        //Assert
        Assert.True(response.IsProcessingError);
        _processMessageService.Verify(m => m.HandleFailPaymentRequestAsync(It.IsAny<CompleteMessage>()), Times.Once);
        _sqsService.Verify(m => m.DeleteMessagesAsync(It.IsAny<List<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    private static DeleteMessageBatchResponse CreateDeleteMessageBatchResultWithSuccessfulEntries(int entries)
    {
        var successfulEntryList = new List<DeleteMessageBatchResultEntry>();
        for (int i = 0; i < entries; i++)
        {
            successfulEntryList.Add(new DeleteMessageBatchResultEntry() { Id = $"message-id-{i}" });
        }

        return new DeleteMessageBatchResponse()
        { Successful = successfulEntryList, Failed = new List<BatchResultErrorEntry>() };
    }

    private static List<PaymentTransactionDto> CreatePaymentTransactionDtosFromMockDbStatus(List<string> statuses)
    {
        List<PaymentTransactionDto> paymentTransactionDtos = new();
        for (int i = 0; i < statuses.Count; i++)
        {
            paymentTransactionDtos.Add(new PaymentTransactionDto()
            {
                PaymentRequestId = Guid.NewGuid(),
                ProviderServiceId = Guid.NewGuid(),
                PaymentProviderPaymentTransactionId = "mock_payment_intent_Id",
                Status = statuses[i],
                EventCreatedDateTimeUtc = DateTime.UtcNow.AddDays(-1), //add previous date
                ProviderType = ProviderType.Stripe.ToString(),
            });
        }

        return paymentTransactionDtos;
    }

    private static ReceiveMessageResponse CreateReceiveMessageResponseWithValidBodyAndStatus(List<string> statuses)
    {
        var messageList = new List<Message>();

        for (int i = 0; i < statuses.Count; i++)
        {
            messageList.Add(new Message()
            {
                MessageId = $"message-id-${i}",
                ReceiptHandle = $"receipt-handle-${i}",
                Body = JsonSerializer.Serialize(ExecutionQueueMessageFactoryWithStatus(statuses[i])),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                {
                    {
                        ExecutionConstants.XeroCorrelationId, new MessageAttributeValue()
                        {
                            DataType = "String",
                            StringValue = Guid.NewGuid().ToString()
                        }
                    },
                     {
                        ExecutionConstants.XeroTenantId, new MessageAttributeValue()
                        {
                            DataType = "String",
                            StringValue = Guid.NewGuid().ToString()
                        }
                    }
                }
            });
        }

        return new ReceiveMessageResponse()
        {
            Messages = messageList
        };
    }

    private static ExecutionQueueMessage? ExecutionQueueMessageFactoryWithStatus(string status)
    {
        var rnd = new Random();

        switch (status)
        {
            case "Succeeded":
                return new ExecutionQueueMessage()
                {
                    PaymentRequestId = Guid.NewGuid(),
                    ProviderServiceId = Guid.NewGuid(),
                    ProviderType = rnd.Next(0, 1) > 0.5 ? ProviderType.Stripe.ToString() : ProviderType.GoCardless.ToString(),
                    Fee = rnd.Next(1, 10),
                    FeeCurrency = "aud",
                    PaymentProviderPaymentTransactionId = $"transaction-reference-${rnd.Next(1, 20)}",
                    PaymentProviderPaymentReferenceId = "ref-123",
                    EventCreatedDateTime = DateTime.UtcNow,
                    Status = TerminalStatus.Succeeded.ToString()
                };
            case "Failed":
                return new ExecutionQueueMessage()
                {
                    PaymentRequestId = Guid.NewGuid(),
                    ProviderServiceId = Guid.NewGuid(),
                    ProviderType = rnd.Next(0, 1) > 0.5 ? ProviderType.Stripe.ToString() : ProviderType.GoCardless.ToString(),
                    Status = TerminalStatus.Failed.ToString(),
                    PaymentProviderPaymentReferenceId = "ref-123",
                    EventCreatedDateTime = DateTime.UtcNow,
                    FailureDetails = "test failure"
                };
            case "Cancelled":
                {
                    return new ExecutionQueueMessage
                    {
                        PaymentRequestId = Guid.NewGuid(),
                        ProviderServiceId = Guid.NewGuid(),
                        Status = TerminalStatus.Cancelled.ToString(),
                        EventCreatedDateTime = DateTime.UtcNow,
                        PaymentProviderPaymentTransactionId = "pi-123",
                        CancellationReason = "Abandoned",
                        ProviderType = ProviderType.Stripe.ToString(),
                    };
                }
            default:
                return null;
        }
    }
}
