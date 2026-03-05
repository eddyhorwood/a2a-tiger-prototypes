using PactNet;
using PactNet.Output.Xunit;
using PaymentExecution.StripeExecutionClient.ConsumerPactTests;
using Xunit.Abstractions;

namespace PaymentExecutionService.ConsumerPactTests.StripeExecution;

public class PayExeServiceSubmitStripeExeConsumerTests(ITestOutputHelper output)
    : StripeSubmitConsumerPactTest(Pact.V4(
            Constants.PacticipantConsumerName,
            Constants.PacticipantStripeExecutionProviderName,
            new PactConfig { Outputters = [new XunitOutput(output)], LogLevel = PactLogLevel.Information })
        .WithHttpInteractions());
