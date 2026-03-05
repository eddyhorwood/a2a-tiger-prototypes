variable "namespace" {
  description = "Your Kora namespace"
  default     = "cp-payment-execution"
  type        = string
}

variable "service_name" {
  description = "Name of the service"
  default     = "payment-execution-service"
  type        = string
}

variable "deployment_role_name" {
  description = "Name of the deployment role"
  default     = "payment-execution-deployment-role"
  type        = string
}

variable "environment" {
  description = "Environment name (test | uat | production)"
  type        = string
}

variable "region" {
  description = "AWS Region"
  type        = string
}

variable "aws_account_id" {
  description = "AWS Account ID"
  type        = string
}

variable "oidc_role_arn" {
  description = "OIDC Role Arn"
  type        = string
}

variable "artifactory_password_arn" {
  description = "AWS SSM Arn for Artifactory Password"
  type        = string
}

variable "artifactory_password_kms_arn" {
  description = "KMS Key which encrytpts the Artifactory Password"
  type        = string
}

variable "database_connection_string_secret_name" {
  description = "Name of the database connection string secret"
  default     = "collecting-payments-execution/collecting-payments-execution-payment-execution-database/payment-execution-db/connection-string"
  type        = string
}

variable "retool_password_secret_name" {
  description = "Name of the database connection string secret for retool"
  default     = "collecting-payments-execution/collecting-payments-execution-payment-execution-database/payment-execution-db/connection-string-retool"
  type        = string
}

variable "schema_manager_password_secret_name" {
  description = "Password of schema manager"
  default     = "collecting-payments-execution/collecting-payments-execution-payment-execution-database/payment-execution-db/schema-manager-password"
  type        = string
}

variable "database_name" {
  description = "Name of the database"
  default     = "payment-execution-database"
  type        = string
}

variable "db_secret_kms_key_arn" {
  description = "KMS Key Arn which is used for db secrets"
  type        = string
}

variable "worker_name" {
  description = "Name of the worker"
  default     = "payment-execution-worker"
  type        = string
}

variable "cancel_lambda_name" {
  description = "Name of the lambda"
  default     = "cancel-execution-lambda"
  type        = string
}

variable "infra_kms_key_arn" {
  description = "The KMS key to encrypt infrastructure backend"
  type        = string
}
variable "queue_name" {
  description = "Name of the queue"
  default     = "collectingpayments-execution-payment-execution-queue"
  type        = string
}

variable "dlq_name" {
  description = "Name of the dlq"
  default     = "collectingpayments-execution-payment-execution-dlq"
  type        = string
}

variable "cancel_queue_name" {
  description = "Name of the queue"
  default     = "collectingpayments-execution-cancel-execution-queue"
  type        = string
}

variable "cancel_dlq_name" {
  description = "Name of the dlq"
  default     = "collectingpayments-execution-cancel-execution-dlq"
  type        = string
}

variable "pagerduty_cloudwatch_endpoint_secret_name" {
  description = "Name of secret holding endpoint which is invoked by CloudWatch via SNS to raise PagerDuty incidents"
  default     = "collecting-payments-execution/pagerduty-cloudwatch-endpoint"
  type        = string
}

variable "default_kms_key_arn" {
  description = "The KMS key to encrypt infrastructure backend"
  type        = string
}
