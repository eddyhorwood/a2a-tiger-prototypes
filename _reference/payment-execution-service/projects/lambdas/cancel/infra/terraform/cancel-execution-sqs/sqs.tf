resource "aws_sqs_queue" "cancel_execution_sqs" {
  name                      = "collectingpayments-execution-cancel-execution-queue-${var.environment}"
  kms_master_key_id         = var.execution_sqs_kms_key_arn
  message_retention_seconds = var.cancel_execution_sqs_messages_retention_in_seconds
}

resource "aws_sqs_queue" "cancel_execution_dlq" {
  name = "collectingpayments-execution-cancel-execution-dlq-${var.environment}"
  redrive_allow_policy = jsonencode({
    redrivePermission = "byQueue",
    sourceQueueArns   = [aws_sqs_queue.cancel_execution_sqs.arn]
  })
  kms_master_key_id = var.execution_sqs_kms_key_arn
}

resource "aws_sqs_queue_redrive_policy" "cancel_execution_queue_redrive_policy" {
  queue_url = aws_sqs_queue.cancel_execution_sqs.id
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.cancel_execution_dlq.arn
    maxReceiveCount     = var.cancel_execution_sqs_redrive_max_receive_count
  })
}
