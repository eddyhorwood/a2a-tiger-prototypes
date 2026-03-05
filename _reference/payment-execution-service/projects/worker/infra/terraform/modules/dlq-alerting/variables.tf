variable "sqs_queue_name" {
  description = "Name of the SQS DLQ to monitor"
  type        = string
}

variable "sns_kms_key_arn" {
  description = "ARN of key used to encrypt sns topic message at rest"
  type        = string
}
