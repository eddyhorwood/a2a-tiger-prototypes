# Cancel Execution Lambda Consumer Pact Tests

This project contains consumer pact tests for the Cancel Execution Lambda, which validates the contract between the Cancel Execution Lambda (consumer) and Stripe Execution Service (provider).

## Overview

Consumer pact tests ensure that the Cancel Execution Lambda correctly implements the expected contract when calling external services. These tests run from the consumer's perspective and generate pact files that can be verified by the provider.

## Test Structure

The project inherits test cases from the shared `PaymentExecution.StripeExecutionClient.ConsumerPactTests` project:

- **`CancelLambdaStripeCancelConsumerTests`**: Concrete implementation of `StripeCancelConsumerPactTest` that tests the cancel endpoint contract.

## What Gets Tested

The pact tests validate the business contract scenarios:

1. **Successful Cancellation** - Cancelling a payment in cancellable status returns 200 OK
2. **Already Succeeded** - Cancelling an already succeeded payment returns 409 Conflict
3. **Already Cancelled** - Cancelling an already cancelled payment returns 200 OK (idempotent)
4. **Not Found** - Cancelling a non-existent payment returns 404 Not Found

**Note**: Infrastructure concerns like header validation (401, 400 for missing headers) are not part of consumer pact tests as they're typically handled at the API Gateway or middleware level, not by the provider's business logic.

## Pact Participants

- **Consumer**: `CancelExecutionLambda`
- **Provider**: `StripeExecutionService`

## Running the Tests

### Locally
```bash
# From the cancel lambda directory
cd projects/lambdas/cancel
make consumer-pact-test  # If defined in Makefile

# Or directly
dotnet test tests/PaymentExecutionLambda.CancelLambda.ConsumerPactTests
```

### Generated Pacts

After running the tests, pact files are generated in:
```
tests/PaymentExecutionLambda.CancelLambda.ConsumerPactTests/pacts/
```

These pact files can be:
- Published to a Pact Broker
- Verified by the Stripe Execution Service provider tests
- Used for contract documentation

## CI/CD Integration

These tests should run as part of the Cancel Execution Lambda CI/CD pipeline and publish pacts to the Pact Broker before deployment.

## References

- [Pact Documentation](https://docs.pact.io/)
- [PactNet Library](https://github.com/pact-foundation/pact-net)
- [Xero Pact Guidelines](https://accelerators.xero.dev/docs/api-accelerator/guides/pact-provider-tests/)

