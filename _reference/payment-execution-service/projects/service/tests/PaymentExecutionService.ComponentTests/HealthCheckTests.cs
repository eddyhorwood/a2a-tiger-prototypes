using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaymentExecution.Repository;
using Xunit;

namespace PaymentExecutionService.ComponentTests;

public class HealthCheckTests(ComponentTestsFixture fixture) : IClassFixture<ComponentTestsFixture>
{
    [Fact]
    public async Task GivenReadyEndpoint_WhenServiceIsHealthy_ReturnsOk()
    {
        // Arrange
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Secret", "TERCES");

        // Act
        var response = await client.GetAsync("ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenReadyEndpoint_WhenPaymentExecutionDBIsUnhealthy_ReturnsError()
    {
        // Arrange
        var dbRepository = new Mock<IPaymentTransactionRepository>(MockBehavior.Strict);
        dbRepository.SetupGet(x => x.HealthCheck().Result).Returns(false);
        var client = fixture.WithWebHostBuilder(b =>
        {
            b.ConfigureTestServices(x =>
            {
                x.AddTransient(_ => dbRepository.Object);
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Add("X-Secret", "TERCES");

        // Act
        var response = await client.GetAsync("ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GivenPingEndpoint_WhenInvoked_ReturnsOk()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenSecurePingEndpoint_WhenAuthenticated_ReturnsOk()
    {
        // Arrange
        var client = fixture.CreateAuthenticatedClientWithTenantId();

        // Act
        var response = await client.GetAsync("secure-ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenSecurePingEndpoint_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("secure-ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenHealthCheckEndpoint_WhenSendingSecretHeader_ThenReturnsOk()
    {
        // Arrange
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Secret", "TERCES");

        // Act
        var response = await client.GetAsync("healthcheck");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
