namespace PaymentExecutionLambda.CancelLambda;

public static class Constants
{
    public const string ClientName = "cancel-execution-lambda";

    public static class Secrets
    {
        public const string DbConnectionString = "Override_DataAccess__PaymentExecutionDB__ConnectionString";
        public const string LdClientSdkKey = "Override_LaunchDarkly__SdkKey";
        public const string IdentityClientSecret = "Override_Identity__Client__ClientSecret";
    }

    public static class EnvironmentVariables
    {
        public const string Environment = "ENVIRONMENT";
        public const string LambdaTaskRoot = "LAMBDA_TASK_ROOT";
    }

    public static class Environments
    {
        public const string Development = "Development";
    }

    public static class Configuration
    {
        public const string OverridePrefix = "Override_";
    }
}
