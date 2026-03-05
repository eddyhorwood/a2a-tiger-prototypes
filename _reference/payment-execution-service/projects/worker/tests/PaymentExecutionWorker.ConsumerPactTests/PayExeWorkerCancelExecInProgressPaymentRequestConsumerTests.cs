using PactNet;
using PactNet.Output.Xunit;
using PaymentExecution.PaymentRequestClient.ConsumerPactTest;
using Xunit.Abstractions;
namespace PaymentExecutionWorker.ConsumerPactTests;

public class PayExeWorkerCancelExecInProgressPaymentRequestConsumerTests(ITestOutputHelper output) : CancelExecutionInProgressPaymentRequestConsumerTests(
    Pact.V4(
        Constants.PacticipantConsumerName,
        Constants.PacticipantPaymentRequestProviderName,
        new PactConfig() { Outputters = [new XunitOutput(output)], LogLevel = PactLogLevel.Information }
    ).WithHttpInteractions());
