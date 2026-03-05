#!/usr/bin/env bash
set -euo pipefail

## Get the current AWS account ID to ensure we are in the test environment
ASSUMED_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
if [ "$ASSUMED_ACCOUNT_ID" != "590184096169" ]; then
    echo "Assumed role in incorrect account: $ASSUMED_ACCOUNT_ID while trying to clean test database. Please do NOT attempt to clean UAT or Prod datastores ;)" && exit 1
fi

## Get variables needed to connect to the test database
SERVER_ENDPOINT=$(aws rds describe-db-cluster-endpoints \
    --db-cluster-identifier collectingpayments-execution-execution-db-test \
    --query 'DBClusterEndpoints[?EndpointType==`WRITER`].Endpoint' \
    --output text)
SCHEMA_MANAGER_PASSWORD=$(aws secretsmanager get-secret-value \
    --secret-id collecting-payments-execution/collecting-payments-execution-payment-execution-database/payment-execution-db/schema-manager-password \
    --query SecretString \
    --output text)
APPLICATION_USER_PASSWORD=$(aws secretsmanager get-secret-value \
    --secret-id collecting-payments-execution/collecting-payments-execution-payment-execution-database/payment-execution-db/connection-string \
    --query SecretString \
    --output text | grep -o 'Password=[^;]*' | cut -d'=' -f2
)

## replace temp password with password resolved from secret 
sed -i '' -e "s|temp_p@ssw0rd|$APPLICATION_USER_PASSWORD|g" migration_scripts/V2__CreatePaymentExecutionUser.sql

## Run flyway clean against the test database
docker compose -f ./docker-compose.yml run \
    -e FLYWAY_URL="jdbc:postgresql://$SERVER_ENDPOINT:5432/payment_execution_db" \
    -e FLYWAY_USER="payment_execution_schema_manager" \
    -e FLYWAY_PASSWORD="$SCHEMA_MANAGER_PASSWORD" \
    -e FLYWAY_LOCATIONS=filesystem:/flyway/migration_scripts,filesystem:/flyway/callback_scripts \
    -e FLYWAY_DEFAULT_SCHEMA=payment_execution \
    -e FLYWAY_CLEAN_DISABLED=false \
    run-migration clean migrate -connectRetries=60

## return local file back to original version to avoid leaving change in git staging area. 
sed -i '' -e "s|$APPLICATION_USER_PASSWORD|temp_p@ssw0rd|g" migration_scripts/V2__CreatePaymentExecutionUser.sql
