.SILENT:

# Run unit tests with code coverage
unit-test:
	echo "Running unit tests..."
	bash scripts/unitTestsCoverage.sh

# Run component tests with code coverage (starts and stops dependencies automatically)
component-test: start-local-dependencies
	echo "Running component tests..."
	bash scripts/componentTestsCoverage.sh; \
	EXIT_CODE=$$?; \
	$(MAKE) stop-local-dependencies; \
	exit $$EXIT_CODE

# Run consumer pact tests
consumer-pact-test:
	echo "Running consumer pact tests..."
	dotnet test tests/PaymentExecutionLambda.CancelLambda.ConsumerPactTests/PaymentExecutionLambda.CancelLambda.ConsumerPactTests.csproj

# Run all tests (unit + component + pact)
test-all: unit-test component-test consumer-pact-test
	echo "All tests complete!"

# Build the lambda project for Release
build:
	echo "Building lambda project (Release)..."
	dotnet build src/PaymentExecutionLambda.CancelLambda/PaymentExecutionLambda.CancelLambda.csproj -c Release

# Build the lambda project for Debug (used by test-lambda)
build-debug:
	echo "Building lambda project (Debug)..."
	dotnet build src/PaymentExecutionLambda.CancelLambda/PaymentExecutionLambda.CancelLambda.csproj -c Debug

# Clean build artifacts
clean:
	echo "Cleaning build artifacts..."
	dotnet clean src/PaymentExecutionLambda.CancelLambda/PaymentExecutionLambda.CancelLambda.csproj
	rm -rf src/PaymentExecutionLambda.CancelLambda/bin
	rm -rf src/PaymentExecutionLambda.CancelLambda/obj
	rm -rf tests/PaymentExecutionLambda.CancelLambda.UnitTests/bin
	rm -rf tests/PaymentExecutionLambda.CancelLambda.UnitTests/obj
	rm -rf tests/PaymentExecutionLambda.CancelLambda.ComponentTests/bin
	rm -rf tests/PaymentExecutionLambda.CancelLambda.ComponentTests/obj
	rm -rf tests/PaymentExecutionLambda.CancelLambda.ConsumerPactTests/bin
	rm -rf tests/PaymentExecutionLambda.CancelLambda.ConsumerPactTests/obj
	rm -rf tests/PaymentExecutionLambda.CancelLambda.ConsumerPactTests/pacts
	rm -rf tests/results

# Restore NuGet packages
restore:
	echo "Restoring NuGet packages..."
	dotnet restore src/PaymentExecutionLambda.CancelLambda/PaymentExecutionLambda.CancelLambda.csproj

# Package lambda for deployment (includes build)
package:
	echo "Packaging lambda for deployment..."
	cd src/PaymentExecutionLambda.CancelLambda && \
	dotnet lambda package -c Release -o ../../bin/release/PaymentExecutionLambda.CancelLambda.zip

# Start the Lambda Test Tool for local testing (builds Debug first)
test-lambda: build-debug
	echo "Starting Lambda Test Tool..."
	echo "Lambda Test Tool will be available at http://localhost:5050"
	cd src/PaymentExecutionLambda.CancelLambda && \
	dotnet lambda-test-tool-8.0

# Start all local dependencies (database + identity-mock + stripe-execution-mock)
start-local-dependencies:
	echo "Starting local dependencies..."
	docker compose up \
		--wait \
		--force-recreate \
		-d \
		payment-execution-db \
		run-migration \
		identity-mock \
		stripe-execution-mock
	echo ""
	echo "✅ Local dependencies started successfully!"
	echo "  • Database: localhost:5432 (payment_execution_db)"
	echo "  • Identity mock: http://localhost:5003"
	echo "  • Stripe mock: http://localhost:12112"
	echo ""

# Stop all local dependencies
stop-local-dependencies:
	docker compose down
	echo "Stopped and removed local dependency containers"

# Restart local dependencies (useful during development)
restart-local-dependencies: stop-local-dependencies start-local-dependencies
	echo "Local dependencies restarted"

# Lint repository
lint:
	echo "Linting repository..."
	docker compose run -T \
		--rm \
		megalinter

# Lint repository and fix all auto-fixable issues
lint-and-fix:
	echo "Linting repository and fixing all auto-fixable issues..."
	docker compose run -T \
		--rm \
		-e APPLY_FIXES="all" \
		megalinter

# Run a Checkov secrets check against repository
checkov-secrets-only:
	echo "Running a Checkov secrets check against repository..."
	docker compose run -T \
		--rm \
		-e ENABLE_LINTERS="REPOSITORY_CHECKOV" \
		-e REPOSITORY_CHECKOV_ARGUMENTS="--framework secrets --skip-check CKV_OPENAPI*,CKV_TF_1 --skip-path obj --skip-path bin" \
		megalinter

# Creates a secret in AWS Secrets Manager in Test
# See instructions in: https://accelerators.xero.dev/docs/guides/secrets-management/
create-secret-for-test:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:ap-southeast-2:333186395126:key/mrk-0189429af5434cfeaa2fc09577836f56' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"bfifma5ebpfcbq4cb9a5ud"}]'

# Creates a secret in AWS Secrets Manager in UAT
# See instructions in: https://accelerators.xero.dev/docs/guides/secrets-management/
create-secret-for-uat:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:us-west-2:333186395126:key/mrk-ce1ccc0db8b74bec90237092efb4931e' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"bfifma5ebpfcbq4cb9a5ud"}]'

# Creates a secret in AWS Secrets Manager in Production
# See instructions in: https://accelerators.xero.dev/docs/guides/secrets-management/
create-secret-for-prod:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:us-east-1:333186395126:key/mrk-0ba3f90baa79430097b54952973bcb5f' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"bfifma5ebpfcbq4cb9a5ud"}]'

