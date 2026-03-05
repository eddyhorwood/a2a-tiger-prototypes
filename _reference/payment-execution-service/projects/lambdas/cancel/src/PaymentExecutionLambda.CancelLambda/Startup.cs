using Amazon.Lambda.Annotations;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentExecution.Domain;
using PaymentExecution.Repository;
using PaymentExecutionLambda.CancelLambda.Extensions;
using PaymentExecutionLambda.CancelLambda.Mappings;
using Serilog;
using System.Runtime.CompilerServices;
using PaymentExecution.FeatureFlagClient;
using PaymentExecution.StripeExecutionClient;

[assembly: InternalsVisibleTo("PaymentExecutionLambda.CancelLambda.UnitTests")]

namespace PaymentExecutionLambda.CancelLambda
{
    [LambdaStartup]
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            InitializeBootstrapLogger();
            var environment = GetEnvironmentName();
            var basePath = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.LambdaTaskRoot) ??
                           Directory.GetCurrentDirectory();

            // Fetch secrets from Secrets Manager if secret paths are provided
            if (environment != Constants.Environments.Development)
            {
                ResolveSecretsManagerSecrets(
                    [
                        Constants.Secrets.DbConnectionString, 
                        Constants.Secrets.LdClientSdkKey,
                        Constants.Secrets.IdentityClientSecret
                    ]);
            }

            var configuration = BuildConfiguration(environment, basePath);
            ConfigureLogging(services, configuration);
            RegisterApplicationServices(services, configuration);
        }

        private static void InitializeBootstrapLogger()
        {
            // Initialize a basic console logger for early startup
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        internal static string GetEnvironmentName()
        {
            var environment = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.Environment);

            // Default to "Development" if not set due to Lambda Annotations initialization timing
            // The actual environment will be set by Terraform and available at runtime
            if (string.IsNullOrEmpty(environment))
            {
                environment = Constants.Environments.Development;
            }

            return environment;
        }

        private static IConfiguration BuildConfiguration(string environment, string basePath)
        {
            var configEnvironment = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(environment.ToLower());
            
            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{configEnvironment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables(prefix: Constants.Configuration.OverridePrefix)
                .Build();
        }

        private static void ConfigureLogging(IServiceCollection services, IConfiguration configuration)
        {
            // Re-configure logger with full configuration
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            services.AddLogging(builder => builder.AddSerilog(dispose: true));
        }

        private static void RegisterApplicationServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConfiguration>(configuration);

            // TimeProvider (required by repository)
            services.AddSingleton(TimeProvider.System);
            services.AddDbIntegrationServices(configuration);
            services.AddAutoMapper(typeof(MappingProfile));
            services.AddCancellationValidationService();
            services.AddProviderIntegrationServices();
            services.ConfigMediatR(configuration);
            services.AddLaunchDarkly(configuration);
            services.ConfigureXeroIdentityClient(configuration);
            services.AddStripeExecutionClient(Constants.ClientName, configuration);
        }

        /// <summary>
        /// Resolves AWS Secrets Manager secrets to actual secret values.
        /// For each environment variable that contains a secret path, fetches the actual secret
        /// and replaces the environment variable value with the secret string.
        /// </summary>
        /// <param name="environmentVariableNames">Names of the environment variables to check and resolve</param>
        internal static void ResolveSecretsManagerSecrets(string[]? environmentVariableNames)
        {
            if (environmentVariableNames == null || environmentVariableNames.Length == 0)
            {
                return;
            }

            using var client = new AmazonSecretsManagerClient();

            foreach (var environmentVariableName in environmentVariableNames)
            {
                var secretId = Environment.GetEnvironmentVariable(environmentVariableName);

                if (string.IsNullOrEmpty(secretId))
                {
                    continue; // Not set, skip to next
                }

                var secretValue = GetSecretValueFromSecretsManager(client, environmentVariableName, secretId);
                Environment.SetEnvironmentVariable(environmentVariableName, secretValue);
            }
        }

        /// <summary>
        /// Retrieves and validates a secret value from AWS Secrets Manager.
        /// </summary>
        /// <param name="client">The AWS Secrets Manager client</param>
        /// <param name="environmentVariableName">The environment variable name (used for logging)</param>
        /// <param name="secretId">The secret ID (ARN or secret name/path)</param>
        /// <returns>The secret value as a string</returns>
        /// <exception cref="InvalidOperationException">Thrown if the secret cannot be retrieved or is invalid</exception>
        internal static string GetSecretValueFromSecretsManager(
            IAmazonSecretsManager client,
            string environmentVariableName,
            string secretId)
        {
            try
            {
                var request = new GetSecretValueRequest { SecretId = secretId };
                var response = Task.Run(
                        async () => await client.GetSecretValueAsync(request))
                    .GetAwaiter().GetResult();

                // Validate that we got a valid secret value
                if (string.IsNullOrWhiteSpace(response.SecretString))
                {
                    Log.Error(
                        "Retrieved secret value is null or empty for environment variable: {EnvironmentVariableName}, Secret ID: {SecretId}",
                        environmentVariableName, secretId);
                    throw new InvalidOperationException(
                        $"Retrieved secret value is null or empty for environment variable '{environmentVariableName}'. Secret ID: {secretId}");
                }

                return response.SecretString;
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Failed to retrieve secret from Secrets Manager for environment variable: {EnvironmentVariableName}, Secret ID: {SecretId}",
                    environmentVariableName, secretId);
                throw new InvalidOperationException(
                    $"Failed to retrieve secret from Secrets Manager for environment variable '{environmentVariableName}'. Secret ID: {secretId}",
                    ex);
            }
        }
    }
}
