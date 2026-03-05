variable "environment" {
  description = "Environment name (uat | production)"
  default     = "uat"
  type        = string
}

variable "secrets_kms_arn" {
  description = "kms key used to encrypt/decrypt service secrets"
  default     = "uat"
  type        = string
}

variable "newrelic_kms_arn" {
  description = "kms key used to encrypt/decrypt new relic keys"
  default     = "uat"
  type        = string
}

variable "db_secrets_kms_arn" {
  description = "kms key used to encrypt/decrypt database connection string"
  default     = "uat"
  type        = string
}
