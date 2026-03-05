output "cancel_execution_sqs_arn" {
  description = "ARN of the cancel execution SQS queue"
  value       = aws_sqs_queue.cancel_execution_sqs.arn
}
