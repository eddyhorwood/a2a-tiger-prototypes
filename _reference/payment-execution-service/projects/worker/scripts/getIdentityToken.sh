#!/usr/bin/env bash
set +euxo pipefail

env=$1
scope=${2:-default}

echo "Retrieving identity token for test client in $env"
client_id="xero_collecting-payments-execution_payment-execution-worker"
case $env in
local)
  token_url="http://localhost:5003/connect/token"
  client_secret="secret"
  client_id="local_caller"
  ;;
test)
  token_url="https://identity.fringe4.xero-test.com/connect/token"
  client_secret="secret"
  ;;
uat)
  token_url="https://integration-identity.xero-uat.com/connect/token"
  client_secret=$(aws secretsmanager get-secret-value --secret "collecting-payments-execution/collecting-payments-execution-stripe-payments-service/testing-identity-client-secret" --query "SecretString" --output text)
  ;;
*)
  echo "Unrecognized environment $env, please provide one of test, uat"
  exit 2
  ;;
esac

client_scope=xero_collecting-payments_payment-request-service
case $scope in
payment_exec_submit)
  client_id="xero_collecting-payments-execution_payment-execution-service-consumer"
  client_scope="xero_collecting-payments-execution_payment-execution-service.submit"
  ;;
**)
  client_scope="$client_scope.$scope"
  ;;
esac
echo "Requesting token for scope: $client_scope"
combined="$client_id:$client_secret"


unameOut="$(uname -s)"
case "${unameOut}" in
    Linux*)     echo "Detected Linux" && base64=$(echo -n "$combined" | base64 -w0);;
    Darwin*)    base64=$(echo -n "$combined" | base64);;
    *)          echo "Unsupported script for OS" && exit 1;;
esac

authorisationHeader="Basic $base64"

curl --request POST $token_url \
  --header "Authorization: $authorisationHeader" \
  --header "Content-Type: application/x-www-form-urlencoded" \
  --data "grant_type=client_credentials&scope=$client_scope"
