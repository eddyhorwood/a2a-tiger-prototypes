# PowerShell script to send a valid cancel payment request message to UAT SQS queue
# Usage: .\send_cancel_message_uat.ps1

param(
    [string]$TenantId = "550e8400-e29b-41d4-a716-446655440000",  # Example tenant ID - replace with actual
    [string]$Region = "ap-southeast-2"  # Update this if different
)

# Configuration
$QueueName = "collectingpayments-execution-cancel-execution-queue-uat"
$CorrelationId = [System.Guid]::NewGuid().ToString()

# Generate sample data
$PaymentRequestId = [System.Guid]::NewGuid().ToString()

# Create the message body (PaymentCancellationRequest)
$MessageBody = @{
    paymentRequestId = $PaymentRequestId
    providerType = "Stripe"
    cancellationReason = "User requested cancellation"
} | ConvertTo-Json -Compress

Write-Host "Sending cancel payment request message to UAT queue..." -ForegroundColor Green
Write-Host "Queue: $QueueName"
Write-Host "Payment Request ID: $PaymentRequestId"
Write-Host "Tenant ID: $TenantId"
Write-Host "Correlation ID: $CorrelationId"
Write-Host ""

try {
    # Get queue URL
    $QueueUrl = aws sqs get-queue-url --queue-name $QueueName --region $Region --output text --query 'QueueUrl'
    
    if ([string]::IsNullOrWhiteSpace($QueueUrl)) {
        throw "Could not find queue $QueueName"
    }

    Write-Host "Queue URL: $QueueUrl"
    Write-Host ""

    # Create message attributes
    $MessageAttributes = @{
        "Xero-Tenant-Id" = @{
            StringValue = $TenantId
            DataType = "String"
        }
        "Xero-Correlation-Id" = @{
            StringValue = $CorrelationId
            DataType = "String"
        }
    } | ConvertTo-Json -Compress

    # Send message to SQS
    $Result = aws sqs send-message `
        --queue-url $QueueUrl `
        --message-body $MessageBody `
        --message-attributes $MessageAttributes `
        --region $Region

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Successfully sent cancel payment request message to UAT queue!" -ForegroundColor Green
        Write-Host "Message details:"
        Write-Host "  - Payment Request ID: $PaymentRequestId"
        Write-Host "  - Provider Type: Stripe"
        Write-Host "  - Cancellation Reason: User requested cancellation"
        Write-Host "  - Tenant ID: $TenantId"
        Write-Host "  - Correlation ID: $CorrelationId"
    } else {
        throw "Failed to send message to queue"
    }
}
catch {
    Write-Host ""
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure you have:"
    Write-Host "1. AWS CLI configured with UAT environment access"
    Write-Host "2. Proper permissions to access the SQS queue"
    Write-Host "3. The queue exists in the specified region"
    exit 1
}
