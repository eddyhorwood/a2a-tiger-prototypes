/*
Pass AWS backend configuration via CLI args e.g.
  terraform init `
    -backend-config="bucket=<s3_bucket_name>" `
    -backend-config="region=<aws_region_name>" `
    -backend-config="dynamodb_table=<dynamodb_lock_table_name>"
*/

terraform {
  required_version = "~> 1.8.2"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.47.0"
    }
    kora = {
      source  = "tf.xero.dev/paas/kora"
      version = "1.7.0"
    }
  }
}

provider "kora" {}

provider "aws" {
  region = local.aws_region
  default_tags {
    # Partially Satisfies XREQ-93
    tags = {
      uuid = local.cortex_tag
    }
  }
  ignore_tags {
    key_prefixes = ["managed:ownership:"]
  }
}

terraform {
  backend "s3" {
    # 'bucket', 'dynamodb_table', 'kms_key_id', and 'region' will be specified on the command line
    key     = "infra/terraform/terraform.tfstate"
    encrypt = true
  }
}

resource "random_password" "database_password" {
  length  = 16
  special = true
}

resource "kora_resource" "aurora_postgres_aurora_postgres" {
  type      = "aurora-postgres"
  version   = "aurora-postgres-1.0"
  namespace = local.namespace
  #
  secret {
    name  = "administrator_password"
    value = random_password.database_password.result
  }

  configuration = jsonencode({
    name                        = "collectingpayments-execution-payment-execution-db-${(var.environment) == "production" ? "prod" : (var.environment)}"
    region                      = local.aws_region
    vpc_id                      = local.aws_vpc_id
    account_id                  = local.aws_account_id
    component_uuid              = local.cortex_tag
    engine_version              = "16.8"
    prevent_destroy             = true
    allow_minor_version_upgrade = true
    max_capacity                = local.env_to_max_capacity[var.environment]
    min_capacity                = local.env_to_min_capacity[var.environment]
    backup_window               = "11:00-11:59"
    maintenance_window          = "Sun:12:00-Sun:15:00"
    ingress_security_group_rules = [
      {
        cidr_block  = "10.0.0.0/8"
        description = ""
      },
      {
        cidr_block  = "172.16.0.0/12"
        description = ""
      }
    ]
    dre_consulted      = false
    instance_class     = "db.serverless"
    ca_cert_identifier = "rds-ca-rsa2048-g1"
    
    # enabled to support dspm https://xero.atlassian.net/wiki/x/RwEQ-T4
    iam_auth_enabled            = true
    manage_dspm_user            = true
  })
}
