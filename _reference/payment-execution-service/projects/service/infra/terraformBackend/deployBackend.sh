#!/usr/bin/env bash

set -euo pipefail

ENVIRONMENT=$1

[[ "$ENVIRONMENT" != "test" && "$ENVIRONMENT" != "uat" && "$ENVIRONMENT" != "production" ]] && echo "ENVIRONMENT must be (uat|production)" && exit 1

AWS_CALLER_IDENTITY=$(aws sts get-caller-identity)
[[ $AWS_CALLER_IDENTITY != *"Developer"* ]] && echo "Assume a Developer role" && exit 1
ASSUMED_ACCOUNT=$(aws sts get-caller-identity --query Account)
ASSUMED_ACCOUNT_ID=$(eval echo "$ASSUMED_ACCOUNT")

case "$ENVIRONMENT" in
    test)
        AWS_REGION="ap-southeast-2"
        KMS_KEY_ARN="arn:aws:kms:ap-southeast-2:333186395126:key/mrk-d056f8a301b2467496ae8ca4cfe14298"
        AWS_ACCOUNT_ID="590184096169"
        ;;
    uat)
        AWS_REGION="us-west-2"
        KMS_KEY_ARN="arn:aws:kms:us-west-2:333186395126:key/mrk-b883401ceb07443e846ee9999581032b"
        AWS_ACCOUNT_ID="471112828694"
        ;;
    production)
        AWS_REGION="us-east-1"
        KMS_KEY_ARN="arn:aws:kms:us-east-1:333186395126:key/mrk-b6707da6297841878502555ec9d1111e"
        AWS_ACCOUNT_ID="211125634578"
        ;;
esac

CUR_DIR=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
STACK_PATH="$CUR_DIR/terraformBackendStack.yaml"

if [ "$ASSUMED_ACCOUNT_ID" != "$AWS_ACCOUNT_ID" ]; then
    echo "Assumed role in incorrect account: $ASSUMED_ACCOUNT_ID while trying to deploy to $ENVIRONMENT. Assume role with permission to deploy infrastructure in account: $AWS_ACCOUNT_ID" && exit 1
fi

aws cloudformation deploy \
    --template-file "$STACK_PATH" \
    --stack-name "payment-execution-service-tf-backend-${ENVIRONMENT}" \
    --region "$AWS_REGION" \
    --parameter-overrides \
        "Env=${ENVIRONMENT}" \
        "ServiceName=payment-execution-service" \
        "KMSKeyId=${KMS_KEY_ARN}" \
    --tags \
        "uuid=334db7HzPYMT119B9go9X9" \
    --no-fail-on-empty-changeset