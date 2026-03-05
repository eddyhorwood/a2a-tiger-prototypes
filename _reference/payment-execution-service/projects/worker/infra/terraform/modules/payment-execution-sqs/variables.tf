variable "execution_sqs_messages_retention_in_seconds" {
  type        = number
  description = "The number of seconds a message in the execution even queue."
  default     = 86400 //24 hours
}

variable "execution_sqs_redrive_max_receive_count" {
  type        = number
  description = "the number of times a execution can receive a message from a source queue before it is moved to a dead-letter queue."
  default     = 2
}

variable "environment" {
  type        = string
  description = "Environment name (local | test| uat | production)"
  default     = "local"
}

variable "aws_account_id" {
  type        = string
  description = "AWS account ID"
}

variable "execution_sqs_kms_key_arn" {
  type        = string
  description = "ARN for kms key to encrypt queue messages."
}

locals {

  env_to_aws_region = {
    "test"       = "ap-southeast-2"
    "uat"        = "us-west-2"
    "production" = "us-east-1"
  }
  aws_region = local.env_to_aws_region[var.environment]

  env_to_keda_iam_roles = {
    "test"       = "arn:aws:iam::620528608035:role/ap-southeast-2-k8s-paas-test-rua-keda-operator"
    "uat"        = "arn:aws:iam::620528608035:role/us-west-2-k8s-paas-uat-rua-keda-operator"
    "production" = "arn:aws:iam::620528608035:role/us-east-1-k8s-paas-prod-rua-keda-operator"
  }
  keda_operator_role = local.env_to_keda_iam_roles[var.environment]
}
