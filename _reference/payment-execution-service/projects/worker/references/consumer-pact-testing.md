# Pact testing for Payment Execution Worker

This project contains the pact consumer tests for the Payment Execution Worker.

## Running the tests

The tests can be run just as any other unit tests, the only difference being they will generate the Pact files in the `pacts` folder.

## Test Structure

These tests provide only the Pact configuration for the PaymentExecutionWorker consumer. See for example  the [PaymentExecutionWorkerFailConsumerTests](../tests/PaymentExecutionWorker.ConsumerPactTests/PayExeWorkerFailPaymentRequestConsumerTests.cs)

The actual body of these tests are in their respective integration consumer test project. See for example the 'generic' [FailConsumerTests](../../shared/PaymentExecution.PaymentRequestClient.ConsumerPactTest/FailPaymentRequestConsumerTest.cs) 

This allows different consumers to re-use the same test interactions, while defining their own pact consumer config.  

## Interacting with the Pact Broker from local

To interact with the Pact Broker from your local machine, you can use the `pact-broker` CLI.

You will need to have run the pact tests locally to generate the Pact files, and also grab your personal token from [Pactflow](https://xero.pactflow.io/settings/api-tokens).

Then, you can run commands against the broker using the pact-cli docker container, for example the following command will publish the pacts to the broker with a harcoded version of 0.0.1 and branch local-test:
```shell
docker run -v ./tests/PaymentExecutionWorker.ConsumerPactTests/pacts:/pacts pactfoundation/pact-cli pact-broker publish /pacts --consumer-app-version 0.0.1 --branch local-test --broker-base-url https://xero.pactflow.io --broker-token  <your-token-from-pact-flow>
```
Command to list the pact versions
```shell
docker run -v ./tests/PaymentExecutionWorker.ConsumerPactTests/pacts:/pacts pactfoundation/pact-cli pact-broker list_latest_pact_versions  --broker-base-url https://xero.pactflow.io --broker-token  <your-token-from-pact-flow>
```
Command to get pact version for a specific consumer 
```shell
docker run -v ./tests/PaymentExecutionWorker.ConsumerPactTests/pacts:/pacts pactfoundation/pact-cli pact-broker describe_version  --broker-base-url https://xero.pactflow.io --broker-token  <your-token-from-pact-flow> --pacticipant=PaymentExecutionService 
```
## Provider states

There are provider states configured by the various providers we verify against.
See for example the `PaymentRequestServiceProviderState` class in the `PaymentExecution.PaymentRequestClient.ConsumerPactTests` project
Other services which provide pact producers may define different provider state methods.
