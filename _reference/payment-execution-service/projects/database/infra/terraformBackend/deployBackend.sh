#!/usr/bin/env bash

set -euo pipefail

ENVIRONMENT=$1

[[ "$ENVIRONMENT" != "test" && "$ENVIRONMENT" != "uat" && "$ENVIRONMENT" != "production" ]] && echo "ENVIRONMENT must be (test|uat|production)" && exit 1

AWS_CALLER_IDENTITY=$(aws sts get-caller-identity)
[[ $AWS_CALLER_IDENTITY != *"Developer"* ]] && echo "Assume a Developer role" && exit 1

case "$ENVIRONMENT" in
test)
    AWS_REGION="ap-southeast-2"
    KMS_KEY_ARN="arn:aws:kms:ap-southeast-2:333186395126:key/mrk-d056f8a301b2467496ae8ca4cfe14298"
    ;;
uat)
    AWS_REGION="us-west-2"
    KMS_KEY_ARN="arn:aws:kms:us-west-2:333186395126:key/mrk-b883401ceb07443e846ee9999581032b"
    ;;
production)
    AWS_REGION="us-east-1"
    KMS_KEY_ARN="arn:aws:kms:us-east-1:333186395126:key/mrk-b6707da6297841878502555ec9d1111e"
    ;;
esac

CUR_DIR=$(
    cd "$(dirname "${BASH_SOURCE[0]}")"
    pwd -P
)
STACK_PATH="$CUR_DIR/terraformBackendStack.yaml"

aws cloudformation deploy \
    --template-file "$STACK_PATH" \
    --stack-name "payment-execution-database-tf-backend-${ENVIRONMENT}" \
    --region "$AWS_REGION" \
    --parameter-overrides \
    "Env=${ENVIRONMENT}" \
    "ServiceName=payment-execution-database" \
    "KMSKeyId=${KMS_KEY_ARN}" \
    --tags \
    "uuid=svZ2goYQnrGkthqfsaUeBb" \
    --no-fail-on-empty-changeset
