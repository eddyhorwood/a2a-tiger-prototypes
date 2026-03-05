using PactNet;
using PactNet.Output.Xunit;
using PaymentExecution.StripeExecutionClient.ConsumerPactTests;
using Xunit.Abstractions;

namespace PaymentExecutionLambda.CancelLambda.ConsumerPactTests;

public class CancelLambdaStripeCancelConsumerTests(ITestOutputHelper output)
    : StripeCancelConsumerPactTest(Pact.V4(
            Constants.PacticipantConsumerName,
            Constants.PacticipantStripeExecutionProviderName,
            new PactConfig { Outputters = [new XunitOutput(output)], LogLevel = PactLogLevel.Information })
        .WithHttpInteractions());

