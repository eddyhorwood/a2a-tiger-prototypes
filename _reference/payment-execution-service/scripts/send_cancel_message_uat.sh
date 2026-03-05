#!/bin/bash

# Script to send a valid cancel payment request message to UAT SQS queue
# Usage: ./send_cancel_message_uat.sh

set -e

# Configuration
QUEUE_NAME="collectingpayments-execution-cancel-execution-queue-uat"
AWS_REGION="ap-southeast-2"  # Update this if different
TENANT_ID="550e8400-e29b-41d4-a716-446655440000"  # Example tenant ID - replace with actual
CORRELATION_ID="$(uuidgen)"

# Generate sample data
PAYMENT_REQUEST_ID="$(uuidgen)"

# Create the message body (PaymentCancellationRequest)
MESSAGE_BODY=$(cat <<EOF
{
  "paymentRequestId": "${PAYMENT_REQUEST_ID}",
  "providerType": "Stripe",
  "cancellationReason": "User requested cancellation"
}
EOF
)

echo "Sending cancel payment request message to UAT queue..."
echo "Queue: ${QUEUE_NAME}"
echo "Payment Request ID: ${PAYMENT_REQUEST_ID}"
echo "Tenant ID: ${TENANT_ID}"
echo "Correlation ID: ${CORRELATION_ID}"
echo ""

# Get queue URL
QUEUE_URL=$(aws sqs get-queue-url --queue-name "${QUEUE_NAME}" --region "${AWS_REGION}" --output text --query 'QueueUrl')

if [ -z "$QUEUE_URL" ]; then
    echo "Error: Could not find queue ${QUEUE_NAME}"
    echo "Make sure you have access to the UAT environment and the queue exists"
    exit 1
fi

echo "Queue URL: ${QUEUE_URL}"
echo ""

# Send message to SQS
aws sqs send-message \
    --queue-url "${QUEUE_URL}" \
    --message-body "${MESSAGE_BODY}" \
    --message-attributes '{
        "Xero-Tenant-Id": {
            "StringValue": "'${TENANT_ID}'",
            "DataType": "String"
        },
        "Xero-Correlation-Id": {
            "StringValue": "'${CORRELATION_ID}'",
            "DataType": "String"
        }
    }' \
    --region "${AWS_REGION}"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Successfully sent cancel payment request message to UAT queue!"
    echo "Message details:"
    echo "  - Payment Request ID: ${PAYMENT_REQUEST_ID}"
    echo "  - Provider Type: Stripe"
    echo "  - Cancellation Reason: User requested cancellation"
    echo "  - Tenant ID: ${TENANT_ID}"
    echo "  - Correlation ID: ${CORRELATION_ID}"
else
    echo ""
    echo "❌ Failed to send message to queue"
    exit 1
fi
