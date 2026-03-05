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
      uuid = "334db7HzPYMT119B9go9X9"
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
