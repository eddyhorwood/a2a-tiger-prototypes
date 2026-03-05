using System.Text.Json;
using Amazon.SQS.Model;
using AutoFixture.Xunit2;
using Microsoft.Extensions.DependencyInjection;
using PaymentExecution.Domain.Models;
using PaymentExecution.Repository.Models;
using Xunit;

namespace PaymentExecutionWorker.ComponentTests;

public class WorkerComponentTests : IAsyncLifetime
{

    private const string ExecutionSucceedResponseOverrideStubId = "da58b429-58a2-4850-bd75-ad939b3bc352";
    private const string GetPaymentRequestResponseOverrideStubId = "4b80b620-cf25-415f-b7e8-31ebe5fcb270";

    [Fact]
    public async Task GivenMessagesInTheQueue_WhenWorkerProcessesValidMessages_ThenMessagesAreDeletedFromTheQueue()
    {
        var cts = new CancellationTokenSource();
        await using ComponentTestFixture fixture = new(_ => { });
        var numberOfMessagesToProcess = 10;
        await fixture.SetupSqsAndDbWithCount(numberOfMessagesToProcess);
        var worker = fixture.Services.GetRequiredService<Worker.Worker>();

        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();
        cts.Dispose();

        var response = await fixture.GetMessagesFromQueue();

        Assert.Empty(response.Messages);

        await fixture.PurgeQueue();
    }

    [Theory]
    [InlinePaymentExecutionAutoData(nameof(StripeValidCompleteStatus.Succeeded), "execution-succeed")]
    [InlinePaymentExecutionAutoData(nameof(StripeValidCompleteStatus.Failed), "fail")]
    [InlinePaymentExecutionAutoData(nameof(StripeValidCompleteStatus.Cancelled), "cancel-execution-in-progress")]
    public async Task
        GivenValidMessageEnqueuedOfTerminalStatus_WhenWorkerProcessesMessage_ThenMessageShouldBeCorrectlyProcessed(
            string testStatus, string expectedPaymentRequestEndpoint, ExecutionQueueMessage mockedQueueMessage, PaymentTransactionDto paymentTransactionDto)
    {
        var cts = new CancellationTokenSource();
        await using ComponentTestFixture fixture = new(_ => { });
        var testPaymentRequestId = Guid.NewGuid();
        var expectedUrl = $"/v1/payment-requests/{testPaymentRequestId}/{expectedPaymentRequestEndpoint}";

        //Set up queue message
        mockedQueueMessage.Status = testStatus;
        // Match fields with corresponding DB record (mimic submit flow)
        mockedQueueMessage.PaymentRequestId = testPaymentRequestId;
        mockedQueueMessage.PaymentProviderPaymentTransactionId = paymentTransactionDto.PaymentProviderPaymentTransactionId;
        mockedQueueMessage.ProviderServiceId = paymentTransactionDto.ProviderServiceId!.Value;
        var sqsFormattedMessage = new List<Message> { new() { Body = JsonSerializer.Serialize(mockedQueueMessage) } };
        await fixture.PopulateSqsWithMessages(sqsFormattedMessage);

        //Set up DB record
        paymentTransactionDto.PaymentRequestId = testPaymentRequestId;
        paymentTransactionDto.Status = "in_progress";
        await fixture.GetTestRepositoryInterface().InsertMockSubmittedPaymentTransaction(paymentTransactionDto);

        // Act
        var worker = fixture.Services.GetRequiredService<Worker.Worker>();
        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();
        cts.Dispose();

        //Assert queue
        var response = await fixture.GetMessagesFromQueue();
        Assert.Empty(response.Messages);

        // Assert DB status has been updated
        var recordPostProcessing = await fixture.GetPaymentTransactionByPaymentRequestId(testPaymentRequestId);
        Assert.Equal(testStatus, recordPostProcessing.Status);

        // Assert Payment Request call
        var wireMockRequestsMade = await fixture.GetListOfRequestUrlsSentToPaymentRequestWiremock();
        Assert.NotNull(wireMockRequestsMade);
        var callsToPaymentRequestWithPaymentRequestIdUnderTest = wireMockRequestsMade!.Where(
            req => req.Request.Url.Contains(testPaymentRequestId.ToString())).ToList();
        Assert.Single(callsToPaymentRequestWithPaymentRequestIdUnderTest);
        Assert.Equal(expectedUrl, callsToPaymentRequestWithPaymentRequestIdUnderTest[0].Request.Url);

        //Cleanup
        await fixture.PurgeQueue();
    }

