#!/usr/bin/env bash

set -euo pipefail

TERRAFORM_OPERATION=$1
ENVIRONMENT=$2

[[ -z $TERRAFORM_OPERATION ]] && echo "TERRAFORM_OPERATION empty" && exit 1
[[ "$ENVIRONMENT" != "test" && "$ENVIRONMENT" != "uat" && "$ENVIRONMENT" != "production" ]] && echo "ENVIRONMENT must be (test|uat|production)" && exit 1

case "$ENVIRONMENT" in
test)
    AWS_ACCOUNT_ID="590184096169"
    ;;
uat)
    AWS_ACCOUNT_ID="471112828694"
    ;;
production)
    AWS_ACCOUNT_ID="211125634578"
    ;;
esac

ASSUMED_ACCOUNT=$(aws sts get-caller-identity --query Account)
ASSUMED_ACCOUNT_ID=$(eval echo "$ASSUMED_ACCOUNT")

if [ "$ASSUMED_ACCOUNT_ID" != "$AWS_ACCOUNT_ID" ]; then
    echo "Assumed role in incorrect account: $ASSUMED_ACCOUNT_ID while trying to deploy to $ENVIRONMENT. Assume role with permission to deploy infrastructure in account: $AWS_ACCOUNT_ID" && exit 1
fi

terraform \
    init \
    -backend-config="environment/$ENVIRONMENT.backend" \
    -reconfigure

# shellcheck disable=SC2086
terraform \
    $TERRAFORM_OPERATION \
    -var-file="environment/$ENVIRONMENT.tfvars"
