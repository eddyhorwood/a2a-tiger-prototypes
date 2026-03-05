# Cancel Execution Lambda

This Lambda function is responsible for cancelling abandoned payment requests in the Payment Execution Service. The project uses Lambda Annotations framework with dependency injection.

## Local Development and Testing

### Prerequisites

1. **Install AWS Lambda Test Tool for .NET 8**:

   ```bash
   dotnet tool install -g Amazon.Lambda.TestTool-8.0
   ```

2. **Install Amazon Lambda Tools**:
   ```bash
   dotnet tool install -g Amazon.Lambda.Tools
   ```

### Quick Start with Make

The easiest way to run the Lambda Test Tool is using the Makefile:

```bash
make test-lambda
```

This command will:

- Build the project in Debug configuration
- Start the Lambda Test Tool at `http://localhost:5050`

### Running with Local Dependencies

The Lambda function requires a PostgreSQL database connection. Start the local dependencies before testing:

**Start Local Dependencies:**

```bash
make start-local-dependencies
```

This will:

- Start a PostgreSQL container on port `5432`
- Create the `payment_execution_db` database
- Run all Flyway migrations
- Display the connection string

**Stop Local Dependencies:**

```bash
make stop-local-dependencies
```

**Connection String:**

```
Server=localhost;Database=payment_execution_db;User Id=payment_execution_user;Password=temp_p@ssw0rd;
```

This connection string is already configured in `appsettings.json` and will be used automatically when testing locally.

### Using the Test Tool Interface

Once the test tool starts:

- The browser will open at `http://localhost:5050`
- **Function Selection**: Choose `PaymentExecutionLambda.CancelLambda::PaymentExecutionLambda.CancelLambda.Function_Handler_Generated::Handler`
- **Sample Request**: Use empty JSON `{}` or any valid JSON payload
- **Execute**: Click "Execute Function" to test locally
- **View Logs**: Check console output for structured logging with correlation IDs

### Other Available Make Commands

**Development:**

- `make build` - Build the project (Release configuration)
- `make build-debug` - Build the project (Debug configuration)
- `make clean` - Clean build artifacts and test results
- `make restore` - Restore NuGet packages

**Testing:**

- `make unit-test` - Run unit tests with code coverage
- `make component-test` - Run component tests (automatically starts/stops Docker dependencies)
- `make test-all` - Run all tests (unit + component)

**Deployment:**

- `make package` - Package lambda for AWS deployment

### Manual Method (Alternative)

If you prefer to run commands manually:

1. **Build the project**:

   ```bash
   dotnet build src/PaymentExecutionLambda.CancelLambda/PaymentExecutionLambda.CancelLambda.csproj -c Debug
   ```

2. **Start the Lambda Test Tool**:
   ```bash
   cd src/PaymentExecutionLambda.CancelLambda
   dotnet lambda-test-tool-8.0
   ```

## Configuration

### Environment Variables

- `ENVIRONMENT` - Deployment environment (test, uat, production)
- `LAMBDA_TASK_ROOT` - Lambda runtime directory (auto-set by AWS)

### Configuration Files

- `appsettings.json` - Base Serilog configuration
- `appsettings.{ENVIRONMENT}.json` - Environment-specific settings

## Testing

This project includes comprehensive test coverage with both unit and component tests.

### Unit Tests

Located in `tests/PaymentExecutionLambda.CancelLambda.UnitTests/`

**Run unit tests:**

```bash
make unit-test
```

Unit tests focus on:

- Individual method logic
- Extension methods
- Mapping configurations
- Message parsing and validation

### Component Tests

Located in `tests/PaymentExecutionLambda.CancelLambda.ComponentTests/`

**Run component tests:**

```bash
make component-test
```

This command automatically:

1. **Starts** Docker-based dependencies:
   - PostgreSQL database on port `5432`
   - Identity mock on port `5003`
   - Stripe execution mock (WireMock) on port `12112`
2. **Runs** component tests with code coverage
3. **Stops** and removes all dependencies after completion

**Manual dependency management (optional):**

If you want to start dependencies manually for debugging:

```bash
make start-local-dependencies
```

Stop dependencies:

```bash
make stop-local-dependencies
```

**Key Features:**

- ✅ **Real database** - Uses PostgreSQL container for integration testing
- ✅ **Docker-based mocks** - Identity and Stripe APIs mocked via Docker
- ✅ **End-to-end validation** - Tests complete Lambda handler flow
- ✅ **Realistic scenarios** - Tests with actual SQS events and database interactions

Component tests validate:

- SQS message processing with valid/invalid payloads
- Database CRUD operations with real PostgreSQL
- Stripe Execution API integration (mocked with WireMock)
- Identity authentication flow (mocked)
- Error handling and retry logic
- Tenant ID and correlation ID validation

### Consumer Pact Tests

Located in `tests/PaymentExecutionLambda.CancelLambda.ConsumerPactTests/`

Validates the contract with Stripe Execution Service.

**Run consumer pact tests:**

```bash
make consumer-pact-test
```

### Run All Tests

```bash
make test-all
```

This runs unit, component, and consumer pact tests with code coverage.