    [Theory, AutoData]
    public async Task
        GivenValidMessageEnqueuedOfCancelledStatusWithLargeCancellationReason_WhenWorkerProcessesMessage_ThenMessageShouldBeCorrectlyProcessed(ExecutionQueueMessage mockedQueueMessage, PaymentTransactionDto paymentTransactionDto)
    {
        var expectedStatus = "Cancelled";
        var cts = new CancellationTokenSource();
        await using ComponentTestFixture fixture = new(_ => { });
        var testPaymentRequestId = Guid.NewGuid();
        var expectedUrl = $"/v1/payment-requests/{testPaymentRequestId}/cancel-execution-in-progress";
        var expectedCancellationReason = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        //Set up queue message
        mockedQueueMessage.Status = expectedStatus;
        // Match fields with corresponding DB record (mimic submit flow)
        mockedQueueMessage.PaymentRequestId = testPaymentRequestId;
        mockedQueueMessage.PaymentProviderPaymentTransactionId = paymentTransactionDto.PaymentProviderPaymentTransactionId;
        mockedQueueMessage.ProviderServiceId = paymentTransactionDto.ProviderServiceId!.Value;
        mockedQueueMessage.FeeCurrency = "USD";
        mockedQueueMessage.ProviderType = "Stripe";
        mockedQueueMessage.CancellationReason = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var sqsFormattedMessage = new List<Message> { new() { Body = JsonSerializer.Serialize(mockedQueueMessage) } };
        await fixture.PopulateSqsWithMessages(sqsFormattedMessage);

        //Set up DB record
        paymentTransactionDto.PaymentRequestId = testPaymentRequestId;
        paymentTransactionDto.Status = "in_progress";
        paymentTransactionDto.FeeCurrency = "USD";
        paymentTransactionDto.ProviderType = "Stripe";
        await fixture.GetTestRepositoryInterface().InsertMockSubmittedPaymentTransaction(paymentTransactionDto);

        // Act
        var worker = fixture.Services.GetRequiredService<Worker.Worker>();
        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();
        cts.Dispose();

        //Assert queue
        var response = await fixture.GetMessagesFromQueue();
        Assert.Empty(response.Messages);

        // Assert DB status has been updated
        var recordPostProcessing = await fixture.GetPaymentTransactionByPaymentRequestId(testPaymentRequestId);
        Assert.Equal(expectedStatus, recordPostProcessing.Status);
        Assert.Equal(expectedCancellationReason, recordPostProcessing.CancellationReason);

        // Assert Payment Request call
        var wireMockRequestsMade = await fixture.GetListOfRequestUrlsSentToPaymentRequestWiremock();
        Assert.NotNull(wireMockRequestsMade);
        var callsToPaymentRequestWithPaymentRequestIdUnderTest = wireMockRequestsMade!.Where(
            req => req.Request.Url.Contains(testPaymentRequestId.ToString())).ToList();
        Assert.Single(callsToPaymentRequestWithPaymentRequestIdUnderTest);
        Assert.Equal(expectedUrl, callsToPaymentRequestWithPaymentRequestIdUnderTest[0].Request.Url);

        //Cleanup
        await fixture.PurgeQueue();
    }

    [Fact]
    public async Task GivenMessagesInTheQueueIncludingTwoInvalidMessages_WhenWorkerProcessesMessages_ThenInvalidMessagesAreNotDeletedFromQueue()
    {
        var cts = new CancellationTokenSource();
        await using ComponentTestFixture fixture = new(_ => { });
        var numberOfMessagesToProcess = 10;
        await fixture.SetupSqsAndDbWithTwoInvalidMessages(numberOfMessagesToProcess);

        var worker = fixture.Services.GetRequiredService<Worker.Worker>();

        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();
        cts.Dispose();

        var response = await fixture.GetMessagesFromQueue();

        Assert.Equal(2, response.Messages.Count);

        await fixture.PurgeQueue();
    }

