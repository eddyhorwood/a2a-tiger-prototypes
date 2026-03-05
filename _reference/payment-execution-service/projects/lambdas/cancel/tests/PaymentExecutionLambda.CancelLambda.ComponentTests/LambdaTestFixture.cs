using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentExecution.Common;
using PaymentExecution.FeatureFlagClient;
using PaymentExecution.Repository;
using PaymentExecution.Repository.Models;
using PaymentExecution.TestUtilities;

namespace PaymentExecutionLambda.CancelLambda.ComponentTests;

public class LambdaTestFixture : IDisposable, IAsyncDisposable
{
    private IServiceProvider ServiceProvider { get; }

    public LambdaTestFixture()
    {
        // Configure services using Lambda's Startup
        var services = new ServiceCollection();
        var startup = new Startup();
        startup.ConfigureServices(services);

        // Register test-specific services
        services.AddScoped<IPaymentTransactionComponentTestRepository,
                           PaymentTransactionComponentTestRepository>();

        ServiceProvider = services.BuildServiceProvider();

        ResetFeatureFlagsToDefault();
    }

    public Function CreateLambdaFunction()
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<Function>>();
        var serviceScopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        var mapper = ServiceProvider.GetRequiredService<AutoMapper.IMapper>();
        return new Function(logger, serviceScopeFactory, mapper);
    }

    public async Task InsertMockPaymentTransactionAsync(PaymentTransactionDto paymentTransactionDto, Guid organisationId)
    {
        using var scope = ServiceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentTransactionComponentTestRepository>();
        await repository.InsertMockPaymentTransaction(paymentTransactionDto, organisationId);
    }

    public async Task<PaymentTransactionDto?> GetPaymentTransactionAsync(Guid paymentRequestId)
    {
        using var scope = ServiceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
        var result = await repository.GetPaymentTransactionsByPaymentRequestId(paymentRequestId);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task DeletePaymentTransactionsByOrgAsync(Guid organisationId)
    {
        using var scope = ServiceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
        await repository.DeleteAllDataByOrganisationId(organisationId);
    }

    public void SetProviderCancellationFeatureFlag(bool enabled)
    {
        var mockFeatureSource = ServiceProvider.GetService<IMockFeatureSource>();
        if (mockFeatureSource != null)
        {
            var dataSource = mockFeatureSource.GetDataSource();
            dataSource.Update(dataSource.Flag(ExecutionConstants.FeatureFlags.EnableProviderCancellation.Name)
                .VariationForAll(enabled));
        }
    }

    public void ResetFeatureFlagsToDefault()
    {
        var mockFeatureSource = ServiceProvider.GetService<IMockFeatureSource>();
        mockFeatureSource?.ResetToDefaultLocalDevValues();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (ServiceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
