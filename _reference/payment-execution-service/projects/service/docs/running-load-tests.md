# Load testing

This repository includes configuration for conducting load testing using [k6](https://k6.io/docs/), [Xero's recommended tool for load testing](https://xero.atlassian.net/wiki/spaces/QE/pages/269865011315/Performance+testing+strategy).

## Getting Started

### Running a Load Test

#### Local development

The load test project is located within `tests/PaymentExecutionService.LoadTests`.

To initiate a load test against the service running in a local environment, run the following command:

```sh
make avg-load-test PR_IDENTITY_CLIENT_SECRET=secret PE_IDENTITY_CLIENT_SECRET=<secret>
```

To run the load test against a different environment, specify the `LOAD_TEST_ENV` variable:

```sh
make avg-load-test LOAD_TEST_ENV=uat  PR_IDENTITY_CLIENT_SECRET=secret PE_IDENTITY_CLIENT_SECRET=<secret>
```

The above Make commands spin up a k6 Docker container and run the provided test `example.test.ts` using the configuration located in the directory for the specified environment. See the [Makefile](../Makefile) for a full list of load test tasks.

#### Github Actions Integration

The generated Github Actions pipeline includes workflows to run load tests against the Uat environment. Workflows can run be triggered per test type (e.g. load, stress).

**Note**: Avoid committing to the default branch while a load test run is in progress, as this may redeploy the service currently being tested, thereby invalidating the load test. Before initiating a load test, communicate with all relevant teams so they are aware of a possible increase in traffic.

### Workload Configuration

The project utilizes [k6 Scenarios](https://k6.io/docs/using-k6/scenarios/) to customise the workload configuration for specific test types. The workload configuration for each test type is defined in JSON files in the `src/<env>` sub-directory e.g. `tests/PaymentExecutionService.LoadTests/src/env/development/config.load.json`.
