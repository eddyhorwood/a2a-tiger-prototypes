using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("PaymentExecutionLambda.CancelLambda.ComponentTests.ComponentTestInitialiser", "PaymentExecutionLambda.CancelLambda.ComponentTests")]

namespace PaymentExecutionLambda.CancelLambda.ComponentTests;

public class ComponentTestInitialiser : XunitTestFramework
{
    private const string DotnetEnvironment = "DOTNET_ENVIRONMENT";

    public ComponentTestInitialiser(IMessageSink messageSink) : base(messageSink)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DotnetEnvironment)))
        {
            Environment.SetEnvironmentVariable(DotnetEnvironment, "Development");
        }
    }
}
