# Cancel Payment Request Message Scripts

This directory contains scripts to send valid cancel payment request messages to the UAT SQS queue for testing the cancel lambda functionality.

## Prerequisites

1. **AWS CLI configured** with UAT environment access
2. **Proper IAM permissions** to send messages to the SQS queue
3. **Queue exists** in the UAT environment: `collectingpayments-execution-cancel-execution-queue-uat`

## Scripts Available

### 1. Bash Script (Linux/macOS)

```bash
./send_cancel_message_uat.sh
```

### 2. PowerShell Script (Windows)

```powershell
.\send_cancel_message_uat.ps1 [-TenantId <tenant-id>] [-Region <aws-region>]
```

## Message Format

The scripts send a message with the following structure:

**Message Body (PaymentCancellationRequest):**

```json
{
  "paymentRequestId": "<generated-uuid>",
  "providerType": "Stripe",
  "cancellationReason": "User requested cancellation"
}
```

**Message Attributes:**

- `Xero-Tenant-Id`: Tenant ID (UUID format)
- `Xero-Correlation-Id`: Correlation ID for tracing

## Examples

### Using Bash script with default values:

```bash
./send_cancel_message_uat.sh
```

### Using PowerShell with custom tenant ID:

```powershell
.\send_cancel_message_uat.ps1 -TenantId "123e4567-e89b-12d3-a456-426614174000"
```

### Using PowerShell with custom parameters:

```powershell
.\send_cancel_message_uat.ps1 -TenantId "123e4567-e89b-12d3-a456-426614174000" -Region "us-west-2"
```

## Default Values

- **Tenant ID**: `550e8400-e29b-41d4-a716-446655440000` (example UUID)
- **AWS Region**: `ap-southeast-2`
- **Provider Type**: `Stripe`
- **Cancellation Reason**: `User requested cancellation`

## Troubleshooting

### Common Issues:

1. **Queue not found**

   - Verify the queue exists in the UAT environment
   - Check if you're using the correct AWS region

2. **Access Denied**

   - Ensure your IAM user/role has permissions to send messages to the SQS queue
   - Verify you're authenticated to the correct AWS account

3. **AWS credentials not configured**
   - Run `aws configure` to set up your credentials
   - Or use environment variables: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`

### Required IAM Permissions:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["sqs:GetQueueUrl", "sqs:SendMessage"],
      "Resource": "arn:aws:sqs:*:*:collectingpayments-execution-cancel-execution-queue-uat"
    }
  ]
}
```

## Monitoring

After sending a message, you can monitor the lambda execution through:

1. **AWS CloudWatch Logs** - Check the cancel lambda log group
2. **AWS SQS Console** - Monitor queue metrics and message counts
3. **AWS X-Ray** - If tracing is enabled, track the message processing

## Queue Information

- **Queue Name**: `collectingpayments-execution-cancel-execution-queue-uat`
- **Environment**: UAT
- **Expected Lambda**: Cancel Execution Lambda function
- **Dead Letter Queue**: `collectingpayments-execution-cancel-execution-dlq-uat`