    [Fact]
    public async Task GivenCallToPaymentRequestFails_WhenMessageIsUnsuccessfullyProcessedTwice_ThenMessagesShouldBeInDlq()
    {
        await using ComponentTestFixture fixture = new(_ => { });
        //Currently, all of these messages are set up with Succeed status for both the message the DB record
        var numberOfMessagesToProcess = 10;
        await fixture.SetupSqsAndDbWithCount(numberOfMessagesToProcess);

        await fixture.MockPaymentRequestExecutionSucceedToReturnStatusCode(ExecutionSucceedResponseOverrideStubId, 500);
        var worker = fixture.Services.GetRequiredService<Worker.Worker>();

        //Act 1
        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();
        cts.Dispose();

        //Assert 1
        var firstResponse = await fixture.GetMessagesFromQueue();
        Assert.NotEmpty(firstResponse.Messages);
        Assert.Equal(10, firstResponse.Messages.Count);

        await Task.Delay(TimeSpan.FromSeconds(2));

        //Act 2
        var secondCts = new CancellationTokenSource();
        await worker.StartAsync(secondCts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await secondCts.CancelAsync();
        secondCts.Dispose();

        //Assert 2
        var secondResponse = await fixture.GetMessagesFromDlq();
        Assert.NotEmpty(secondResponse.Messages);
        Assert.Equal(10, secondResponse.Messages.Count);

        //Cleanup
        await ComponentTestFixture.RemoveMappingOverrideOnPaymentRequestService(ExecutionSucceedResponseOverrideStubId);
        await fixture.PurgeQueue();
    }

    [Theory]
    [AutoData]
    public async Task
        GivenDbRecordHasNotBeenUpdatedWithProviderDetailsFromSubmitFlow_WhenWorkerProcessesMessage_ThenMessageShouldNotBeDeletedFromTheQueue(
            ExecutionQueueMessage mockedQueueMessage, InsertPaymentTransactionDto paymentTransactionDto)
    {
        var cts = new CancellationTokenSource();
        await using ComponentTestFixture fixture = new(_ => { });
        //Set up queue message
        mockedQueueMessage.Status = nameof(TerminalStatus.Succeeded);
        var sqsFormattedMessage = new List<Message> { new() { Body = JsonSerializer.Serialize(mockedQueueMessage) } };
        await fixture.PopulateSqsWithMessages(sqsFormattedMessage);
        //Set up database record with matching Payment Request Id with initial insert only
        paymentTransactionDto.PaymentRequestId = mockedQueueMessage.PaymentRequestId;
        await fixture.InsertPaymentTransactions([paymentTransactionDto]);

        var worker = fixture.Services.GetRequiredService<Worker.Worker>();

        //Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();
        cts.Dispose();

        //Assert
        var response = await fixture.GetMessagesFromQueue();
        Assert.Single(response.Messages);

        //Cleanup
        await fixture.PurgeQueue();
    }

    /// <summary>
    /// This test checks that if the database is already of a terminal state, it should process the message without calling Payment Request.
    /// This is to ensure that Payment Request is not updated with an event out of order
    /// </summary>
    [Theory]
    [PaymentExecutionData]
    public async Task
        GivenDbRecordIsAlreadyInTerminalStateAndIncomingMessageIsOfADifferentState_WhenWorkerProcessesMessage_ThenMessageShouldBeProcessedWithoutCallingPaymentRequest(
            ExecutionQueueMessage mockedQueueMessage, PaymentTransactionDto paymentTransactionDto)
    {
        var cts = new CancellationTokenSource();
        await using ComponentTestFixture fixture = new(_ => { });
        var testPaymentRequestId = Guid.NewGuid();
        var incomingMessageStatus = nameof(StripeValidCompleteStatus.Failed);
        var expectedDbStatus = nameof(StripeValidCompleteStatus.Succeeded);

        //Set up queue message
        mockedQueueMessage.Status = incomingMessageStatus;
        // Match fields with DB record from submit flow
        mockedQueueMessage.PaymentRequestId = testPaymentRequestId;
        mockedQueueMessage.PaymentProviderPaymentTransactionId = paymentTransactionDto.PaymentProviderPaymentTransactionId;
        mockedQueueMessage.ProviderServiceId = paymentTransactionDto.ProviderServiceId!.Value;
        var sqsFormattedMessage = new List<Message> { new() { Body = JsonSerializer.Serialize(mockedQueueMessage) } };
        await fixture.PopulateSqsWithMessages(sqsFormattedMessage);

        //Set up DB record
        paymentTransactionDto.PaymentRequestId = testPaymentRequestId;
        paymentTransactionDto.Status = expectedDbStatus;
        await fixture.GetTestRepositoryInterface().InsertMockSubmittedAndMockProcessedPaymentTransaction(paymentTransactionDto);

        //Act
        var worker = fixture.Services.GetRequiredService<Worker.Worker>();
        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();
        cts.Dispose();

        //Assert on sqs
        var response = await fixture.GetMessagesFromQueue();
        Assert.Empty(response.Messages);

        //Assert that db status has not been updated
        var recordPostProcessing = await fixture.GetPaymentTransactionByPaymentRequestId(testPaymentRequestId);
        Assert.Equal(expectedDbStatus, recordPostProcessing.Status);

        //Assert payment request has not been updated
        var wireMockRequestsMade = await fixture.GetListOfRequestUrlsSentToPaymentRequestWiremock();
        Assert.NotNull(wireMockRequestsMade);
        var callsToPaymentRequestWithPaymentRequestIdUnderTest = wireMockRequestsMade!.Where(
            req => req.Request.Url.Contains(testPaymentRequestId.ToString())).ToList();
        Assert.Empty(callsToPaymentRequestWithPaymentRequestIdUnderTest);

        //Cleanup
        await fixture.PurgeQueue();
    }

    /// <summary>
    /// This test ensures that the payment request status is called in the scenario of a processing failure after a successful DB update on initial processing
    /// </summary>
    [Theory]
    [InlinePaymentExecutionAutoData(nameof(StripeValidCompleteStatus.Succeeded), "execution-succeed")]
    [InlinePaymentExecutionAutoData(nameof(StripeValidCompleteStatus.Failed), "fail")]
    [InlinePaymentExecutionAutoData(nameof(StripeValidCompleteStatus.Cancelled), "cancel-execution-in-progress")]
    public async Task
        GivenIncomingMessageIsAReplayOfEventMatchingInDb_WhenMessageProcessed_ThenMessageShouldBeProcessedAndCallPaymentRequest(
            string statusUnderTest, string expectedEndpoint, ExecutionQueueMessage mockedQueueMessage, PaymentTransactionDto paymentTransactionDto)
    {
        var cts = new CancellationTokenSource();
        await using ComponentTestFixture fixture = new(_ => { });
        var testPaymentRequestId = Guid.NewGuid();
        var expectedUrl = $"/v1/payment-requests/{testPaymentRequestId}/{expectedEndpoint}";
        var mockedEventCreatedDateTime = DateTime.UtcNow;

        //Set up queue message
        mockedQueueMessage.EventCreatedDateTime = mockedEventCreatedDateTime;
        mockedQueueMessage.Status = statusUnderTest;
        // Match fields with DB record from submit flow
        mockedQueueMessage.PaymentRequestId = testPaymentRequestId;
        mockedQueueMessage.PaymentProviderPaymentTransactionId = paymentTransactionDto.PaymentProviderPaymentTransactionId;
        mockedQueueMessage.ProviderServiceId = paymentTransactionDto.ProviderServiceId!.Value;
        var sqsFormattedMessage = new List<Message> { new() { Body = JsonSerializer.Serialize(mockedQueueMessage) } };
        await fixture.PopulateSqsWithMessages(sqsFormattedMessage);

        //Set up DB record
        paymentTransactionDto.EventCreatedDateTimeUtc = mockedEventCreatedDateTime;
        paymentTransactionDto.PaymentRequestId = testPaymentRequestId;
        paymentTransactionDto.Status = statusUnderTest;
        await fixture.GetTestRepositoryInterface().InsertMockSubmittedAndMockProcessedPaymentTransaction(paymentTransactionDto);

        //Act
        var worker = fixture.Services.GetRequiredService<Worker.Worker>();
        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();
        cts.Dispose();

        //Assert on sqs
        var response = await fixture.GetMessagesFromQueue();
        Assert.Empty(response.Messages);

        //Assert payment request has been called
        var wireMockRequestsMade = await fixture.GetListOfRequestUrlsSentToPaymentRequestWiremock();
        Assert.NotNull(wireMockRequestsMade);
        var callsToPaymentRequestWithPaymentRequestIdUnderTest = wireMockRequestsMade!.Where(
            req => req.Request.Url.Contains(testPaymentRequestId.ToString())).ToList();
        Assert.Single(callsToPaymentRequestWithPaymentRequestIdUnderTest);
        Assert.Equal(expectedUrl, callsToPaymentRequestWithPaymentRequestIdUnderTest[0].Request.Url);

        //Cleanup
        await fixture.PurgeQueue();
    }

    /// <summary>
    /// This test checks that if the incoming event is less recent than the event in the database, it should process the message without calling Payment Request.
    /// This is to ensure that Payment Request is not updated with an outdated event
    /// </summary>
    [Theory]
    [PaymentExecutionData]
    public async Task
        GivenIncomingMessageIsStale_WhenWorkerProcessesMessage_ThenMessageShouldBeProcessedWithoutUpdatingDbOrCallingPaymentRequest(
            ExecutionQueueMessage mockedQueueMessage, PaymentTransactionDto paymentTransactionDto)
    {
        var cts = new CancellationTokenSource();
        await using ComponentTestFixture fixture = new(_ => { });
        var testPaymentRequestId = Guid.NewGuid();
        var mockDateTime = DateTime.UtcNow;
        var incomingMessageStatus = nameof(StripeValidCompleteStatus.Succeeded);
        var expectedDbStatus = "in_progress";

        //Set up queue message
        mockedQueueMessage.Status = incomingMessageStatus;
        mockedQueueMessage.EventCreatedDateTime = mockDateTime.AddHours(-5);
        // Match fields with DB record from submit flow
        mockedQueueMessage.PaymentRequestId = testPaymentRequestId;
        mockedQueueMessage.PaymentProviderPaymentTransactionId = paymentTransactionDto.PaymentProviderPaymentTransactionId;
        mockedQueueMessage.ProviderServiceId = paymentTransactionDto.ProviderServiceId!.Value;
        var sqsFormattedMessage = new List<Message> { new() { Body = JsonSerializer.Serialize(mockedQueueMessage) } };
        await fixture.PopulateSqsWithMessages(sqsFormattedMessage);

        //Setup DB record
        paymentTransactionDto.PaymentRequestId = testPaymentRequestId;
        paymentTransactionDto.EventCreatedDateTimeUtc = mockDateTime;
        paymentTransactionDto.Status = expectedDbStatus;
        await fixture.GetTestRepositoryInterface().InsertMockSubmittedAndMockProcessedPaymentTransaction(paymentTransactionDto);

        var worker = fixture.Services.GetRequiredService<Worker.Worker>();

        //Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();
        cts.Dispose();

        //Assert on queue
        var response = await fixture.GetMessagesFromQueue();
        Assert.Empty(response.Messages);

        //Assert db record status has not been updated
        var recordPostProcessing = await fixture.GetPaymentTransactionByPaymentRequestId(testPaymentRequestId);
        Assert.Equal(expectedDbStatus, recordPostProcessing.Status);

        //Assert payment request calls
        var wireMockRequestsMade = await fixture.GetListOfRequestUrlsSentToPaymentRequestWiremock();
        Assert.NotNull(wireMockRequestsMade);
        var callsToPaymentRequestWithPaymentRequestIdUnderTest
            = wireMockRequestsMade.Where(req => req.Request.Url.Contains(testPaymentRequestId.ToString())).ToList();
        Assert.Empty(callsToPaymentRequestWithPaymentRequestIdUnderTest);

        //Cleanup
        await fixture.PurgeQueue();
    }

    [Theory, PaymentExecutionData]
    public async Task
        GivenReprocessingMessageAndPrExeSucceedReturns400DueToPrInInSuccessStatus_WhenWorkerProcessesMessage_ThenMessageIsDeletedFromQueue(
            ExecutionQueueMessage mockedQueueMessage, PaymentTransactionDto paymentTransactionDto)
    {
        //Arrange
        var cts = new CancellationTokenSource();
        await using ComponentTestFixture fixture = new(_ => { });
        await fixture.MockPaymentRequestExecutionSucceedToReturnStatusCode(
            ExecutionSucceedResponseOverrideStubId, 400);
        var testPaymentRequestId = Guid.NewGuid();
        var mockEventDateTime = DateTime.UtcNow;

        //Set up queue message
        mockedQueueMessage.Status = nameof(StripeValidCompleteStatus.Succeeded);
        mockedQueueMessage.EventCreatedDateTime = mockEventDateTime;
        SetupMockedMessageWithRequiredCommonFieldsToDatabaseRecord(testPaymentRequestId, paymentTransactionDto, mockedQueueMessage);
        var sqsFormattedMessage = new List<Message> { new() { Body = JsonSerializer.Serialize(mockedQueueMessage) } };
        await fixture.PopulateSqsWithMessages(sqsFormattedMessage);

        //Set up DB record
        paymentTransactionDto.PaymentRequestId = testPaymentRequestId;
        paymentTransactionDto.EventCreatedDateTimeUtc = mockEventDateTime;
        paymentTransactionDto.Status = nameof(StripeValidCompleteStatus.Succeeded);
        await fixture.GetTestRepositoryInterface().InsertMockSubmittedAndMockProcessedPaymentTransaction(paymentTransactionDto);

        var worker = fixture.Services.GetRequiredService<Worker.Worker>();

        //Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();
        cts.Dispose();

        //Assert on queue
        var response = await fixture.GetMessagesFromQueue();
        Assert.Empty(response.Messages);

        //Cleanup
        await ComponentTestFixture.RemoveMappingOverrideOnPaymentRequestService(ExecutionSucceedResponseOverrideStubId);
        await fixture.PurgeQueue();
    }

    [Theory, PaymentExecutionData]
    public async Task
        GivenReprocessingMessageAndGetPaymentRequestFailsWhenInvestigatingExeSucceedBadRequest_WhenWorkerProcessesMessage_ThenMessageIsNotDeletedFromQueue(
            ExecutionQueueMessage mockedQueueMessage, PaymentTransactionDto paymentTransactionDto)
    {
        //Arrange
        var cts = new CancellationTokenSource();
        await using ComponentTestFixture fixture = new(_ => { });
        await fixture.MockPaymentRequestExecutionSucceedToReturnStatusCode(
            ExecutionSucceedResponseOverrideStubId, 400);
        await fixture.MockPaymentRequestGetPaymentRequestToReturnStatusCode(
            GetPaymentRequestResponseOverrideStubId, 500);
        var testPaymentRequestId = Guid.NewGuid();
        var mockEventDateTime = DateTime.UtcNow;

        //Set up queue message
        mockedQueueMessage.Status = nameof(StripeValidCompleteStatus.Succeeded);
        mockedQueueMessage.EventCreatedDateTime = mockEventDateTime;
        SetupMockedMessageWithRequiredCommonFieldsToDatabaseRecord(testPaymentRequestId, paymentTransactionDto, mockedQueueMessage);
        var sqsFormattedMessage = new List<Message> { new() { Body = JsonSerializer.Serialize(mockedQueueMessage) } };
        await fixture.PopulateSqsWithMessages(sqsFormattedMessage);

        //Set up DB record
        paymentTransactionDto.PaymentRequestId = testPaymentRequestId;
        paymentTransactionDto.EventCreatedDateTimeUtc = mockEventDateTime;
        paymentTransactionDto.Status = nameof(StripeValidCompleteStatus.Succeeded);
        await fixture.GetTestRepositoryInterface().InsertMockSubmittedAndMockProcessedPaymentTransaction(paymentTransactionDto);

        var worker = fixture.Services.GetRequiredService<Worker.Worker>();

        //Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await cts.CancelAsync();
        cts.Dispose();

        //Assert on queue
        var response = await fixture.GetMessagesFromQueue();
        Assert.Single(response.Messages);

        //Cleanup
        await ComponentTestFixture.RemoveMappingOverrideOnPaymentRequestService(GetPaymentRequestResponseOverrideStubId);
        await ComponentTestFixture.RemoveMappingOverrideOnPaymentRequestService(ExecutionSucceedResponseOverrideStubId);
        await fixture.PurgeQueue();
    }

    private static void SetupMockedMessageWithRequiredCommonFieldsToDatabaseRecord(
        Guid paymentRequestId, PaymentTransactionDto paymentTransactionDto, ExecutionQueueMessage mockedQueueMessage)
    {
        mockedQueueMessage.PaymentRequestId = paymentRequestId;
        mockedQueueMessage.PaymentProviderPaymentTransactionId = paymentTransactionDto.PaymentProviderPaymentTransactionId;
        mockedQueueMessage.ProviderServiceId = paymentTransactionDto.ProviderServiceId!.Value;
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using ComponentTestFixture fixture = new(_ => { });
        await fixture.PurgeQueue();
        await fixture.GetTestRepositoryInterface().WipeDb();
        var activeStubIds = new List<string>
        {
            ExecutionSucceedResponseOverrideStubId,
            GetPaymentRequestResponseOverrideStubId
        };
        await ComponentTestFixture.RemoveMappingOverrideOnPaymentRequestFromStubIdList(activeStubIds);
    }
}
