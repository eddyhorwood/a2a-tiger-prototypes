# Logging and Monitoring

## Logging

### Serilog

The worker uses [Serilog](https://xero.atlassian.net/wiki/search?text=serilog&spaces=techmap) for logging. Serilog configuration is defined in `src/PaymentExecutionWorker.Worker/appsettings.{environment}.json`. Logs are written to standard output.

### Sumo Logic

 When deployed on PaaS Kubernetes, logs are forwarded to [Sumo Logic](https://xero.atlassian.net/wiki/search?text=sumologic&spaces=techmap).

 Sumo Logic source categories for each environment are configured in [the worker's Helm values files](../infra/k8s).

 [Query and filter the worker's logs through Sumo Logic](https://service.us2.sumologic.com/ui/#/search/create?id=lLnCUIO4X2gBzG7AIsb5UpYkdGkVfN95wlqSS1xq):

 ```
 _sourceCategory = collecting_payments_execution/payment_execution_worker/uat/app/serilog/json
 ```

### Structured logging

The structured log is preferred over string interpolation, as it allows the log to be parsed and searched more easily. See [Serilog: Writing Log Events](https://github.com/serilog/serilog/wiki/Writing-Log-Events) for further details.

## Monitoring

### New Relic agent configuration

The agent is installed and configured in [the worker's Docker container image](../src/PaymentExecutionWorker.Worker/Dockerfile).

The APM agent's base configuration is located in the [New Relic configuration file](../src/PaymentExecutionWorker.Worker/newrelic.config). Environment-specific configuration is injected through environment variables specified in [the worker's Helm values files](../infra/k8s).
