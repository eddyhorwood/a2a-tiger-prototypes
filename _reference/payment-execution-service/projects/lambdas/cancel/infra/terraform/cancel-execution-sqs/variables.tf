variable "environment" {
  description = "Environment name (test | uat | production)"
  default     = "test"
  type        = string
}

variable "cancel_execution_sqs_messages_retention_in_seconds" {
  type        = number
  description = "The number of seconds a message retained in the cancel execution queue."
  default     = 86400 //24 hours
}

variable "cancel_execution_sqs_redrive_max_receive_count" {
  type        = number
  description = "the number of times a lambda can receive a message from a source queue before it is moved to a dead-letter queue."
  default     = 2
}

variable "execution_sqs_kms_key_arn" {
  type        = string
  description = "ARN for kms key to encrypt queue messages."
}
