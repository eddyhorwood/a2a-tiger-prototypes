resource "aws_sqs_queue" "execution_sqs" {
  name                      = "collectingpayments-execution-payment-execution-queue-${var.environment}"
  kms_master_key_id         = var.execution_sqs_kms_key_arn
  message_retention_seconds = var.execution_sqs_messages_retention_in_seconds
}

resource "aws_sqs_queue" "execution_dlq" {
  name = "collectingpayments-execution-payment-execution-dlq-${var.environment}"
  redrive_allow_policy = jsonencode({
    redrivePermission = "byQueue",
    sourceQueueArns   = [aws_sqs_queue.execution_sqs.arn]
  })
  kms_master_key_id = var.execution_sqs_kms_key_arn
}

resource "aws_sqs_queue_redrive_policy" "execution_queue_redrive_policy" {
  queue_url = aws_sqs_queue.execution_sqs.id
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.execution_dlq.arn
    maxReceiveCount     = var.execution_sqs_redrive_max_receive_count
  })
}

data "aws_iam_policy_document" "sqs_access_policy_data" {
  statement {
    actions   = ["SQS:GetQueueAttributes"]
    resources = ["arn:aws:sqs:${local.aws_region}:${var.aws_account_id}:collectingpayments-execution-payment-execution-queue-${var.environment}"]
    effect    = "Allow"
    principals {
      type        = "AWS"
      identifiers = [local.keda_operator_role]
    }
  }
}

resource "aws_sqs_queue_policy" "sqs_access_policy" {
  depends_on = [
    aws_sqs_queue.execution_sqs
  ]
  queue_url = aws_sqs_queue.execution_sqs.id
  policy    = data.aws_iam_policy_document.sqs_access_policy_data.json
}
