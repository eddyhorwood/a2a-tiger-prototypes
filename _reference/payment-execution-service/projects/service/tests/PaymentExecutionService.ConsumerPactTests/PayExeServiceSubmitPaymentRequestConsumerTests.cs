using PactNet;
using PactNet.Output.Xunit;
using PaymentExecution.PaymentRequestClient.ConsumerPactTest;
using Xunit.Abstractions;

namespace PaymentExecutionService.ConsumerPactTests;

public class PayExeServiceSubmitPaymentRequestConsumerTests(ITestOutputHelper output)
    : SubmitPaymentRequestConsumerPactTest(
        Pact.V4(
            Constants.PacticipantConsumerName,
            Constants.PacticipantPaymentRequestProviderName,
            new PactConfig { Outputters = [new XunitOutput(output)], LogLevel = PactLogLevel.Information }
        ).WithHttpInteractions());
