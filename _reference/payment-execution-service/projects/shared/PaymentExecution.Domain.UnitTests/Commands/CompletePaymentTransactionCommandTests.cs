using Moq;
using PaymentExecution.Domain.Commands;
using PaymentExecution.Domain.Models;
using PaymentExecution.SqsIntegrationClient.Service;
namespace PaymentExecution.Domain.UnitTests.Commands;

public class CompletePaymentTransactionCommandTests
{
    private readonly CompletePaymentTransactionCommandHandler _completePaymentHandler;
    private readonly Mock<IExecutionQueueService> _sqsService;

    public CompletePaymentTransactionCommandTests()
    {
        _sqsService = new Mock<IExecutionQueueService>();
        _completePaymentHandler = new CompletePaymentTransactionCommandHandler(_sqsService.Object);
    }

    [Fact]
    public async Task GivenValidRequest_WhenCompletePaymentTransactionCommandHandler_ThenReturnsCommandResponse()
    {
        CompletePaymentTransactionCommand cmd = CreateValidCommand();

        _sqsService.Setup(m => m.SendMessageAsync(It.IsAny<ExecutionQueueMessage>(), cmd.XeroCorrelationId, cmd.XeroTenantId)).Returns(Task.CompletedTask);

        var result = await _completePaymentHandler.Handle(cmd, default);

        _sqsService.Verify(m => m.SendMessageAsync(It.IsAny<ExecutionQueueMessage>(), cmd.XeroCorrelationId, cmd.XeroTenantId), Times.Once);
        Assert.Equivalent(Task.CompletedTask, result);
    }

    [Fact]
    public async Task GivenValidRequest_WhenSqsErrorOccurs_ThenErrorIsThrown()
    {
        CompletePaymentTransactionCommand cmd = CreateValidCommand();

        _sqsService.Setup(m => m.SendMessageAsync(It.IsAny<ExecutionQueueMessage>(), cmd.XeroCorrelationId, cmd.XeroTenantId)).ThrowsAsync(new Exception());

        await Assert.ThrowsAsync<Exception>(async () => await _completePaymentHandler.Handle(cmd, default));
        _sqsService.Verify(m => m.SendMessageAsync(It.IsAny<ExecutionQueueMessage>(), cmd.XeroCorrelationId, cmd.XeroTenantId), Times.Once);
    }

    private static CompletePaymentTransactionCommand CreateValidCommand()
    {
        return new CompletePaymentTransactionCommand
        {
            Message = new ExecutionQueueMessage()
            {
                PaymentRequestId = Guid.NewGuid(),
                ProviderServiceId = Guid.NewGuid(),
                Fee = 5,
                FeeCurrency = "AUD",
                ProviderType = ProviderType.Stripe.ToString(),
                PaymentProviderPaymentTransactionId = "pi_123456",
                PaymentProviderPaymentReferenceId = "ch_123456",
                Status = TerminalStatus.Succeeded.ToString()
            },
            XeroCorrelationId = Guid.NewGuid().ToString(),
            XeroTenantId = Guid.NewGuid().ToString()
        };
    }
}
