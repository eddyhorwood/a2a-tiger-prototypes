variable "namespace" { 
  description = "Your Kora namespace"
  default     = "collecting-payments-execution"
  type = string
}

variable "service_name" {
  description = "Name of the service"
  default     = "payment-execution-worker"
  type        = string
}

variable "environment" {
  description = "Environment name (uat | production)"
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

variable "infra_kms_key_arn" {
  description = "The KMS key to encrypt your infrastructure"
  type        = string
}