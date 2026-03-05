variable "environment" {
  type        = string
  description = "Environment name (test| uat | production)"
  default     = "uat"

  validation {
    condition     = contains(["test", "uat", "production"], var.environment)
    error_message = "Valid value is one of the following:test or uat or production."
  }
}

variable "secrets_kms_arn" {
  description = "kms key used to encrypt/decrypt infrastructure resources"
  type        = string
}

variable "db_secrets_kms_arn" {
  description = "kms key used to encrypt/decrypt database connection string"
  default     = "uat"
  type        = string
}
