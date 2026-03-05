# Logging and Monitoring

## Logging

### Serilog

The service uses [Serilog](https://xero.atlassian.net/wiki/search?text=serilog&spaces=techmap) for logging. Serilog configuration is defined in `src/PaymentExecutionService.Api/appsettings.{environment}.json`. Logs are written to standard output.

### Sumo Logic

 When deployed on PaaS Kubernetes, logs are forwarded to [Sumo Logic](https://xero.atlassian.net/wiki/search?text=sumologic&spaces=techmap).

 Sumo Logic source categories for each environment are configured in [the service's Helm values files](../infra/k8s).

 [Query and filter the services logs through Sumo Logic](https://service.us2.sumologic.com/ui/#/search/create?query=_sourceCategory%20%3D%20collecting_payments_execution%2Fpayment_execution_service%2Ftest%2Fapp%2Fserilog%2Fjson%0A&startTime=-1d&endTime=now):

 ```
 _sourceCategory = collecting_payments_execution/payment_execution_service/test/app/serilog/json
 ```
### Structured logging

The structured log is preferred over string interpolation, as it allows the log to be parsed and searched more easily. See [`GreetingService.cs`](./src/PaymentExecutionService.Api/Services/GreetingService.cs) for an example and [Serilog: Writing Log Events](https://github.com/serilog/serilog/wiki/Writing-Log-Events) for further details.

### Identity logs

To view authentication logs, see the [Identity documentation](https://github.dev.xero.com/pages/identity/identity.docs/docs/troubleshooting/#accessing-identity-logs).

## Monitoring

This service uses New Relic Application Performance Monitoring in UAT and Production. Each environment has an APM dashboard.

### New Relic agent configuration

The agent is installed and configured in [the service's Docker container image](../src/PaymentExecutionService.Api/Dockerfile).

The APM agent's base configuration is located in the [New Relic configuration file](../src/PaymentExecutionService.Api/newrelic.config). Environment-specific configuration is injected through environment variables specified in [the service's Helm values files](../infra/k8s).

### View your APM Dashboards

[Query to view your services' dashboards in UAT and Production](https://one.newrelic.com/nr1-core?filters=%28domain%20IN%20%28%27APM%27%2C%20%27EXT%27%29%20AND%20type%20IN%20%28%27APPLICATION%27%2C%20%27SERVICE%27%29%29%20AND%20%28name%20LIKE%20%27payment-execution-service%27%20OR%20id%20%3D%20%27payment-execution-service%27%20OR%20domainId%20%3D%20%27payment-execution-service%27%29%20AND%20%28name%20LIKE%20%27test%27%20OR%20id%20%3D%20%27test%27%20OR%20domainId%20%3D%20%27test%27%29%20OR%20%28name%20LIKE%20%27uat%27%20OR%20id%20%3D%20%27uat%27%20OR%20domainId%20%3D%20%27uat%27%29%20OR%20%28name%20LIKE%20%27prod%27%20OR%20id%20%3D%20%27prod%27%20OR%20domainId%20%3D%20%27prod%27%29&). 
