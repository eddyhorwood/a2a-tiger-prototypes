# Launch darkly

## How we use LaunchDarkly

We use LaunchDarkly in this repository to manage feature flags, allowing us to control the release of new features dynamically.

### Active Feature Flags
- payment-execution-service-enabled:<br>
  kill switch for the service, at least until we exit pre-release and begin delivering feature behind flags.

## Local development

When running locally or in component tests, launch darkly is configured to use an in memory datasource, rather than connecting to the Xero Global Launch Darkly project.
This is accomplished by inserting a TestData.Datasource object into our LDClient service.
Customisation of this service is done in the LaunchDarklyDatasourceExtensions class.

If you wish to test using a specific flag configuration locally, you can do so by updating this class to set local dev flags "VariationForAll" as required.

## Configuration

There are several properties required to connect to LaunchDarkly in deployed environments.
Here's a table describing them and how they're managed.

| Parameter Name / Key                                                                             | Source          | Managed by | 
|--------------------------------------------------------------------------------------------------|-----------------|------------|
| collecting-payments-execution/payment-execution-service/launch-darkly-config/xero-global-sdk-key | Secrets Manager | Manually   |
