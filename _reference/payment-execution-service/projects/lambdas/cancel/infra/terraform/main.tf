/*
Pass AWS backend configuration via CLI args e.g.
  terraform init `
    -backend-config="bucket=<s3_bucket_name>" `
    -backend-config="region=<aws_region_name>" `
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
      version = "~> 1.7"
    }
  }
}

provider "kora" {}

provider "aws" {
  region = local.aws_region
  default_tags {
    # Partially Satisfies XREQ-93
    tags = {
      uuid = local.common_tags.uuid
    }
  }
  ignore_tags {
    key_prefixes = ["managed:ownership:"]
  }
}

terraform {
  backend "s3" {
    # 'bucket', 'kms_key_id', and 'region' will be specified on the command line
    key          = "infra/terraform/terraform.tfstate"
    encrypt      = true
    use_lockfile = true
  }
}

module "cancel-execution-queue" {
  source                    = "./cancel-execution-sqs"
  environment               = var.environment
  execution_sqs_kms_key_arn = local.execution_sqs_kms_key_arn
}
