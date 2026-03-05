.SILENT:

# Run unit tests with code coverage
unit-test:
	echo "Running unit tests..."
	docker compose run -T \
		--rm \
		dotnet \
		bash scripts/unitTestsCoverage.sh
	
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

# Run the Worker
start-worker: 
	docker compose up \
		--wait --build \
		payment-execution-worker

	echo "Started Worker"

# Stop the Worker
stop-worker: 
	docker compose kill \
		payment-execution-worker

	echo "Stopped Worker"

# Run all local dependencies
start-local-dependencies: 
	docker compose up \
		--wait \
		seq \
		seq-input-gelf \
		localstack \
		run-migration \
		terraform-runner \
		identity-mock \
		payment-execution-db \
		payment-request-mock

	echo "Started local dependencies"
	echo "View and query your local environments aggregated logs via your browser at http://localhost:5341"

# Stop all local dependencies
stop-local-dependencies: 
	docker compose kill \
		seq \
		seq-input-gelf \
		localstack \
		run-migration \
		terraform-runner \
		identity-mock \
		payment-execution-db

	echo "Stopped local dependencies"

create-secret-for-test:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:ap-southeast-2:333186395126:key/mrk-c6cfafdbad6b470bbf4fbe44f40f55c4' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"BdzazQewZtyQbUfCK7XYxc"}]'

# Creates a secret in AWS Secrets Manager in UAT
# See instructions in: https://accelerators.xero.dev/docs/guides/secrets-management/
create-secret-for-uat:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:us-west-2:333186395126:key/mrk-02d6309915624425bb2c221f1420e06d' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"BdzazQewZtyQbUfCK7XYxc"}]'

# Creates a secret in AWS Secrets Manager in Prod
# See instructions in: https://accelerators.xero.dev/docs/guides/secrets-management/
create-secret-for-prod:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:us-east-1:333186395126:key/mrk-99c14f3773964c1a87a5f03e1c08f00b' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"BdzazQewZtyQbUfCK7XYxc"}]'
