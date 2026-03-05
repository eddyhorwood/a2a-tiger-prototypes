.SILENT:

# Creates a secret in AWS Secrets Manager in Test
# See instructions in: https://accelerators.xero.dev/docs/guides/secrets-management/
create-secret-for-test:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:ap-southeast-2:333186395126:key/mrk-d2383b3a51f34c9998a86533a954eca0' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"svZ2goYQnrGkthqfsaUeBb"}]'

# Creates a secret in AWS Secrets Manager in UAT
# See instructions in: https://accelerators.xero.dev/docs/guides/secrets-management/
create-secret-for-uat:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:us-west-2:333186395126:key/mrk-b8ebf1ec60304c63897cdeaa3e98761c' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"svZ2goYQnrGkthqfsaUeBb"}]'

# Creates a secret in AWS Secrets Manager in Prod
# See instructions in: https://accelerators.xero.dev/docs/guides/secrets-management/
create-secret-for-prod:
	echo "Starting KMS encryption of secret (ensure you have logged into AWS SSO)..."
	aws secretsmanager create-secret \
		--name '$(NAME)' \
		--description '$(DESCRIPTION)' \
		--kms-key-id 'arn:aws:kms:us-east-1:333186395126:key/mrk-3f409b4773434143ba42dcce583c6157' \
		--secret-string '$(SECRET)' \
		--tags '[{"Key":"uuid","Value":"svZ2goYQnrGkthqfsaUeBb"}]'

# Runs Flyway migration locally usind docker compose
run-migration:
	echo "Running Flyway migration..." 
	docker compose -f ./docker-compose.yml run -T \
		--rm \
		-e FLYWAY_URL='jdbc:postgresql://$(DB_ENDPOINT):5432/payment_execution_db' \
		-e FLYWAY_USER="payment_execution_schema_manager" \
        -e FLYWAY_PASSWORD='$(DB_PASSWORD)' \
        -e FLYWAY_DEFAULT_SCHEMA=payment_execution \
        -e FLYWAY_CLEAN_DISABLED=false \
		run-migration clean migrate -connectRetries=60

# Runs bootstrap sql script against the database
run-bootstrap:
	psql -h '$(DB_ENDPOINT)' -p 5432 -d postgres -U administrator -W -f ./bootstrap.sql

# Connects to the database
connect-db:
	psql -h '$(DB_ENDPOINT)' -p 5432 -d payment_execution_db -U $(DB_USER) -W

db-up-and-run-migration: 
	echo "Starting payment execution database..."
	docker compose up run-migration --remove-orphans

db-down:
	echo "Stopping payment execution database..."
	docker compose kill \
		payment-execution-db \
		run-migration
	echo "Stopped payment execution database."

clean-test: 
	echo "Cleaning test database..."
	./scripts/clean_test.sh
	echo "Cleaned test database."