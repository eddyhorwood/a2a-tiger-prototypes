/*
Pass AWS backend configuration via CLI args e.g.
  terraform init `
    -backend-config="bucket=<s3_bucket_name>" `
    -backend-config="region=<aws_region_name>" `
    -backend-config="dynamodb_table=<dynamodb_lock_table_name>"
*/

terraform {
  required_version = "~> 1.10.4"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.83"
    }

    kora = {
      source  = "tf.xero.dev/paas/kora"
      version = "~> 1.7"
    }
  }
}

provider "kora" {}

provider "aws" {
  region = local.aws_region
  default_tags {
    tags = {
      # Partially Satisfies XREQ-93
      uuid = "BdzazQewZtyQbUfCK7XYxc"
    }
  }
  ignore_tags {
    key_prefixes = ["managed:ownership:"]
  }
}

terraform {
  backend "s3" {
    # 'bucket', 'dynamodb_table' and 'region' will be specified on the command line
    key     = "infra/terraform/terraform.tfstate"
    encrypt = true
  }
}

module "payment-execution-queue" {
  source                    = "./modules/payment-execution-sqs"
  aws_account_id            = local.aws_account_id
  environment               = var.environment
  execution_sqs_kms_key_arn = local.execution_sqs_kms_key_arn
}
module "dlq-alerting" {
  source          = "./modules/dlq-alerting"
  sqs_queue_name  = module.payment-execution-queue.dlq_name
  sns_kms_key_arn = var.secrets_kms_arn
  count           = var.environment == "production" ? 1 : 0
}
