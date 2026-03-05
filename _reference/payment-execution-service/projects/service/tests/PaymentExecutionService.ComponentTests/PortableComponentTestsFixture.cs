using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Xero.Identity.Authentication.HealthChecks.Testing;

namespace PaymentExecutionService.ComponentTests;

public class PortableComponentTestsFixture(string environmentName = "Development") : ComponentTestsFixture
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environmentName);
        builder.ConfigureTestServices(services =>
        {
            // Custom config
            // This will automatically return healthy for all configured Identity authorities
            services.AddXeroIdentityHealthChecksTesting();
        });

        var mockApiDependencies = Convert.ToBoolean(new ConfigurationBuilder()
            .AddJsonFile("appsettings.ComponentTests.json").Build().GetValue<bool>("MockApiDependencies"));

        if (mockApiDependencies)
        {
            builder.UseSetting("InternalServices:PaymentsExecutionService:BaseUrl", WireMockServer.Url);
        }
    }
}
