.SILENT:
# Run unit tests with code coverage
unit-test:
	echo "Running unit tests..."
	docker compose run -T \
		--rm \
		dotnet \
		bash service/scripts/unitTestsCoverage.sh

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
		-e REPOSITORY_CHECKOV_ARGUMENTS="--framework secrets --skip-check CKV_OPENAPI_4,CKV_OPENAPI_5,CKV_TF_1 --skip-path obj --skip-path bin" \
		megalinter

# Run component tests with code coverage and manage dependencies
component-test:start-local-dependencies
	echo "Running component tests..."
	docker compose run -T \
		--rm \
		-e ASPNETCORE_ENVIRONMENT=Development \
        -e DOTNET_ENVIRONMENT=Development \
        -e AWS_DEFAULT_REGION=ap-southeast-2 \
		-e AWS_ACCESS_KEY_ID=local \
		-e AWS_SECRET_ACCESS_KEY=local \
		-e AWS__ServiceURL=http://localstack:4566 \
		dotnet \
		bash service/scripts/componentTestsCoverage.sh Development ; \
		{ \
			exit_status=$$?; \
			$(MAKE) stop-local-dependencies; \
			exit $$exit_status; \
		}

# Regenerates the OpenAPI spec from the current API code
generate-api-spec:
	echo "Generating OpenAPI spec..."
	docker compose run -T \
		--rm \
		dotnet \
		dotnet build ./service/src/PaymentExecutionService.Api -t:GenerateOpenApiSpec -c Release

# Run the API
start-api:
	docker compose up \
		--wait --build \
		payment-execution-service

	echo "Started API"

# Stop the API
stop-api:
	docker compose kill \
		payment-execution-service

	echo "Stopped API"

# Run all local dependencies
start-local-dependencies:
	docker compose up \
		--wait \
		seq \
		seq-input-gelf \
		identity-mock \
		auth-service \
		run-migration \
		localstack \
        terraform-runner \
        payment-request-mock \
        stripe-execution-mock

	echo "Started local dependencies"
	echo "Waiting for terraform runner to finish..."
	docker compose wait terraform-runner
	echo "View and query your local environments aggregated logs via your browser at http://localhost:5341"

# Stop all local dependencies
stop-local-dependencies:
	docker compose kill \
		seq \
		seq-input-gelf \
		auth-service \
		identity-mock \
	    run-migration \
	    localstack \
        terraform-runner \
        payment-request-mock \
        stripe-execution-mock \
        payment-execution-db

	echo "Stopped local dependencies"

# Bundles load test scripts using Webpack.
build-load-tests:
	cd tests/PaymentExecutionService.LoadTests && npm install && npm run build

# Default to running load tests against a Development environment.
# Allowed values: development | uat
LOAD_TEST_ENV ?= development

# Runs load tests within a Docker container using k6.
avg-load-test: build-load-tests
	docker compose run \
		-e ENV=$(LOAD_TEST_ENV) \
		-e PR_IDENTITY_CLIENT_SECRET=$(PR_IDENTITY_CLIENT_SECRET) \
		-e PE_IDENTITY_CLIENT_SECRET=$(PE_IDENTITY_CLIENT_SECRET) \
		k6 run /tests/PaymentExecutionService.LoadTests/dist/example.test.js \
		-c /tests/PaymentExecutionService.LoadTests/src/env/$(LOAD_TEST_ENV)/config.load.json

stress-test: build-load-tests
	docker compose run \
		-e ENV=$(LOAD_TEST_ENV) \
		-e PR_IDENTITY_CLIENT_SECRET=$(PR_IDENTITY_CLIENT_SECRET) \
        -e PE_IDENTITY_CLIENT_SECRET=$(PE_IDENTITY_CLIENT_SECRET) \
		k6 run /tests/PaymentExecutionService.LoadTests/dist/example.test.js \
		-c /tests/PaymentExecutionService.LoadTests/src/env/$(LOAD_TEST_ENV)/config.stress.json

# Creates a secret in AWS Secrets Manager in Test
# See instructions in: https://accelerators.xero.dev/docs/guides/secrets-management/
create-secret-for-test:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:ap-southeast-2:333186395126:key/mrk-effa8200b23d41619d9455e37431d89d' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"334db7HzPYMT119B9go9X9"}]'

# Creates a secret in AWS Secrets Manager in UAT
# See instructions in: https://accelerators.xero.dev/docs/guides/secrets-management/
create-secret-for-uat:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:us-west-2:333186395126:key/mrk-49934c31ff914ae2b43295def2aef70c' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"334db7HzPYMT119B9go9X9"}]'

# Creates a secret in AWS Secrets Manager in Prod
# See instructions in: https://accelerators.xero.dev/docs/guides/secrets-management/
create-secret-for-prod:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:us-east-1:333186395126:key/mrk-f415b15ce76c49c482d2760c2af20de5' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"334db7HzPYMT119B9go9X9"}]'

		
provider-pact-test: start-local-dependencies
	echo "Running pact provider tests..."
	docker compose run -T \
		--rm \
		-e PACT_BROKER_TOKEN="${PACT_BROKER_TOKEN}" \
		-e PACT_BROKER_BASE_URL="${PACT_BROKER_BASE_URL}" \
		-e PACT_PROVIDER_VERSION="${PACT_PROVIDER_VERSION}" \
		-e PACT_GIT_BRANCH="${PACT_GIT_BRANCH}" \
		-e PACT_BUILD_URI="${PACT_BUILD_URI}" \
		-e PACT_URL="${PACT_URL}" \
		-e CI="${CI}" \
		dotnet \
		bash service/scripts/pactProviderTests.sh Development ; \
		{ \
			exit_status=$$?; \
			exit $$exit_status; \
		}
