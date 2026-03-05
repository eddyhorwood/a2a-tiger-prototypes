variable "environment" {
  description = "Environment name (test | uat | production)"
  default     = "test"
  type        = string
}

variable "cancel_execution_lambda_name" {
  default = "cancel-execution-lambda"
  type    = string
}

variable "vpc_id" {
  description = "The VPC ID"
  type        = string
}
